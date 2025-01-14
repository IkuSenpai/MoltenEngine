﻿using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.Maths;

namespace Molten.Graphics.DX11
{
    internal delegate void CmdQueueDrawCallback();
    internal delegate void CmdQueueDrawFailCallback(ShaderPassDX11 pass, uint passNumber, GraphicsBindResult result);

    /// <summary>Manages the pipeline of a either an immediate or deferred <see cref="GraphicsQueueDX11"/>.</summary>
    public unsafe partial class GraphicsQueueDX11 : GraphicsQueue
    {
        internal const uint D3D11_KEEP_UNORDERED_ACCESS_VIEWS = 0xffffffff;

        /// <summary>
        ///  If you set NumRTVs to D3D11_KEEP_RENDER_TARGETS_AND_DEPTH_STENCIL (0xffffffff), 
        ///  this method does not modify the currently bound render-target views (RTVs) and also does not modify depth-stencil view (DSV).
        /// </summary>
        internal const uint D3D11_KEEP_RENDER_TARGETS_AND_DEPTH_STENCIL = 0xffffffff;
 
        Rectangle[] _scissorRects;
        bool _scissorRectsDirty;

        ViewportF[] _viewports;
        bool _viewportsDirty;
        ViewportF[] _nullViewport;

        ShaderStageDX11[] _shaderStages;
        ShaderCSStage _cs;

        D3DPrimitiveTopology _boundTopology;
        GraphicsSlot<VertexInputLayout> _vertexLayout;
        List<VertexInputLayout> _cachedLayouts = new List<VertexInputLayout>();

        GraphicsDepthWritePermission _boundDepthMode = GraphicsDepthWritePermission.Enabled;

        internal ID3D11RenderTargetView1** RTVs;
        uint _numRTVs;
        internal ID3D11DepthStencilView* DSV;

        GraphicsSlot<BlendStateDX11> _stateBlend;
        GraphicsSlot<RasterizerStateDX11> _stateRaster;
        GraphicsSlot<DepthStateDX11> _stateDepth;
        GraphicsSlotGroup<GraphicsResource> _renderUAVs;

        ID3D11DeviceContext4* _native;
        ID3DUserDefinedAnnotation* _debugAnnotation;

        CommandListDX11 _cmd;

        internal GraphicsQueueDX11(DeviceDX11 device, ID3D11DeviceContext4* context) :
            base(device)
        {      
            DXDevice = device;
            _native = context;

            if (_native->GetType() == DeviceContextType.Immediate)
                Type = CommandQueueType.Immediate;
            else
                Type = CommandQueueType.Deferred;

            Guid debugGuid = ID3DUserDefinedAnnotation.Guid;
            void* ptrDebug = null;
            _native->QueryInterface(ref debugGuid, &ptrDebug);
            _debugAnnotation = (ID3DUserDefinedAnnotation*)ptrDebug;

            uint maxRTs = Device.Capabilities.PixelShader.MaxOutputTargets;
            _scissorRects = new Rectangle[maxRTs];
            _viewports = new ViewportF[maxRTs];
            _nullViewport = new ViewportF[1];

            uint maxVBuffers = Device.Capabilities.VertexBuffers.MaxSlots;
            VertexBuffers = RegisterSlotGroup<GraphicsBuffer, VertexBufferGroupBinder>(GraphicsBindTypeFlags.Input, "V-Buffer", maxVBuffers);
            IndexBuffer = RegisterSlot<GraphicsBuffer, IndexBufferBinder>(GraphicsBindTypeFlags.Input, "I-Buffer", 0);
            _vertexLayout = RegisterSlot<VertexInputLayout, InputLayoutBinder>(GraphicsBindTypeFlags.Input, "Vertex Input Layout", 0);
            Shader = RegisterSlot<HlslShader, ShaderBinder>(GraphicsBindTypeFlags.Input, "Shader", 0);

            _shaderStages = new ShaderStageDX11[]
            {
                new ShaderVSStage(this),
                new ShaderHSStage(this),
                new ShaderDSStage(this),
                new ShaderGSStage(this),
                new ShaderPSStage(this)
            };

            _cs = new ShaderCSStage(this);

            _stateBlend = RegisterSlot<BlendStateDX11, BlendBinder>(GraphicsBindTypeFlags.Input, "Blend State", 0);
            _stateDepth = RegisterSlot<DepthStateDX11, DepthStencilBinder>(GraphicsBindTypeFlags.Input, "Depth-Stencil State", 0);
            _stateRaster = RegisterSlot<RasterizerStateDX11, RasterizerBinder>(GraphicsBindTypeFlags.Input, "Rasterizer State", 0);

            uint numRenderUAVs = Device.Capabilities.VertexShader.MaxUnorderedAccessSlots;
            _renderUAVs = RegisterSlotGroup(GraphicsBindTypeFlags.Output, "UAV", numRenderUAVs, new UavGroupBinderOM());

            RTVs = EngineUtil.AllocPtrArray<ID3D11RenderTargetView1>(maxRTs);

            Surfaces = RegisterSlotGroup<IRenderSurface2D, SurfaceGroupBinder>(GraphicsBindTypeFlags.Output, "Render Surface", maxRTs);
            DepthSurface = RegisterSlot<IDepthStencilSurface, DepthSurfaceBinder>(GraphicsBindTypeFlags.Output, "Depth Surface", 0);

            // Apply the surface of the graphics device's output initialally.
            SetRenderSurfaces(null);
        }

