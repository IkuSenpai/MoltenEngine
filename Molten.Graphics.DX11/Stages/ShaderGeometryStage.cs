﻿using Silk.NET.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Molten.Graphics
{
    internal class ShaderGeometryStage : PipeShaderStage<ID3D11GeometryShader>
    {
        public ShaderGeometryStage(PipeDX11 pipe) :
            base(pipe, ShaderType.GeometryShader)
        {

        }

        protected override unsafe void OnBindConstants(PipeSlotGroup<ShaderConstantBuffer> grp,
            ID3D11Buffer** buffers, uint* firstConstants, uint* numConstants)
        {
            Pipe.Context->GSSetConstantBuffers1(grp.FirstChanged, grp.NumSlotsChanged, buffers, firstConstants, numConstants);
        }

        protected override unsafe void OnBindResources(PipeSlotGroup<PipeBindableResource> grp,
            ID3D11ShaderResourceView** srvs)
        {
            Pipe.Context->GSSetShaderResources(grp.FirstChanged, grp.NumSlotsChanged, srvs);
        }

        protected override unsafe void OnBindSamplers(PipeSlotGroup<ShaderSampler> grp, ID3D11SamplerState** resources)
        {
            Pipe.Context->GSSetSamplers(grp.FirstChanged, grp.NumSlotsChanged, resources);
        }

        protected override unsafe void OnBindShader(PipeSlot<ShaderComposition<ID3D11GeometryShader>> slot)
        {
            if (slot.BoundValue != null)
                Pipe.Context->GSSetShader(slot.BoundValue.RawShader, null, 0);
            else
                Pipe.Context->GSSetShader(null, null, 0);
        }
    }
}