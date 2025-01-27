﻿using Silk.NET.Direct3D11;

namespace Molten.Graphics.DX11
{
    /// <summary>Stores a rasterizer state for use with a <see cref="GraphicsQueueDX11"/>.</summary>
    internal unsafe class RasterizerStateDX11 : GraphicsObject
    {
        internal StructKey<RasterizerDesc2> Desc { get; }

        internal unsafe ref ID3D11RasterizerState2* NativePtr => ref _native;

        ID3D11RasterizerState2* _native;

        /// <summary>
        /// Creates a new instance of <see cref="RasterizerStateDX11"/>.
        /// </summary>
        /// <param name="device">The <see cref="DeviceDX11"/> to use when creating the underlying rasterizer state object.</param>
        /// <param name="desc"></param>
        internal RasterizerStateDX11(DeviceDX11 device, ref ShaderPassParameters parameters) : 
            base(device, GraphicsBindTypeFlags.Input)
        {
            Desc = new StructKey<RasterizerDesc2>();
            ref RasterizerDesc2 raDesc = ref Desc.Value;
            raDesc.MultisampleEnable = parameters.IsMultisampleEnabled;
            raDesc.DepthClipEnable = parameters.IsDepthClipEnabled;
            raDesc.AntialiasedLineEnable = parameters.IsAALineEnabled;
            raDesc.ScissorEnable = parameters.IsScissorEnabled;
            raDesc.FillMode = parameters.Fill.ToApi();
            raDesc.CullMode = parameters.Cull.ToApi();
            raDesc.DepthBias = parameters.DepthBiasEnabled ? parameters.DepthBias : 0;
            raDesc.DepthBiasClamp = parameters.DepthBiasEnabled ? parameters.DepthBiasClamp : 0;
            raDesc.SlopeScaledDepthBias = parameters.SlopeScaledDepthBias;
            raDesc.ConservativeRaster = (ConservativeRasterizationMode)parameters.ConservativeRaster;
            raDesc.ForcedSampleCount = parameters.ForcedSampleCount;
            raDesc.FrontCounterClockwise = parameters.IsFrontCounterClockwise;

            device.Ptr->CreateRasterizerState2(Desc, ref _native);
        }

        protected override void OnApply(GraphicsQueue cmd) { }

        public override void GraphicsRelease()
        {
            SilkUtil.ReleasePtr(ref _native);
            Desc.Dispose();
        }

        public static implicit operator ID3D11RasterizerState*(RasterizerStateDX11 state)
        {
            return (ID3D11RasterizerState*)state._native;
        }
    }
}
