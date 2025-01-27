﻿namespace Molten.Graphics
{
    internal class FinalizeStep : RenderStep
    {
        RenderCamera _orthoCamera;
        ObjectRenderData _dummyData;

        internal override void Initialize(RenderService renderer)
        {
            _dummyData = new ObjectRenderData();
            _orthoCamera = new RenderCamera(RenderCameraMode.Orthographic);
        }

        public override void Dispose()
        {

        }

        internal override void Render(RenderService renderer, RenderCamera camera, RenderChainContext context, Timing time)
        {
            _orthoCamera.Surface = camera.Surface;

            RectangleF bounds = new RectangleF(0, 0, camera.Surface.Width, camera.Surface.Height);
            GraphicsQueue cmd = renderer.Device.Queue;

            if (!camera.HasFlags(RenderCameraFlags.DoNotClear))
                renderer.Surfaces.ClearIfFirstUse(camera.Surface, camera.BackgroundColor);

            cmd.SetRenderSurfaces(camera.Surface);
            cmd.DepthSurface.Value = null;
            cmd.SetViewports(camera.Surface.Viewport);
            cmd.SetScissorRectangle((Rectangle)camera.Surface.Viewport.Bounds);

            // We only need scissor testing here
            IRenderSurface2D sourceSurface = context.HasComposed ? context.PreviousComposition : renderer.Surfaces[MainSurfaceType.Scene];
            RectStyle style = RectStyle.Default;

            cmd.Begin();
            renderer.SpriteBatch.Draw(sourceSurface, bounds, Vector2F.Zero, camera.Surface.Viewport.Bounds.Size, 0, Vector2F.Zero, ref style, null, 0, 0);

            if (camera.HasFlags(RenderCameraFlags.ShowOverlay))
                renderer.Overlay.Render(time, renderer.SpriteBatch, renderer.Profiler, context.Scene.Profiler, camera);

            renderer.SpriteBatch.Flush(cmd, _orthoCamera, _dummyData);
            cmd.End();
        }
    }
}
