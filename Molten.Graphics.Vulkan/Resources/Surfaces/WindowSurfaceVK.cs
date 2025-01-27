﻿using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;
using Silk.NET.GLFW;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Image = Silk.NET.Vulkan.Image;

namespace Molten.Graphics.Vulkan
{
    internal unsafe class WindowSurfaceVK : RenderSurface2DVK, INativeSurface
    {
        struct BackBuffer
        {
            internal Image Texture;

            internal ImageView View;

            internal BackBuffer(Image texture, ImageView view)
            {
                Texture = texture;
                View = view;
            }
        }

        public event WindowSurfaceHandler OnHandleChanged;
        public event WindowSurfaceHandler OnParentChanged;
        public event WindowSurfaceHandler OnClose;
        public event WindowSurfaceHandler OnMinimize;
        public event WindowSurfaceHandler OnRestore;
        public event WindowSurfaceHandler OnFocusGained;
        public event WindowSurfaceHandler OnFocusLost;

        GraphicsQueueVK _presentQueue;
        WindowHandle* _window;
        PresentModeKHR _mode;
        SurfaceCapabilitiesKHR _cap;
        SwapchainKHR _swapChain;
        BackBuffer[] _backBuffer;

        string _title;
        uint _curChainSize;

        ColorSpaceKHR _colorSpace = ColorSpaceKHR.SpaceSrgbNonlinearKhr;
        Format _format;

        KhrSwapchain _extSwapChain;

        internal unsafe WindowSurfaceVK(DeviceVK device, string title, TextureDimensions dimensions,
            GraphicsResourceFlags flags = GraphicsResourceFlags.None,
            GraphicsFormat format = GraphicsFormat.B8G8R8A8_UNorm,
            PresentModeKHR presentMode = PresentModeKHR.MailboxKhr,
            string name = null) : 
            base(device, dimensions, AntiAliasLevel.None, MSAAQuality.Default, format, flags, false, name)
        {
            _title = title;
            _mode = presentMode;
        }

        protected override void CreateImage()
        {
            DeviceVK device = Device as DeviceVK;
            _format = ResourceFormat.ToApi();
            RendererVK renderer = Device.Renderer as RendererVK;

            KhrSurface extSurface = renderer.GetInstanceExtension<KhrSurface>();
            if (extSurface == null)
            {
                renderer.Log.Error($"VK_KHR_surface extension is unsupported. Unable to initialize WindowSurfaceVK");
                return;
            }

            _extSwapChain = device.GetExtension<KhrSwapchain>();
            if (_extSwapChain == null)
            {
                renderer.Log.Error($"VK_KHR_swapchain extension is unsupported. Unable to initialize WindowSurfaceVK");
                return;
            }

            renderer.GLFW.WindowHint(WindowHintClientApi.ClientApi, ClientApi.NoApi);
            renderer.GLFW.WindowHint(WindowHintBool.Resizable, true);
            _window = renderer.GLFW.CreateWindow((int)Width, (int)Height, _title, null, null);

            VkHandle instanceHandle = new VkHandle(renderer.Instance->Handle);
            VkNonDispatchableHandle surfaceHandle = new VkNonDispatchableHandle();

            Result r = (Result)renderer.GLFW.CreateWindowSurface(instanceHandle, _window, null, &surfaceHandle);
            if (!r.Check(renderer))
                return;

            Native = new SurfaceKHR(surfaceHandle.Handle);
            _presentQueue = renderer.NativeDevice.FindPresentQueue(this);

            if (_presentQueue == null)
            {
                renderer.Log.Error($"No command queue found to present window surface");
                return;
            }

            // Check surface capabilities
            r = extSurface.GetPhysicalDeviceSurfaceCapabilities(device.Adapter, Native, out _cap);
            if (!r.Check(renderer))
                return;

            if (!IsFormatSupported(extSurface, ResourceFormat, _colorSpace))
            {
                renderer.Log.Error($"Surface format '{ResourceFormat}' with a '{_colorSpace}' color-space is not supported");
                return;
            }

            _mode = ValidatePresentMode(extSurface, _mode);
            ValidateBackBufferSize();

            // TODO Set properties for swapchain image in SetCreateInfo() override 
            base.CreateImage();

            r = CreateSwapChain();
            if (r.Check(renderer))
                _backBuffer = GetBackBufferImages();
        }

