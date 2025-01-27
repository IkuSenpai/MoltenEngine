﻿namespace Molten.Graphics
{
    public abstract partial class GraphicsDevice
    {
        public abstract IRenderSurface2D CreateSurface(uint width, uint height, GraphicsFormat format = GraphicsFormat.R8G8B8A8_SNorm, 
            GraphicsResourceFlags flags = GraphicsResourceFlags.GpuWrite,
            uint mipCount = 1, uint arraySize = 1, AntiAliasLevel aaLevel = AntiAliasLevel.None, bool allowMipMapGen = false, string name = null);

        public abstract IDepthStencilSurface CreateDepthSurface(uint width, uint height, DepthFormat format = DepthFormat.R24G8_Typeless,
            GraphicsResourceFlags flags = GraphicsResourceFlags.GpuWrite,
            uint mipCount = 1, uint arraySize = 1, AntiAliasLevel aaLevel = AntiAliasLevel.None, bool allowMipMapGen = false, string name = null);

        /// <summary>Creates a form with a surface which can be rendered on to.</summary>
        /// <param name="formTitle">The title of the form.</param>
        /// <param name="formName">The internal name of the form.</param>
        /// <param name="mipCount">The number of mip map levels of the form surface.</param>
        /// <returns></returns>
        public abstract INativeSurface CreateFormSurface(string formTitle, string formName, uint mipCount = 1);

        /// <summary>Creates a GUI control with a surface which can be rendered on to.</summary>
        /// <param name="controlTitle">The title of the form.</param>
        /// <param name="controlName">The internal name of the control.</param>
        /// <param name="mipCount">The number of mip map levels of the form surface.</param>
        /// <returns></returns>
        public abstract INativeSurface CreateControlSurface(string controlTitle, string controlName, uint mipCount = 1);
    }
}
