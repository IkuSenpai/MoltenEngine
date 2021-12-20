﻿using Silk.NET.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Molten.Graphics
{
    internal unsafe class GraphicsRasterizerStage : PipelineComponent<DeviceDX11, PipeDX11>
    {
        PipelineBindSlot<GraphicsRasterizerState, DeviceDX11, PipeDX11> _slotState;

        Silk.NET.Maths.Rectangle<int>[] _apiScissorRects;
        Rectangle[] _scissorRects;
        bool _scissorRectsDirty;

        Silk.NET.Direct3D11.Viewport[] _apiViewports;
        ViewportF[] _viewports;
        bool _viewportsDirty;
        ViewportF[] _nullViewport;

        internal GraphicsRasterizerStage(PipeDX11 pipe) : base(pipe)
        {
            _nullViewport = new ViewportF[1];

            int maxRTs = pipe.Device.Features.SimultaneousRenderSurfaces;
            _scissorRects = new Rectangle[maxRTs];
            _viewports = new ViewportF[maxRTs];
            _apiScissorRects = new Silk.NET.Maths.Rectangle<int>[maxRTs];
            _apiViewports = new Silk.NET.Direct3D11.Viewport[maxRTs];

            _slotState = AddSlot<GraphicsRasterizerState>(0);
            _slotState.OnObjectForcedUnbind += _slotState_OnBoundObjectDisposed;
        }

        private void _slotState_OnBoundObjectDisposed(PipelineBindSlot<DeviceDX11, PipeDX11> slot, PipelineDisposableObject obj)
        {
            if(Current == obj)
            {
                ID3D11RasterizerState* tmpState = null;
                Pipe.Context->RSSetState(tmpState);
            }
        }

        protected override void OnDispose()
        {
            Current = null;

            base.OnDispose();
        }

        public void SetScissorRectangle(Rectangle rect, int slot = 0)
        {
            _scissorRects[slot] = rect;
            _scissorRectsDirty = true;
        }

        public void SetScissorRectangles(Rectangle[] rects)
        {
            for (int i = 0; i < rects.Length; i++)
                _scissorRects[i] = rects[i];

            // Reset any remaining scissor rectangles to whatever the first is.
            for (int i = rects.Length; i < _scissorRects.Length; i++)
                _scissorRects[i] = _scissorRects[0];

            _scissorRectsDirty = true;
        }

        /// <summary>
        /// Applies the provided viewport value to the specified viewport slot.
        /// </summary>
        /// <param name="vp">The viewport value.</param>
        public void SetViewport(ViewportF vp, int slot)
        {
                _viewports[slot] = vp;
        }

        /// <summary>
        /// Applies the specified viewport to all viewport slots.
        /// </summary>
        /// <param name="vp">The viewport value.</param>
        public void SetViewports(ViewportF vp)
        {
            for (int i = 0; i < _viewports.Length; i++)
                _viewports[i] = vp;

            _viewportsDirty = true;
        }

        /// <summary>
        /// Sets the provided viewports on to their respective viewport slots. <para/>
        /// If less than the total number of viewport slots was provided, the remaining ones will be set to whatever the same value as the first viewport slot.
        /// </summary>
        /// <param name="viewports"></param>
        public void SetViewports(ViewportF[] viewports)
        {
            if (viewports == null)
            {
                RenderSurface surface = null;
                RenderSurface surfaceZero = Pipe.Output.GetRenderSurface(0);

                for (int i = 0; i < _viewports.Length; i++)
                {
                    surface = Pipe.Output.GetRenderSurface(i);
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

        public void GetViewports(ViewportF[] outArray)
        {
            Array.Copy(_viewports, outArray, _viewports.Length);
        }

        public ViewportF GetViewport(int index)
        {
            return _viewports[index];
        }

        /// <summary>Applies the current state to the device. Called internally.</summary>
        internal void Refresh()
        {
            // Ensure the default preset is used if a null state was requested.
            bool stateChanged = _slotState.Bind(Pipe, Current, PipelineBindType.Output);

            if (stateChanged)   // Update rasterizer state.
                Pipe.Context->RSSetState(Current);

            // Check if scissor rects need updating
            if (_scissorRectsDirty)
            {
                for (int i = 0; i < _scissorRects.Length; i++)
                    _apiScissorRects[i] = _scissorRects[i].ToApi();

                Pipe.Context->RSSetScissorRects((uint)_apiScissorRects.Length, ref _apiScissorRects[0]);
                _scissorRectsDirty = false;
            }

            // Check if viewports need updating.
            if (_viewportsDirty)
            {
                for (int i = 0; i < _viewports.Length; i++)
                    _apiViewports[i] = _viewports[i].ToApi();

                Pipe.Context->RSSetViewports((uint)_viewports.Length, ref _apiViewports[0]);
                _viewportsDirty = false;
            }
        }

        /// <summary>Gets the currently active blend state.</summary>
        public GraphicsRasterizerState Current { get; set; }

        /// <summary>Gets the number of applied viewports.</summary>
        public int ViewportCount => _viewports.Length;
    }
}