        public override void Begin(GraphicsCommandListFlags flags = GraphicsCommandListFlags.None)
        {
            base.Begin(flags);

            if (Type != CommandQueueType.Deferred)
                _cmd = null;
        }

        public override GraphicsCommandList End()
        {
            base.End();

            if (Type == CommandQueueType.Deferred)
            {
                ID3D11CommandList* ptrCmd = EngineUtil.Alloc<ID3D11CommandList>();

                _native->FinishCommandList(false, &ptrCmd);
                _cmd = new CommandListDX11(this, ptrCmd);
                Tracker.Track(_cmd);

                if (_cmd.Flags.Has(GraphicsCommandListFlags.CpuSyncable))
                    _cmd.Fence = new GraphicsOpenFence();
            }

            return _cmd;
        }

        public override void Execute(GraphicsCommandList list)
        {
            if (!list.Flags.Has(GraphicsCommandListFlags.Deferred))
                throw new GraphicsCommandQueueException(this, "Cannot execute an immediate-mode command list. Use Submit() instead.");

            CommandListDX11 cmd = list as CommandListDX11;
            _native->ExecuteCommandList(cmd.Ptr, true);
        }

        public override void Submit(GraphicsCommandListFlags flags)
        {
            if (flags.Has(GraphicsCommandListFlags.Deferred))
                throw new GraphicsCommandQueueException(this, "Cannot submit deferred command lists to the immediate graphics queue");
        }

        protected override unsafe ResourceMap GetResourcePtr(GraphicsResource resource, uint subresource, GraphicsMapType mapType)
        {
            Map map = 0;
            GraphicsResourceFlags flags = resource.Flags;

            if (flags.Has(GraphicsResourceFlags.CpuWrite))
            {
                if (mapType == GraphicsMapType.Discard)
                {
                    map = Map.WriteDiscard;
                    Profiler.Current.MapDiscardCount++;
                }
                else
                {
                    if (resource is GraphicsBuffer buffer &&
                        (buffer.BufferType == GraphicsBufferType.Vertex || buffer.BufferType == GraphicsBufferType.Index))
                    {
                        map = Map.WriteNoOverwrite;
                        Profiler.Current.MapNoOverwriteCount++;
                    }
                    else
                    {
                        if (resource.Flags.Has(GraphicsResourceFlags.CpuWrite) &&
                            !resource.Flags.Has(GraphicsResourceFlags.CpuRead) &&
                            !resource.Flags.Has(GraphicsResourceFlags.GpuWrite))
                        {
                            map = Map.WriteNoOverwrite;
                            Profiler.Current.MapNoOverwriteCount++;
                        }
                        else
                        {
                            map = Map.Write;
                            Profiler.Current.MapReadWriteCount++;
                        }
                    }
                }
            }
            else
            {
                if (mapType != GraphicsMapType.Read)
                    throw new InvalidOperationException($"Cannot map a resource for writing without CPU write access");
            }

            if (flags.Has(GraphicsResourceFlags.CpuRead))
            {
                map |= Map.Read;

                // Only increment if we haven't already incremented during write flag check (above).
                if (!flags.Has(GraphicsResourceFlags.CpuWrite))
                    Profiler.Current.MapReadWriteCount++;
            }
            else
            {
                if (mapType == GraphicsMapType.Read)
                    throw new InvalidOperationException($"Cannot map a resource for reading without CPU read access");
            }

            MappedSubresource resMap = new MappedSubresource();
            _native->Map((ID3D11Resource*)resource.Handle, subresource, map, 0, &resMap);
            return new ResourceMap(resMap.PData, resMap.RowPitch, resMap.DepthPitch);
        }

