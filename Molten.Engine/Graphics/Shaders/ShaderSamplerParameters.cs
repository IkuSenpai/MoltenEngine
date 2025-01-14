﻿namespace Molten.Graphics
{
    public struct ShaderSamplerParameters
    {
        /// <summary>Gets or sets the method to use for resolving a U texture coordinate that is outside the 0 to 1 range.</summary>
        [ShaderNode(ShaderNodeParseType.Enum)]
        public SamplerAddressMode AddressU;

        /// <summary>Gets or sets the method to use for resolving a V texture coordinate that is outside the 0 to 1 range.</summary>
        [ShaderNode(ShaderNodeParseType.Enum)]
        public SamplerAddressMode AddressV;

        /// <summary>Gets or sets the method to use for resolving a W texture coordinate that is outside the 0 to 1 range.</summary>
        [ShaderNode(ShaderNodeParseType.Enum)]
        public SamplerAddressMode AddressW;

        /// <summary>Border color to use if <see cref="SamplerAddressMode.Border"/> is specified 
        /// for AddressU, AddressV, or AddressW. Range must be between 0.0 and 1.0 inclusive.</summary>
        [ShaderNode(ShaderNodeParseType.Color)]
        public Color4 BorderColor;

        /// <summary>A function that compares sampled data against existing sampled data.</summary>
        [ShaderNode(ShaderNodeParseType.Enum)]
        public ComparisonMode Comparison;

        /// <summary>Gets or sets the filtering method to use when sampling a texture.</summary>
        [ShaderNode(ShaderNodeParseType.Enum)]
        public SamplerFilter Filter;

        /// <summary>Clamping value used if <see cref="SamplerFilter.Anisotropic"/> or 
        /// <see cref="SamplerFilter.ComparisonAnisotropic"/> is specified in SamplerFilter. Valid values are between 1 and 16.</summary>
        [ShaderNode(ShaderNodeParseType.UInt32)]
        public uint MaxAnisotropy;

        /// <summary>Upper end of the mipmap range to clamp access to, where 0 is the largest
        ///     and most detailed mipmap level and any level higher than that is less detailed.
        ///     This value must be greater than or equal to MinLOD. To have no upper limit
        ///     on LOD set this to a large value such as D3D11_FLOAT32_MAX.</summary>
        [ShaderNode(ShaderNodeParseType.Float)]
        public float MaxMipMapLod;

        /// <summary>Lower end of the mipmap range to clamp access to, where 0 is the largest and most detailed mipmap level 
        /// and any level higher than that is less detailed.</summary>
        [ShaderNode(ShaderNodeParseType.Float)]
        public float MinMipMapLod;

        /// <summary>Gets or sets the offset from the calculated mipmap level. For example, if Direct3D calculates 
        /// that a texture should be sampled at mipmap level 3 and MipLODBias is 2, then 
        /// the texture will be sampled at mipmap level 5.</summary>
        [ShaderNode(ShaderNodeParseType.Float)]
        public float LodBias;

        public ShaderSamplerParameters(SamplerPreset preset)
        {
            ApplyPreset(preset);
        }

        /// <summary>Gets whether or not the sampler a comparison sampler. This is determined by the <see cref="Filter"/> mode.</summary>
        public bool IsComparisonSampler
        {
            get
            {
                return Filter >= SamplerFilter.ComparisonMinMagMipPoint &&
                    Filter <= SamplerFilter.ComparisonAnisotropic;
            }
        }

        public void ApplyPreset(SamplerPreset preset)
        {
            // Revert to default
            Filter = SamplerFilter.MinMagMipLinear;
            AddressU = SamplerAddressMode.Wrap;
            AddressV = SamplerAddressMode.Wrap;
            AddressW = SamplerAddressMode.Wrap;
            MinMipMapLod = float.MinValue;
            MaxMipMapLod = float.MaxValue;
            LodBias = 0f;
            MaxAnisotropy = 1;
            BorderColor = Color.White;
            Comparison = ComparisonMode.Never;

            // Now apply preset values.
            switch (preset)
            {
                case SamplerPreset.Clamp:
                    AddressU = SamplerAddressMode.Clamp;
                    AddressV = SamplerAddressMode.Clamp;
                    AddressW = SamplerAddressMode.Clamp;
                    break;
            }
        }
    }

    public enum SamplerPreset
    {
        /// <summary>The default blend mode. All address modes are set to <see cref="SamplerAddressMode.Wrap"/></summary>
        Default = 0,

        Clamp = 1,
    }
}
