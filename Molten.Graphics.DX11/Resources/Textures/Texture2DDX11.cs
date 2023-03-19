﻿using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;

namespace Molten.Graphics
{
    public unsafe class Texture2DDX11 : TextureDX11, ITexture2D
    {
        internal ID3D11Texture2D1* NativeTexture;
        protected Texture2DDesc1 _desc;

        /// <summary>Creates a new instance of <see cref="Texture2DDX11"/> and uses a provided texture for its description. Note: This does not copy the contents 
        /// of the provided texture in to the new instance.</summary>
        /// <param name="descTexture">The <see cref="Texture2DDX11"/> to use as a template configuration for a new <see cref="Texture2DDX11"/> instance.</param>
        /// <param name="flags">A set of flags to override those of the provided template texture.</param>
        internal Texture2DDX11(Texture2DDX11 descTexture, TextureFlags flags)
            : this(descTexture.Renderer, 
                  descTexture.Width,
                  descTexture.Height, 
                  descTexture.DxgiFormat, 
                  descTexture.MipMapCount, 
                  descTexture.ArraySize, 
                  flags, 
                  descTexture.MultiSampleLevel, 
                  descTexture.SampleQuality, 
                  descTexture.Name)
        { }

        /// <summary>Creates a new instance of <see cref="Texture2DDX11"/> and uses a provided texture for its description. Note: This does not copy the contents 
        /// of the provided texture in to the new instance.</summary>
        /// <param name="descTexture">The <see cref="Texture2DDX11"/> to use as a template configuration for a new <see cref="Texture2DDX11"/> instance.</param>
        internal Texture2DDX11(Texture2DDX11 descTexture)
            : this(descTexture.Renderer, 
                  descTexture.Width, 
                  descTexture.Height, 
                  descTexture.DxgiFormat, 
                  descTexture.MipMapCount, 
                  descTexture.ArraySize, 
                  descTexture.Flags, 
                  descTexture.MultiSampleLevel,
                  descTexture.SampleQuality,
                  descTexture.Name)
        { }

        internal Texture2DDX11(
            RenderService renderer,
            uint width,
            uint height,
            Format format = Format.FormatR8G8B8A8Unorm,
            uint mipCount = 1,
            uint arraySize = 1,
            TextureFlags flags = TextureFlags.None,
            AntiAliasLevel aaLevel = AntiAliasLevel.None,
            MSAAQuality msaa = MSAAQuality.Default,
            string name = null)
            : base(renderer, width, height, 1, mipCount, arraySize, aaLevel, msaa, format, flags, name)
        {
            _desc = new Texture2DDesc1()
            {
                Width = Math.Max(width, 1),
                Height = Math.Max(height, 1),
                MipLevels = mipCount,
                ArraySize = Math.Max(arraySize, 1),
                Format = format,
                BindFlags = (uint)GetBindFlags(),
                CPUAccessFlags = (uint)GetAccessFlags(),
                SampleDesc = new SampleDesc()
                {
                    Count = (uint)aaLevel,
                    Quality = (uint)msaa,
                },
                Usage = GetUsageFlags(),
                MiscFlags = (uint)GetResourceFlags(),
                TextureLayout = TextureLayout.None,
            };
        }

        public Texture2DProperties Get2DProperties()
        {
            return new Texture2DProperties()
            {
                Width = Width,
                Height = Height,
                ArraySize = ArraySize,
                Flags = Flags,
                Format = DataFormat,
                MipMapLevels = MipMapCount,
                MultiSampleLevel = MultiSampleLevel,
                Name = Name,
            };
        }

        protected override unsafe ID3D11Resource* CreateResource(bool resize)
        {
            SubresourceData* subData = null;
            (Device as DeviceDX11).Ptr->CreateTexture2D1(ref _desc, subData, ref NativeTexture);
            return (ID3D11Resource*)NativeTexture;
        }

        protected override void SetSRVDescription(ref ShaderResourceViewDesc1 desc)
        {
            if (_desc.SampleDesc.Count > 1)
            {
                desc.ViewDimension = D3DSrvDimension.D3D101SrvDimensionTexture2Dmsarray;
                desc.Texture2DMSArray = new Tex2DmsArraySrv()
                {
                    ArraySize = _desc.ArraySize,
                    FirstArraySlice = 0,
                };
            }
            else
            {
                desc.ViewDimension = D3DSrvDimension.D3DSrvDimensionTexture2Darray;
                desc.Texture2DArray = new Tex2DArraySrv1()
                {
                    ArraySize = _desc.ArraySize,
                    MipLevels = _desc.MipLevels,
                    MostDetailedMip = 0,
                    FirstArraySlice = 0,
                    PlaneSlice = 0,
                };
            }
        }

        protected override void SetUAVDescription(ref ShaderResourceViewDesc1 srvDesc, ref UnorderedAccessViewDesc1 desc)
        {
            desc.Format = srvDesc.Format;
            desc.ViewDimension = UavDimension.Texture2Darray;
            
            desc.Texture2DArray = new Tex2DArrayUav1()
            {
                ArraySize = _desc.ArraySize,
                FirstArraySlice = srvDesc.Texture2DArray.FirstArraySlice,
                MipSlice = 0,
                PlaneSlice = 0
            };

            desc.Buffer = new BufferUav()
            {
                FirstElement = 0,
                NumElements = _desc.Width * _desc.Height * _desc.ArraySize,
            };
        }

        protected override void UpdateDescription(uint newWidth, uint newHeight, uint newDepth, 
            uint newMipMapCount, uint newArraySize, Format newFormat)
        {
            _desc.ArraySize = newArraySize;
            _desc.Width = newWidth;
            _desc.Height = newHeight;
            _desc.MipLevels = newMipMapCount;
            _desc.Format = newFormat;
        }

        public void Resize(uint newWidth, uint newHeight)
        {
            QueueChange(new TextureResize()
            {
                NewWidth = newWidth,
                NewHeight = newHeight,
                NewMipMapCount = MipMapCount,
                NewArraySize = _desc.ArraySize,
                NewFormat = DxgiFormat,
            });
        }

        public void Resize(uint newWidth, uint newHeight, 
            uint newMipMapCount, 
            uint newArraySize, 
            GraphicsFormat newFormat)
        {
            QueueChange(new TextureResize()
            {
                NewWidth = newWidth,
                NewHeight = newHeight,
                NewMipMapCount = newMipMapCount == 0 ? MipMapCount : newMipMapCount,
                NewArraySize = newArraySize == 0 ? _desc.ArraySize : newArraySize,
                NewFormat = newFormat.ToApi(),
            });
        }

        internal override Usage UsageFlags => _desc.Usage;

        public override bool IsUnorderedAccess => ((BindFlag)_desc.BindFlags & BindFlag.UnorderedAccess) == BindFlag.UnorderedAccess;
    }
}