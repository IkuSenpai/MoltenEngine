﻿namespace Molten.Graphics
{
    public abstract class ShaderSampler : GraphicsObject
    {
        protected ShaderSampler(GraphicsDevice device, SamplerPreset preset) : 
            base(device, GraphicsBindTypeFlags.Input)
        {
            device.SamplerBank.ApplyPreset(this, preset);
            CheckIfComparisonSampler();
        }

        protected void CheckIfComparisonSampler()
        {
            IsComparisonSampler = Filter >= SamplerFilter.ComparisonMinMagMipPoint &&
                    Filter <= SamplerFilter.ComparisonAnisotropic;
        }

        /// <summary>Gets or sets the method to use for resolving a U texture coordinate that is outside the 0 to 1 range.</summary>
        [ShaderNode(ShaderNodeParseType.Enum)]
        public abstract SamplerAddressMode AddressU { get; set; }

        /// <summary>Gets or sets the method to use for resolving a V texture coordinate that is outside the 0 to 1 range.</summary>
        [ShaderNode(ShaderNodeParseType.Enum)]
        public abstract SamplerAddressMode AddressV { get; set; }

        /// <summary>Gets or sets the method to use for resolving a W texture coordinate that is outside the 0 to 1 range.</summary>
        [ShaderNode(ShaderNodeParseType.Enum)]
        public abstract SamplerAddressMode AddressW { get; set; }

        /// <summary>Border color to use if SharpDX.Direct3D11.TextureAddressMode.Border is specified 
        /// for AddressU, AddressV, or AddressW. Range must be between 0.0 and 1.0 inclusive.</summary>
        [ShaderNode(ShaderNodeParseType.Color)]
        public abstract Color4 BorderColor { get; set; }

        /// <summary>A function that compares sampled data against existing sampled data. 
        /// The function options are listed in SharpDX.Direct3D11.Comparison.</summary>
        [ShaderNode(ShaderNodeParseType.Enum)]
        public abstract ComparisonMode Comparison { get; set; }

        /// <summary>Gets or sets the filtering method to use when sampling a texture (see SharpDX.Direct3D11.Filter).</summary>
        [ShaderNode(ShaderNodeParseType.Enum)]
        public abstract SamplerFilter Filter { get; set; }

        /// <summary>Clamping value used if SharpDX.Direct3D11.Filter.Anisotropic or SharpDX.Direct3D11.Filter.ComparisonAnisotropic 
        /// is specified in SamplerFilter. Valid values are between 1 and 16.</summary>
        [ShaderNode(ShaderNodeParseType.UInt32)]
        public abstract uint MaxAnisotropy { get; set; }

        /// <summary>Upper end of the mipmap range to clamp access to, where 0 is the largest
        ///     and most detailed mipmap level and any level higher than that is less detailed.
        ///     This value must be greater than or equal to MinLOD. To have no upper limit
        ///     on LOD set this to a large value such as D3D11_FLOAT32_MAX.</summary>
        [ShaderNode(ShaderNodeParseType.Float)]
        public abstract float MaxMipMapLod { get; set; }

        /// <summary>Lower end of the mipmap range to clamp access to, where 0 is the largest and most detailed mipmap level 
        /// and any level higher than that is less detailed.</summary>
        [ShaderNode(ShaderNodeParseType.Float)]
        public abstract float MinMipMapLod { get; set; }

        /// <summary>Gets or sets the offset from the calculated mipmap level. For example, if Direct3D calculates 
        /// that a texture should be sampled at mipmap level 3 and MipLODBias is 2, then 
        /// the texture will be sampled at mipmap level 5.</summary>
        [ShaderNode(ShaderNodeParseType.Float)]
        public abstract float LodBias { get; set; }

        /// <summary>Gets whether or not the sampler a comparison sampler. This is determined by the <see cref="Filter"/> mode.</summary>
        public bool IsComparisonSampler { get; private set; }
    }
}