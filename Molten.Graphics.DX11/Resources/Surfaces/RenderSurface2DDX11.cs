﻿using Silk.NET.Direct3D11;
using Silk.NET.DXGI;

namespace Molten.Graphics.DX11
{
    public unsafe class RenderSurface2DDX11 : Texture2DDX11, IRenderSurface2D
    {
        internal RenderSurface2DDX11(
            GraphicsDevice device,
            uint width,
            uint height,
            GraphicsResourceFlags flags = GraphicsResourceFlags.None,
            GraphicsFormat format = GraphicsFormat.R8G8B8A8_SNorm,
            uint mipCount = 1,
            uint arraySize = 1,
            AntiAliasLevel aaLevel = AntiAliasLevel.None,
            MSAAQuality msaa = MSAAQuality.Default,
            bool allowMipMapGen = false, 
            string name = null)
            : base(device, width, height, flags, format, mipCount, arraySize, aaLevel, msaa, allowMipMapGen, name)
        {
            Viewport = new ViewportF(0, 0, width, height);

            Name = $"Surface_{name ?? GetType().Name}";
            RTV = new RenderTargetView(this)
            {
                Desc = new RenderTargetViewDesc1(DxgiFormat),
            };
        }

        protected override ID3D11Resource* CreateResource(bool resize)
        {
            RTV.Release();

            ID3D11Resource* resource =  base.CreateResource(resize);
            SetRTVDescription(ref RTV.Desc);

            if (_desc.SampleDesc.Count > 1)
            {
                RTV.Desc.ViewDimension = RtvDimension.Texture2Dmsarray;
                RTV.Desc.Texture2DMSArray = new Tex2DmsArrayRtv
                {
                    ArraySize = _desc.ArraySize,
                    FirstArraySlice = 0,
                };
            }
            else
            {
                RTV.Desc.ViewDimension = RtvDimension.Texture2Darray;
                RTV.Desc.Texture2DArray = new Tex2DArrayRtv1()
                {
                    ArraySize = _desc.ArraySize,
                    MipSlice = 0,
                    FirstArraySlice = 0,
                    PlaneSlice = 0,
                };
            }

            RTV.Create(resource);
            return resource;
        }

        protected virtual void SetRTVDescription(ref RenderTargetViewDesc1 desc) { }

        protected override void UpdateDescription(uint newWidth, uint newHeight, uint newDepth, uint newMipMapCount, uint newArraySize, Format newFormat)
        {
            _desc.Width = newWidth;
            _desc.Height = newHeight;
            _desc.Format = newFormat;
            //_description.MipLevels = newMipMapCount; // NOTE: Do we set this on render targets?

            Viewport = new ViewportF(Viewport.X, Viewport.Y, newWidth, newHeight);
        }

        internal virtual void OnClear(GraphicsQueueDX11 cmd, Color color)
        {
            OnApply(cmd);

            if (RTV.Ptr != null)
            {
                Color4 c4 = color;
                cmd.Ptr->ClearRenderTargetView((ID3D11RenderTargetView*)RTV.Ptr, (float*)&c4);
            }
        }

        public void Clear(GraphicsPriority priority, Color color)
        {
            QueueTask(priority, new SurfaceClearTask()
            {
                Color = color,
                Surface = this,
            });
        }

        /// <summary>Called when the render target needs to be disposed.</summary>
        public override void GraphicsRelease()
        {
            RTV.Release();
            base.GraphicsRelease();
        }

        /// <summary>Gets the viewport that defines the default renderable area of the render target.</summary>
        public ViewportF Viewport { get; protected set; }

        /// <summary>
        /// Gets the DX11 render target view (RTV) for the current render surface.
        /// </summary>
        internal RenderTargetView RTV { get; }

    }
}