        private Result CreateSwapChain()
        {
            DeviceVK device = Device as DeviceVK;
            SwapchainCreateInfoKHR createInfo = new SwapchainCreateInfoKHR()
            {
                SType = StructureType.SwapchainCreateInfoKhr,
                Surface = Native,
                MinImageCount = _curChainSize,
                ImageFormat = ResourceFormat.ToApi(),
                ImageColorSpace = _colorSpace,
                ImageExtent = new Extent2D(Width, Height),
                ImageArrayLayers = 1,
                ImageUsage = ImageUsageFlags.ColorAttachmentBit,
            };

            // Detect swap-chain sharing mode.
            (createInfo.ImageSharingMode, GraphicsQueueVK[] sharingWith) = device.GetSharingMode(device.Queue, _presentQueue);
            uint* familyIndices = stackalloc uint[sharingWith.Length];

            for (int i = 0; i < sharingWith.Length; i++)
                familyIndices[i] = sharingWith[i].FamilyIndex;

            createInfo.QueueFamilyIndexCount = (uint)sharingWith.Length;
            createInfo.PQueueFamilyIndices = familyIndices;
            createInfo.PreTransform = _cap.CurrentTransform;
            createInfo.CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr;
            createInfo.PresentMode = _mode;
            createInfo.Clipped = true;
            createInfo.OldSwapchain = _swapChain;

            return _extSwapChain.CreateSwapchain(device, &createInfo, null, out _swapChain);
        }

        private BackBuffer[] GetBackBufferImages()
        {
            DeviceVK device = Device as DeviceVK;
            RendererVK renderer = Device.Renderer as RendererVK;

            Image[] images = renderer.Enumerate<Image>((count, items) =>
            {
                return _extSwapChain.GetSwapchainImages(device, _swapChain, count, items);
            }, "Swapchain image");

            BackBuffer[] buffer = new BackBuffer[images.Length];
            ImageViewCreateInfo createInfo = new ImageViewCreateInfo()
            {
                SType = StructureType.ImageCreateInfo,
                ViewType = ImageViewType.Type2D,
                Format = _format,
                Components = new ComponentMapping()
                {
                    R = ComponentSwizzle.Identity,
                    G = ComponentSwizzle.Identity,
                    B = ComponentSwizzle.Identity,
                    A = ComponentSwizzle.Identity
                },
                SubresourceRange = new ImageSubresourceRange()
                {
                    AspectMask = ImageAspectFlags.ColorBit,
                    BaseMipLevel = 0,
                    LevelCount = 0,
                    BaseArrayLayer = 0,
                    LayerCount = 1, // Number of array slices
                },
            };

            for (int i = 0; i < images.Length; i++)
            {
                buffer[i].Texture = images[i];
                createInfo.Image = images[i];
                Result r = renderer.VK.CreateImageView(device, &createInfo, null, out buffer[i].View);
                if (!r.Check(device, () => $"Failed to create image view for back-buffer image {i}"))
                    break;
            }

            return buffer;
        }

        private bool IsFormatSupported(KhrSurface extSurface, GraphicsFormat format, ColorSpaceKHR colorSpace)
        {
            DeviceVK device = Device as DeviceVK;
            SurfaceFormatKHR[] supportedFormats = (Device.Renderer as RendererVK).Enumerate<SurfaceFormatKHR>((count, items) =>
            {
                return extSurface.GetPhysicalDeviceSurfaceFormats(device.Adapter, Native, count, items);
            }, "surface format");

            Format vkFormat = format.ToApi();

            for(int i = 0; i < supportedFormats.Length; i++)
            {
                if (supportedFormats[i].Format == vkFormat && supportedFormats[i].ColorSpace == colorSpace)
                    return true;
            }

            return false;
        }

        private PresentModeKHR ValidatePresentMode(KhrSurface extSurface, PresentModeKHR requested)
        {
            DeviceVK device = Device as DeviceVK;
            PresentModeKHR[] supportedModes = (Device.Renderer as RendererVK).Enumerate<PresentModeKHR>((count, items) =>
            {
                return extSurface.GetPhysicalDeviceSurfacePresentModes(device.Adapter, Native, count, items);
            }, "present mode");

            for (int i = 0; i < supportedModes.Length; i++)
            {
                if (supportedModes[i] == requested)
                    return requested;
            }

            return PresentModeKHR.FifoKhr;
        }

        private void ValidateBackBufferSize()
        {
            BackBufferMode mode = Device.Renderer.Settings.Graphics.BufferingMode;
            if (mode == BackBufferMode.Default)
                _curChainSize = _cap.MinImageCount + 1;
            else
                _curChainSize = (uint)mode;

            if (_cap.MaxImageCount > 0)
                _curChainSize = uint.Clamp(_curChainSize, _cap.MinImageCount, _cap.MaxImageCount);
            else
                _curChainSize = uint.Max(_cap.MinImageCount, _curChainSize);
        }

