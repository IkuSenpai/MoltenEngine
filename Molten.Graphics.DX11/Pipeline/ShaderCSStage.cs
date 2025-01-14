﻿using Silk.NET.Direct3D11;

namespace Molten.Graphics.DX11
{
    internal class ShaderCSStage : ShaderStageDX11
    {
        public ShaderCSStage(GraphicsQueueDX11 queue) : base(queue, ShaderType.Compute)
        {
            uint uavSlots = queue.Device.Capabilities.Compute.MaxUnorderedAccessSlots;
            UAVs = queue.RegisterSlotGroup(GraphicsBindTypeFlags.Output, "UAV", uavSlots, new UavGroupBinder(this));
        }

        internal override bool Bind()
        {
            bool baseChanged = base.Bind();
            bool uavChanged = false;

            ShaderComposition composition = Shader.BoundValue;

            // Apply unordered acces views to slots
            if (composition != null)
            {
                for (int j = 0; j < composition.UnorderedAccessIds.Count; j++)
                {
                    uint slotID = composition.UnorderedAccessIds[j];
                    UAVs[slotID].Value = composition.Pass.Parent.UAVs[slotID]?.Resource;
                }

                uavChanged = UAVs.BindAll();
            }
            else
            {
                // NOTE Unbind UAVs?
            }

            return uavChanged || baseChanged;
        }

        internal override unsafe void SetConstantBuffers(uint startSlot, uint numBuffers, ID3D11Buffer** buffers)
        {
            Cmd.Ptr->CSSetConstantBuffers(startSlot, numBuffers, buffers);
        }

        internal override unsafe void SetResources(uint startSlot, uint numViews, ID3D11ShaderResourceView1** views)
        {
            Cmd.Ptr->CSSetShaderResources(startSlot, numViews, (ID3D11ShaderResourceView**)views);
        }

        internal override unsafe void SetSamplers(uint startSlot, uint numSamplers, ID3D11SamplerState** states)
        {
            Cmd.Ptr->CSSetSamplers(startSlot, numSamplers, states);
        }

        internal override unsafe void SetShader(void* shader, ID3D11ClassInstance** classInstances, uint numClassInstances)
        {
            Cmd.Ptr->CSSetShader((ID3D11ComputeShader*)shader, classInstances, numClassInstances);
        }

        internal unsafe void SetUnorderedAccessViews(uint startSlot, uint numUAVs, ID3D11UnorderedAccessView1** ppUnorderedAccessViews, uint* pUAVInitialCounts)
        {
            Cmd.Ptr->CSSetUnorderedAccessViews(startSlot, numUAVs, (ID3D11UnorderedAccessView**)ppUnorderedAccessViews, pUAVInitialCounts);
        }

        internal GraphicsSlotGroup<GraphicsResource> UAVs { get; }
    }
}