        protected override void OnUnmapResource(GraphicsResource resource, uint subresource)
        {
            _native->Unmap((ID3D11Resource*)resource.Handle, subresource);
        }

        public override unsafe void CopyResourceRegion(
            GraphicsResource source, uint srcSubresource, ResourceRegion* sourceRegion, 
            GraphicsResource dest, uint destSubresource, Vector3UI destStart)
        {
            Box* box = (Box*)sourceRegion;

            _native->CopySubresourceRegion((ID3D11Resource*)dest.Handle, destSubresource, destStart.X, destStart.Y, destStart.Z, (ID3D11Resource*)source.Handle, srcSubresource, box);
            Profiler.Current.CopySubresourceCount++;
        }

        protected override unsafe void UpdateResource(GraphicsResource resource, uint subresource, ResourceRegion? region, void* ptrData, uint rowPitch, uint slicePitch)
        {
            Box* destBox = null;

            if (region != null)
            {
                ResourceRegion value = region.Value;
                destBox = (Box*)&value;
            }

            _native->UpdateSubresource((ID3D11Resource*)resource.Handle, subresource, destBox, ptrData, rowPitch, slicePitch);
            Profiler.Current.UpdateSubresourceCount++;
        }

        protected override void CopyResource(GraphicsResource src, GraphicsResource dest)
        {
            if (src.Handle == null)
                src.Apply(this);

            if(dest is GraphicsBuffer buffer && buffer.BufferType == GraphicsBufferType.Staging)
                dest.Apply(this);

            _native->CopyResource((ID3D11Resource*)dest.Handle, (ID3D11Resource*)src.Handle);
            Profiler.Current.CopyResourceCount++;
        }

