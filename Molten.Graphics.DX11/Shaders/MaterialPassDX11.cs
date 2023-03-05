﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Molten.Font;
using Silk.NET.Core.Native;

namespace Molten.Graphics
{
    public class MaterialPassDX11 : MaterialPass
    {
        public MaterialPassDX11(Material material, string name) : base(material, name) { }

        protected override void OnInitializeState(ref GraphicsStateParameters parameters)
        {
            // Check for unsupported features
            if (parameters.RasterizerDiscardEnabled)
                throw new NotSupportedException($"DirectX 11 mode does not support enabling of '{nameof(GraphicsStateParameters.RasterizerDiscardEnabled)}'");

            DeviceDX11 device = Device as DeviceDX11;

            BlendState = new BlendStateDX11(device, ref parameters);
            BlendState = Device.CacheObject(BlendState.Desc, BlendState);

            RasterizerState = new RasterizerStateDX11(device, ref parameters);
            RasterizerState = Device.CacheObject(RasterizerState.Desc, RasterizerState);

            DepthState = new DepthStateDX11(device, ref parameters);
            DepthState = Device.CacheObject(DepthState.Desc, DepthState);

            Topology = parameters.Topology.ToApi();
        }

        protected override void OnApply(GraphicsCommandQueue cmd) { }

        internal DepthStateDX11 DepthState { get; private set; }

        internal RasterizerStateDX11 RasterizerState { get; private set; }

        internal BlendStateDX11 BlendState { get; private set; }

        internal D3DPrimitiveTopology Topology { get; private set; }
    }
}