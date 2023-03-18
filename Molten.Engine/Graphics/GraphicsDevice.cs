﻿using System.Reflection;
using System.Runtime.CompilerServices;
using Molten.Collections;
using Molten.IO;

namespace Molten.Graphics
{
    /// <summary>
    /// The base class for an API-specific implementation of a graphics device, which provides command/resource access to a GPU.
    /// </summary>
    public abstract partial class GraphicsDevice : EngineObject
    {
        long _allocatedVRAM;
        ThreadedQueue<GraphicsObject> _objectsToDispose;
        Dictionary<Type, Dictionary<StructKey, GraphicsObject>> _objectCache;

        /// <summary>
        /// Creates a new instance of <see cref="GraphicsDevice"/>.
        /// </summary>
        /// <param name="settings">The <see cref="GraphicsSettings"/> to bind to the device.</param>
        /// <param name="log">The <see cref="Logger"/> to use for outputting information.</param>
        protected GraphicsDevice(RenderService renderer, GraphicsSettings settings)
        {
            Settings = settings;
            Renderer = renderer;
            Log = renderer.Log;
            _objectsToDispose = new ThreadedQueue<GraphicsObject>();
            _objectCache = new Dictionary<Type, Dictionary<StructKey, GraphicsObject>>();
        }

        internal void Initialize()
        {
            OnInitialize();

            ShaderSamplerParameters samplerParams = new ShaderSamplerParameters(SamplerPreset.Default);
        }

        protected abstract void OnInitialize();

        internal void DisposeMarkedObjects()
        {
            while (_objectsToDispose.TryDequeue(out GraphicsObject obj))
                obj.GraphicsRelease();
        }

        public void MarkForRelease(GraphicsObject pObject)
        {
            if (IsDisposed)
                pObject.GraphicsRelease();
            else
                _objectsToDispose.Enqueue(pObject);
        }