        private GraphicsBindResult ApplyRenderState(ShaderPassDX11 pass,
            QueueValidationMode mode)
        {
            if (pass.Topology == D3DPrimitiveTopology.D3D11PrimitiveTopologyUndefined)
                return GraphicsBindResult.UndefinedTopology;

            // Check topology
            if (_boundTopology != pass.Topology)
            {
                _boundTopology = pass.Topology;
                _native->IASetPrimitiveTopology(_boundTopology);
            }

            Span<bool> stageChanged = stackalloc bool[_shaderStages.Length];

            for(int i = 0; i < _shaderStages.Length; i++)
            {
                ShaderComposition composition = pass[_shaderStages[i].Type];
                _shaderStages[i].Shader.Value = composition;
                stageChanged[i] = _shaderStages[i].Bind();

                // Set the UAVs needed by each render stage
                if (composition != null)
                {
                    for (int j = 0; j < composition.UnorderedAccessIds.Count; j++)
                    {
                        uint slotID = composition.UnorderedAccessIds[j];
                        _renderUAVs[slotID].Value = composition.Pass.Parent.UAVs[slotID]?.Resource;
                    }
                }
            }

            bool vsChanged = stageChanged[0]; // Stage 0 is vertex buffer.
            bool ibChanged = IndexBuffer.Bind();
            bool vbChanged = VertexBuffers.BindAll();
            bool uavChanged = _renderUAVs.BindAll();

            // Does the vertex input layout need updating?
            if (vbChanged || vsChanged)
            {
                _vertexLayout.Value = GetInputLayout(pass);
                _vertexLayout.Bind();
            }

            GraphicsDepthWritePermission depthWriteMode = pass.WritePermission;
            _stateBlend.Value = pass.BlendState;
            _stateDepth.Value = pass.DepthState;
            _stateRaster.Value = pass.RasterizerState;

            bool bStateChanged = _stateBlend.Bind();
            bool rStateChanged = _stateDepth.Bind();
            bool dStateChanged = _stateRaster.Bind();

            // Check if scissor rects need updating
            if (_scissorRectsDirty)
            {
                fixed (Rectangle* ptrRect = _scissorRects)
                    _native->RSSetScissorRects((uint)_scissorRects.Length, (Box2D<int>*)ptrRect);

                _scissorRectsDirty = false;
            }

            // Check if viewports need updating.
            // TODO Consolidate - Molten viewports are identical in memory layout to DX11 viewports.
            if (_viewportsDirty)
            {
                fixed (ViewportF* ptrViewports = _viewports)
                    _native->RSSetViewports((uint)_viewports.Length, (Silk.NET.Direct3D11.Viewport*)ptrViewports);

                _viewportsDirty = false;
            }

            bool surfaceChanged = Surfaces.BindAll();
            bool depthChanged = DepthSurface.Bind() || (_boundDepthMode != depthWriteMode);

            if (surfaceChanged || depthChanged)
            {
                if (surfaceChanged)
                {
                    _numRTVs = 0;

                    for (uint i = 0; i < Surfaces.SlotCount; i++)
                    {
                        if (Surfaces[i].BoundValue != null)
                        {
                            _numRTVs = (i + 1);
                            RTVs[i] = (Surfaces[i].BoundValue as RenderSurface2DDX11).RTV.Ptr;
                        }
                        else
                        {
                            RTVs[i] = null;
                        }
                    }
                }

                if (depthChanged)
                {
                    if (DepthSurface.BoundValue != null && depthWriteMode != GraphicsDepthWritePermission.Disabled)
                    {
                        DepthSurfaceDX11 dss = DepthSurface.BoundValue as DepthSurfaceDX11;
                        if (depthWriteMode == GraphicsDepthWritePermission.ReadOnly)
                            DSV = dss.ReadOnlyView;
                        else
                            DSV = dss.DepthView;
                    }
                    else
                    {
                        DSV = null;
                    }

                    _boundDepthMode = depthWriteMode;
                }

                _native->OMSetRenderTargets(_numRTVs, (ID3D11RenderTargetView**)RTVs, DSV);
                Profiler.Current.SurfaceBindings++;
            }

            // Validate pipeline state.
            return Validate(mode);
        }

        private GraphicsBindResult ApplyComputeState(ShaderPassDX11 pass)
        {
            _cs.Shader.Value = pass[ShaderType.Compute];
            _cs.Bind();

            Vector3UI groups = DrawInfo.Custom.ComputeGroups;
            if (groups.X == 0)
                groups.X = pass.ComputeGroups.X;

            if (groups.Y == 0)
                groups.Y = pass.ComputeGroups.Y;

            if (groups.Z == 0)
                groups.Z = pass.ComputeGroups.Z;

            DrawInfo.ComputeGroups = groups;
            return Validate(QueueValidationMode.Compute);
        }

        public override void BeginEvent(string label)
        {
            fixed(char* ptr = label)
                _debugAnnotation->BeginEvent(ptr);
        }

        public override void EndEvent()
        {
            _debugAnnotation->EndEvent();
        }

        public override void SetMarker(string label)
        {
            fixed (char* ptr = label)
                _debugAnnotation->SetMarker(ptr);
        }

