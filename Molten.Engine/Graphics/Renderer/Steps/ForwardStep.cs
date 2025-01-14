﻿namespace Molten.Graphics
{
    /// <summary>
    /// The forward-rendering step.
    /// </summary>
    internal class ForwardStep : RenderStep
    {
        public override void Dispose()
        { }

        internal override void Render(RenderService renderer, RenderCamera camera, RenderChainContext context, Timing time)
        {
            if (context.Layer.Renderables.Count == 0)
                return;

            GraphicsQueue cmd = renderer.Device.Queue;
            IRenderSurface2D sScene = renderer.Surfaces[MainSurfaceType.Scene];
            cmd.SetRenderSurface(sScene, 0);
            cmd.DepthSurface.Value = renderer.Surfaces.GetDepth();
            cmd.SetViewports(camera.Surface.Viewport);
            cmd.SetScissorRectangle((Rectangle)camera.Surface.Viewport.Bounds);

            cmd.Begin();
            renderer.RenderSceneLayer(cmd, context.Layer, camera);
            cmd.End();
        }
    }
}