        protected override void OnDispose()
        {
            DisposeMarkedObjects();
            Cmd?.Dispose();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objKey"></param>
        /// <param name="newObj"></param>
        public T CacheObject<T>(StructKey objKey, T newObj)
            where T : GraphicsObject
        {
            if (!_objectCache.TryGetValue(typeof(T), out Dictionary<StructKey, GraphicsObject> objects))
            {
                objects = new Dictionary<StructKey, GraphicsObject>();
                _objectCache.Add(typeof(T), objects);
            }

            if (newObj != null)
            {
                foreach (StructKey key in objects.Keys)
                {
                    if (key.Equals(objKey))
                    {
                        // Dispose of the new object, we found an existing match.
                        newObj.Dispose();
                        return objects[key] as T;
                    }
                }

                // If we reach here, object has no match in the cache. Add it
                objects.Add(objKey.Clone(), newObj);
            }

            return newObj;
        }

        /// <summary>Track a VRAM allocation.</summary>
        /// <param name="bytes">The number of bytes that were allocated.</param>
        public void AllocateVRAM(long bytes)
        {
            Interlocked.Add(ref _allocatedVRAM, bytes);
        }

        /// <summary>Track a VRAM deallocation.</summary>
        /// <param name="bytes">The number of bytes that were deallocated.</param>
        public void DeallocateVRAM(long bytes)
        {
            Interlocked.Add(ref _allocatedVRAM, -bytes);
        }

        /// <summary>
        /// Requests a new <see cref="ShaderSampler"/> from the current <see cref="GraphicsDevice"/>, with the implementation's default sampler settings.
        /// </summary>
        /// <param name="parameters">The parameters to use when creating the new <see cref="ShaderSampler"/>.</param>
        /// <returns></returns>
        public ShaderSampler CreateSampler(ref ShaderSamplerParameters parameters)
        {
            StructKey<ShaderSamplerParameters> key = new StructKey<ShaderSamplerParameters>(ref parameters);
            ShaderSampler newSampler = OnCreateSampler(ref parameters);
            ShaderSampler result = CacheObject(key, newSampler);

            if (result != newSampler)
            {
                newSampler.Dispose();
                key.Dispose();
            }

            return result;
        }


        protected abstract ShaderSampler OnCreateSampler(ref ShaderSamplerParameters parameters);

        internal HlslPass CreateShaderPass(HlslShader shader, string name = null)
        {
            return OnCreateShaderPass(shader, name);
        }

        protected abstract HlslPass OnCreateShaderPass(HlslShader shader, string name);

        public IVertexBuffer CreateVertexBuffer<T>(T[] data, BufferFlags flags = BufferFlags.GpuRead)
            where T : unmanaged, IVertexType
        {
            return CreateVertexBuffer(flags, (uint)data.Length, data);
        }

        public abstract IVertexBuffer CreateVertexBuffer<T>(BufferFlags flags, uint numVertices, T[] initialData = null)
            where T : unmanaged, IVertexType;

        public IIndexBuffer CreateIndexBuffer(ushort[] data, BufferFlags flags = BufferFlags.GpuRead)
        {
            return CreateIndexBuffer(flags, (uint)data.Length, data);
        }

        public IIndexBuffer CreateIndexBuffer(uint[] data, BufferFlags flags = BufferFlags.GpuRead)
        {
            return CreateIndexBuffer(flags, (uint)data.Length, data);
        }

        public abstract IIndexBuffer CreateIndexBuffer(BufferFlags flags, uint numIndices, ushort[] initialData);

        public abstract IIndexBuffer CreateIndexBuffer(BufferFlags flags, uint numIndices, uint[] initialData = null);

        public IStructuredBuffer CreateStructuredBuffer<T>(T[] data, BufferFlags flags = BufferFlags.GpuRead)
            where T : unmanaged
        {
            return CreateStructuredBuffer(flags, (uint)data.Length, false, true, data);
        }

        public abstract IStructuredBuffer CreateStructuredBuffer<T>(BufferFlags flags, uint numElements, bool allowUnorderedAccess, bool isShaderResource, T[] initialData = null)
            where T : unmanaged;

        public abstract IStagingBuffer CreateStagingBuffer(bool allowCpuRead, bool allowCpuWrite, uint byteCapacity);

        /// <summary>
        /// Loads an embedded shader from the target assembly. If an assembly is not provided, the current renderer's assembly is used instead.
        /// </summary>
        /// <param name="nameSpace"></param>
        /// <param name="filename"></param>
        /// <param name="assembly">The assembly that contains the embedded shadr. If an assembly is not provided, the current renderer's assembly is used instead.</param>
        /// <returns></returns>
        public ShaderCompileResult LoadEmbeddedShader(string nameSpace, string filename, Assembly assembly = null)
        {
            string src = "";
            assembly = assembly ?? typeof(RenderService).Assembly;
            Stream stream = EmbeddedResource.TryGetStream($"{nameSpace}.{filename}", assembly);
            if (stream != null)
            {
                using (StreamReader reader = new StreamReader(stream))
                    src = reader.ReadToEnd();

                stream.Dispose();
            }
            else
            {
                Log.Error($"Attempt to load embedded shader failed: '{filename}' not found in namespace '{nameSpace}' of assembly '{assembly.FullName}'");
                return new ShaderCompileResult();
            }

            return Renderer.Compiler.Compile(src, filename, ShaderCompileFlags.EmbeddedFile, assembly, nameSpace);
        }

        /// <summary>Compiles a set of shaders from the provided source string.</summary>
        /// <param name="source">The source code to be parsed and compiled.</param>
        /// <param name="filename">The name of the source file. Used as a pouint of reference in debug/error messages only.</param>
        /// <returns></returns>
        public ShaderCompileResult CompileShaders(ref string source, string filename = null)
        {
            ShaderCompileFlags flags = ShaderCompileFlags.EmbeddedFile;

            if (!string.IsNullOrWhiteSpace(filename))
            {
                FileInfo fInfo = new FileInfo(filename);
                DirectoryInfo dir = fInfo.Directory;
                flags = ShaderCompileFlags.None;
            }

            return Renderer.Compiler.Compile(source, filename, flags, null, null);
        }

        /// <summary>
        /// Gets the amount of VRAM that has been allocated on the current <see cref="GraphicsDevice"/>. 
        /// <para>For a software or integration device, this may be system memory (RAM).</para>
        /// </summary>
        internal long AllocatedVRAM => _allocatedVRAM;

        /// <summary>
        /// Gets the <see cref="Logger"/> that is bound to the current <see cref="GraphicsDevice"/> for outputting information.
        /// </summary>
        public Logger Log { get; }

        /// <summary>
        /// Gets the <see cref="GraphicsSettings"/> bound to the current <see cref="GraphicsDevice"/>.
        /// </summary>
        public GraphicsSettings Settings { get; }

        /// <summary>
        /// Gets the <see cref="IDisplayAdapter"/> that the current <see cref="GraphicsDevice"/> is bound to.
        /// </summary>
        public abstract IDisplayAdapter Adapter { get; }

        /// <summary>
        /// Gets the <see cref="GraphicsDisplayManager"/> that owns the current <see cref="GraphicsDevice"/>.
        /// </summary>
        public abstract GraphicsDisplayManager DisplayManager { get; }

        /// <summary>
        /// The main <see cref="GraphicsCommandQueue"/> of the current <see cref="GraphicsDevice"/>. This is used for issuing immediate commands to the GPU.
        /// </summary>
        public abstract GraphicsCommandQueue Cmd { get; }

        /// <summary>
        /// Gets the <see cref="RenderService"/> that created and owns the current <see cref="GraphicsDevice"/> instance.
        /// </summary>
        public RenderService Renderer { get; }
    }

    /// <summary>
    /// A more advanced version of <see cref="GraphicsDevice"/> which manages the allocation and releasing of an unsafe object pointer, exposed via <see cref="Ptr"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public unsafe abstract class GraphicsDevice<T> : GraphicsDevice
        where T : unmanaged
    {
        T* _ptr;

        protected GraphicsDevice(RenderService renderer, GraphicsSettings settings, bool allocate) :
            base(renderer, settings)
        {
            if (allocate)
                _ptr = EngineUtil.Alloc<T>();
        }

        protected override void OnDispose()
        {
            EngineUtil.Free(ref _ptr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator T(GraphicsDevice<T> device)
        {
            return *device.Ptr;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator T*(GraphicsDevice<T> device)
        {
            return device._ptr;
        }

        /// <summary>
        /// The underlying, native device pointer.
        /// </summary>
        public T* Ptr => _ptr;

        /// <summary>
        /// Gets a protected reference to the underlying device pointer.
        /// </summary>
        protected ref T* PtrRef => ref _ptr;
    }
}