        private GraphicsBindResult ApplyState(HlslShader shader, QueueValidationMode mode, CmdQueueDrawCallback drawCallback)
        {
            GraphicsBindResult vResult = GraphicsBindResult.Successful;

            if (!DrawInfo.Began)
                throw new GraphicsCommandQueueException(this, $"{nameof(GraphicsQueueDX11)}: BeginDraw() must be called before calling {nameof(Draw)}()");

            Shader.Value = shader;
            bool shaderChanged = Shader.Bind();

            if (Shader.BoundValue == null)
                return GraphicsBindResult.NoShader;

            // Re-render the same material for mat.Iterations.
            BeginEvent($"{mode} Call");
            for (uint i = 0; i < shader.Passes.Length; i++)
            {
                ShaderPassDX11 pass = shader.Passes[i] as ShaderPassDX11;
                if (!pass.IsEnabled)
                {
                    SetMarker($"Pass {i} - Skipped (Disabled)");
                    continue;
                }

                if (pass.IsCompute)
                {
                    BeginEvent($"Pass {i} - Compute");
                    vResult = ApplyComputeState(pass);
                    if (vResult == GraphicsBindResult.Successful)
                    {
                        Vector3UI groups = DrawInfo.ComputeGroups;

                        // Re-render the same pass for K iterations.
                        for (int j = 0; j < pass.Iterations; j++)
                        {
                            BeginEvent($"Iteration {j}"); 
                            _native->Dispatch(groups.X, groups.Y, groups.Z);
                            Profiler.Current.DispatchCalls++;
                            EndEvent();
                        }

                        pass.InvokeCompleted(DrawInfo.Custom);
                    }
                    EndEvent();
                }
                else
                {
                    // Skip non-compute passes when we're in compute-only mode.
                    if (mode == QueueValidationMode.Compute)
                    {
                        SetMarker($"Pass {i} - Skipped (Compute-Only-mode)");
                        continue;
                    };

                    BeginEvent($"Pass {i} - Render");
                    vResult = ApplyRenderState(pass, mode);
                    if (vResult == GraphicsBindResult.Successful)
                    {
                        // Re-render the same pass for K iterations.
                        for (int k = 0; k < pass.Iterations; k++)
                        {
                            BeginEvent($"Iteration {k}");
                            drawCallback();
                            Profiler.Current.DrawCalls++;
                            EndEvent();
                        }

                        pass.InvokeCompleted(DrawInfo.Custom);
                    }
                    EndEvent();
                }

                if (vResult != GraphicsBindResult.Successful)
                {
                    Device.Log.Warning($"{mode} call failed with result: {vResult} -- Iteration: Pass {i}/{shader.Passes.Length} -- " +
                    $"Shader: {shader.Name} -- Topology: {pass.Topology} -- IsCompute: {pass.IsCompute}");
                    break;
                }
            }
            EndEvent();

            (Device as DeviceDX11).ProcessDebugLayerMessages();
            return vResult;
        }

        public override GraphicsBindResult Draw(HlslShader shader, uint vertexCount, uint vertexStartIndex = 0)
        {
            return ApplyState(shader, QueueValidationMode.Unindexed, () => _native->Draw(vertexCount, vertexStartIndex));
        }

        /// <inheritdoc/>
        public override GraphicsBindResult DrawInstanced(HlslShader shader,
            uint vertexCountPerInstance,
            uint instanceCount,
            uint vertexStartIndex = 0,
            uint instanceStartIndex = 0)
        {
            return ApplyState(shader, QueueValidationMode.Instanced, () =>
            _native->DrawInstanced(vertexCountPerInstance, instanceCount, vertexStartIndex, instanceStartIndex));
        }

        /// <inheritdoc/>
        public override GraphicsBindResult DrawIndexed(HlslShader shader,
            uint indexCount,
            int vertexIndexOffset = 0,
            uint startIndex = 0)
        {
            return ApplyState(shader, QueueValidationMode.Indexed, () => _native->DrawIndexed(indexCount, startIndex, vertexIndexOffset));
        }

        /// <inheritdoc/>
        public override GraphicsBindResult DrawIndexedInstanced(HlslShader shader,
            uint indexCountPerInstance,
            uint instanceCount,
            uint startIndex = 0,
            int vertexIndexOffset = 0,
            uint instanceStartIndex = 0)
        {
            return ApplyState(shader, QueueValidationMode.InstancedIndexed, () =>
                _native->DrawIndexedInstanced(indexCountPerInstance, instanceCount, startIndex, vertexIndexOffset, instanceStartIndex));
        }

        /// <inheritdoc/>
        public override GraphicsBindResult Dispatch(HlslShader shader, Vector3UI groups)
        {
            DrawInfo.Custom.ComputeGroups = groups;
            return ApplyState(shader, QueueValidationMode.Compute, null);
        }