        protected override void ValidateDimensions(ref TextureDimensions dimensions)
        {
            base.ValidateDimensions(ref dimensions);

            dimensions.Width = uint.Clamp(dimensions.Width, _cap.MinImageExtent.Width, _cap.MaxImageExtent.Width);
            dimensions.Height = uint.Clamp(dimensions.Height, _cap.MinImageExtent.Height, _cap.MaxImageExtent.Height);
        }

        protected override void OnDispose()
        {
            DeviceVK device = Device as DeviceVK;
            if (_swapChain.Handle != 0)
            {
                // Clean up image view handles
                for (int i = 0; i < _backBuffer.Length; i++)
                    (Device.Renderer as RendererVK).VK.DestroyImageView(device, _backBuffer[i].View, null);

                KhrSwapchain extSwapchain = device.GetExtension<KhrSwapchain>();
                extSwapchain?.DestroySwapchain(device, _swapChain, null);
            }

            KhrSurface extSurface = (Device.Renderer as RendererVK).GetInstanceExtension<KhrSurface>();
            extSurface?.DestroySurface(*(Device.Renderer as RendererVK).Instance, Native, null);

            if (_window != null)
                (Device.Renderer as RendererVK).GLFW.DestroyWindow(_window);
        }

        public void Present()
        {
            throw new NotImplementedException();
        }

        public void Dispatch(Action callback)
        {
            throw new NotImplementedException();
        }

        public void Resize(GraphicsPriority priority, uint newWidth, uint newHeight)
        {
            throw new NotImplementedException();
        }

        public void Resize(GraphicsPriority priority, uint newWidth, uint newHeight, uint newMipMapCount, uint newArraySize, GraphicsFormat newFormat)
        {
            throw new NotImplementedException();
        }

        public void GenerateMipMaps(GraphicsPriority priority, Action<GraphicsResource> completionCallback = null)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(GraphicsPriority priority, GraphicsTexture destination, Action<GraphicsResource> completeCallback = null)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(GraphicsPriority priority, uint sourceLevel, uint sourceSlice,
            GraphicsTexture destination, uint destLevel, uint destSlice, 
            Action<GraphicsResource> completeCallback = null)
        {
            throw new NotImplementedException();
        }

        public void SetData(GraphicsPriority priority, TextureData data, uint srcMipIndex, uint srcArraySlice, uint mipCount, uint arrayCount, uint destMipIndex = 0, uint destArraySlice = 0, Action<GraphicsResource> completeCallback = null)
        {
            throw new NotImplementedException();
        }

        public void SetData<T>(GraphicsPriority priority, uint level, T[] data, uint startIndex, uint count, uint pitch, uint arraySlice = 0, Action<GraphicsResource> completeCallback = null) where T : unmanaged
        {
            throw new NotImplementedException();
        }

        public void SetData(GraphicsPriority priority, TextureSlice data, uint mipLevel, uint arraySlice, Action<GraphicsResource> completeCallback = null)
        {
            throw new NotImplementedException();
        }

        public void SetData<T>(GraphicsPriority priority, RectangleUI area, T[] data, uint bytesPerPixel, uint level, uint arrayIndex = 0, Action<GraphicsResource> completeCallback = null) where T : unmanaged
        {
            throw new NotImplementedException();
        }

        public void GetData(GraphicsPriority priority, GraphicsTexture stagingTexture, Action<TextureData> completeCallback = null)
        {
            throw new NotImplementedException();
        }

        public void GetData(GraphicsPriority priority, GraphicsTexture stagingTexture, uint level, uint arrayIndex, Action<TextureSlice> completeCallback = null)
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            if(_window != null)
                (Device.Renderer as RendererVK).GLFW.SetWindowShouldClose(_window, true);
        }

        public bool IsFocused => throw new NotImplementedException();

        public WindowMode Mode { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public nint? ParentHandle { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public nint? WindowHandle => (nint)_window;

        public Rectangle RenderBounds => throw new NotImplementedException();

        /// <inheritdoc/>
        public string Title
        {
            get => _title;
            set
            {
                if (_window != null)
                    (Device.Renderer as RendererVK).GLFW.SetWindowTitle(_window, _title);
            }
        }
        public bool IsVisible { get; set; }

        internal SurfaceKHR Native { get; private set; }
    }
}
