﻿using Silk.NET.Direct3D11;

namespace Molten.Graphics.DX11
{
    internal unsafe class SurfaceGroupBinder : GraphicsGroupBinder<IRenderSurface2D>
    {
        public override void Bind(GraphicsSlotGroup<IRenderSurface2D> grp, uint startIndex, uint endIndex, uint numChanged)
        {
            
        }

        public override void Bind(GraphicsSlot<IRenderSurface2D> slot, IRenderSurface2D value)
        {
            
        }

        public override void Unbind(GraphicsSlotGroup<IRenderSurface2D> grp, uint startIndex, uint endIndex, uint numChanged)
        {
            GraphicsQueueDX11 cmd = grp.Cmd as GraphicsQueueDX11;

            uint numRTs = endIndex + 1;
            var rtvs = cmd.RTVs;
            for (uint i = 0; i < numRTs; i++)
                rtvs[i] = null;

            cmd.Ptr->OMSetRenderTargets(numRTs, (ID3D11RenderTargetView**)rtvs, cmd.DSV);
        }

        public override void Unbind(GraphicsSlot<IRenderSurface2D> slot, IRenderSurface2D value)
        {
            GraphicsQueueDX11 cmd = slot.Cmd as GraphicsQueueDX11;

            var rtvs = cmd.RTVs;
            rtvs[slot.SlotIndex] = null;
            cmd.Ptr->OMSetRenderTargets(1, (ID3D11RenderTargetView**)rtvs, cmd.DSV);
        }
    }
}