        public override void SetRenderSurfaces(IRenderSurface2D[] surfaces, uint count)
        {
            if (surfaces != null)
            {
                for (uint i = 0; i < count; i++)
                    Surfaces[i].Value = surfaces[i] as RenderSurface2DDX11;
            }
            else
            {
                count = 0;
            }

            // Set the remaining surfaces to null.
            for (uint i = count; i < Surfaces.SlotCount; i++)
                Surfaces[i].Value = null;
        }

        public override void SetRenderSurface(IRenderSurface2D surface, uint slot)
        {
            Surfaces[slot].Value = surface as RenderSurface2DDX11;
        }

        public override void GetRenderSurfaces(IRenderSurface2D[] destinationArray)
        {
            if (destinationArray.Length < Surfaces.SlotCount)
                throw new InvalidOperationException($"The destination array is too small ({destinationArray.Length}). A minimum size of {Surfaces.SlotCount} is needed.");

            for (uint i = 0; i < Surfaces.SlotCount; i++)
                destinationArray[i] = Surfaces[i].Value;
        }

        
        public override IRenderSurface2D GetRenderSurface(uint slot)
        {
            return Surfaces[slot].Value;
        }
 
        public override void ResetRenderSurfaces()
        {
            for (uint i = 0; i < Surfaces.SlotCount; i++)
                Surfaces[i].Value = null;
        }

        public override void SetScissorRectangle(Rectangle rect, int slot = 0)
        {
            _scissorRects[slot] = rect;
            _scissorRectsDirty = true;
        }

        public override void SetScissorRectangles(params Rectangle[] rects)
        {
            for (int i = 0; i < rects.Length; i++)
                _scissorRects[i] = rects[i];

            // Reset any remaining scissor rectangles to whatever the first is.
            for (int i = rects.Length; i < _scissorRects.Length; i++)
                _scissorRects[i] = _scissorRects[0];

            _scissorRectsDirty = true;
        }

        public override void SetViewport(ViewportF vp, int slot)
        {
            _viewports[slot] = vp;
        }

        public override void SetViewports(ViewportF vp)
        {
            for (int i = 0; i < _viewports.Length; i++)
                _viewports[i] = vp;

            _viewportsDirty = true;
        }

        public override void SetViewports(ViewportF[] viewports)
        {
            if (viewports == null)
            {
                IRenderSurface2D surface = null;
                IRenderSurface2D surfaceZero = GetRenderSurface(0);

                for (uint i = 0; i < _viewports.Length; i++)
                {
                    surface = GetRenderSurface(i);
                    _viewports[i] = surface != null ? surface.Viewport : surfaceZero.Viewport;
                }
            }
            else
            {
                for (int i = 0; i < viewports.Length; i++)
                    _viewports[i] = viewports[i];

                // Set remaining unset ones to whatever the first is.
                for (int i = viewports.Length; i < _viewports.Length; i++)
                    _viewports[i] = _viewports[0];
            }

            _viewportsDirty = true;
        }

        public override void GetViewports(ViewportF[] outArray)
        {
            Array.Copy(_viewports, outArray, _viewports.Length);
        }

        public override ViewportF GetViewport(int index)
        {
            return _viewports[index];
        }

        /// <summary>Retrieves or creates a usable input layout for the provided vertex buffers and sub-effect.</summary>
        /// <returns>An instance of InputLayout.</returns>
        private VertexInputLayout GetInputLayout(HlslPass pass)
        {
            // Retrieve layout list or create new one if needed.
            foreach (VertexInputLayout l in _cachedLayouts)
            {
                if (l.IsMatch(Device.Log, VertexBuffers))
                    return l;
            }

            ShaderComposition vs = pass[ShaderType.Vertex];
            VertexInputLayout input = new VertexInputLayout(DXDevice, VertexBuffers, (ID3D10Blob*)pass.InputByteCode, vs.InputStructure);
            _cachedLayouts.Add(input);

            return input;
        }

        private GraphicsBindResult Validate(QueueValidationMode mode)
        {
            GraphicsBindResult result = GraphicsBindResult.Successful;

            result |= CheckShader();

            // Validate and update mode-specific data if needed.
            switch (mode)
            {
                case QueueValidationMode.Indexed:
                    result |= CheckVertexSegment();
                    result |= CheckIndexSegment();
                    break;

                case QueueValidationMode.Instanced:
                    result |= CheckVertexSegment();
                    result |= CheckInstancing();
                    break;

                case QueueValidationMode.InstancedIndexed:
                    result |= CheckVertexSegment();
                    result |= CheckIndexSegment();
                    result |= CheckInstancing();
                    break;

                case QueueValidationMode.Compute:
                    result |= CheckComputeGroups();
                    break;
            }

            return result;
        }

        /// <summary>Validate vertex buffer and vertex shader.</summary>
        /// <param name="vbChanged">Has the vertex buffer changed.</param>
        /// <param name="veChanged">Has the vertex effect changed.</param>
        /// <returns></returns>
        private GraphicsBindResult CheckShader()
        {
            GraphicsBindResult result = GraphicsBindResult.Successful;

            if (Shader == null)
                result |= GraphicsBindResult.MissingMaterial;

            return result;
        }

        private GraphicsBindResult CheckComputeGroups()
        {
            ComputeCapabilities comCap = Device.Capabilities.Compute;
            Vector3UI groups = DrawInfo.ComputeGroups;

            if (groups.Z > comCap.MaxGroupCountZ)
            {
                Device.Log.Error($"Unable to dispatch compute shader. Z dimension ({groups.Z}) is greater than supported ({comCap.MaxGroupCountZ}).");
                return GraphicsBindResult.InvalidComputeGroupDimension;
            }

            if (groups.X > comCap.MaxGroupCountX)
            {
                Device.Log.Error($"Unable to dispatch compute shader. X dimension ({groups.X}) is greater than supported ({comCap.MaxGroupCountX}).");
                return GraphicsBindResult.InvalidComputeGroupDimension;
            }
            
            if (groups.Y > comCap.MaxGroupCountY)
            {
                Device.Log.Error($"Unable to dispatch compute shader. Y dimension ({groups.Y}) is greater than supported ({comCap.MaxGroupCountY}).");
                return GraphicsBindResult.InvalidComputeGroupDimension;
            }

            return GraphicsBindResult.Successful;
        }

        private GraphicsBindResult CheckVertexSegment()
        {
            GraphicsBindResult result = GraphicsBindResult.Successful;

            if (VertexBuffers[0] == null)
                result |= GraphicsBindResult.MissingVertexSegment;

            return result;
        }

        private GraphicsBindResult CheckIndexSegment()
        {
            GraphicsBindResult result = GraphicsBindResult.Successful;

            // If the index buffer is null, this method will always fail because 
            // it assumes it is only being called during an indexed draw call.
            if (IndexBuffer.BoundValue == null)
                result |= GraphicsBindResult.MissingIndexSegment;

            return result;
        }

        private GraphicsBindResult CheckInstancing()
        {
            if (_vertexLayout.BoundValue != null && _vertexLayout.BoundValue.IsInstanced)
                return GraphicsBindResult.Successful;
            else
                return GraphicsBindResult.NonInstancedVertexLayout;
        }

        /// <summary>Dispoes of the current <see cref="Graphics.GraphicsQueueDX11"/> instance.</summary>
        protected override void OnDispose()
        {
            SilkUtil.ReleasePtr(ref _native);
            SilkUtil.ReleasePtr(ref _debugAnnotation);

            // Dispose context.
            if (Type != CommandQueueType.Immediate)
                DXDevice.RemoveDeferredContext(this);

            EngineUtil.FreePtrArray(ref RTVs);
            _cmd.Dispose();
        }
        /// <summary>Gets the current <see cref="GraphicsQueueDX11"/> type. This value will not change during the context's life.</summary>
        internal CommandQueueType Type { get; private set; }

        internal DeviceDX11 DXDevice { get; private set; }

        protected override GraphicsCommandList Cmd
        {
            get => _cmd;
            set => _cmd = value as CommandListDX11;
        }

        internal ID3D11DeviceContext4* Ptr => _native;

        internal ID3DUserDefinedAnnotation* Debug => _debugAnnotation;
    }
}
