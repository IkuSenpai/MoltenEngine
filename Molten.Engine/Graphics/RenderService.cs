﻿using Molten.Collections;
using Molten.Graphics.Overlays;
using Molten.Threading;

namespace Molten.Graphics
{
    /// <summary>
    /// A base class that custom renderer implementations must inherit in order to be compatible with Molten engine, 
    /// as it provides basic functionality for interacting with the rest of the engine.
    /// </summary>
    public abstract class RenderService : EngineService
    {
        bool _disposeRequested;
        bool _shouldPresent;
        bool _surfaceResizeRequired;
        RenderChain _chain;
        Dictionary<RenderTaskPriority, ThreadedQueue<RenderTask>> _tasks;
        AntiAliasLevel _requestedMultiSampleLevel = AntiAliasLevel.None;

        internal AntiAliasLevel MsaaLevel = AntiAliasLevel.None;
        internal HlslShader FxStandardMesh;
        internal HlslShader FxStandardMesh_NoNormalMap;
        internal GraphicsBuffer StagingBuffer;

        /// <summary>
        /// Creates a new instance of a <see cref="RenderService"/> sub-type.
        /// </summary>
        public RenderService()
        {
            _tasks = new Dictionary<RenderTaskPriority, ThreadedQueue<RenderTask>>();
            RenderTaskPriority[] priorities = Enum.GetValues<RenderTaskPriority>();
            foreach (RenderTaskPriority p in priorities)
                _tasks[p] = new ThreadedQueue<RenderTask>();

            Surfaces = new SurfaceManager(this);
            Overlay = new OverlayProvider();
            Log.WriteLine("Acquiring render chain");
        }

        protected override ThreadingMode OnStart(ThreadManager threadManager)
        {
            _shouldPresent = true;
            return ThreadingMode.SeparateThread;
        }

        protected override void OnStop()
        {
            _shouldPresent = false;
        }

        private void ProcessTasks(RenderTaskPriority priority)
        {
            ThreadedQueue<RenderTask> queue = _tasks[priority];
            Device.Queue.BeginEvent($"Process '{priority}' tasks");
            while (queue.TryDequeue(out RenderTask task))
                task.Process(this);
            Device.Queue.EndEvent();
        }

        /// <summary>
        /// Present's the renderer to it's bound output devices/surfaces.
        /// </summary>
        /// <param name="time"></param>
        protected override sealed void OnUpdate(Timing time)
        {
            /* TODO: 
            *  Procedure:
            *      1) Calculate:
            *          a) Capture the current transform matrix of each object in the render tree
            *          b) Calculate the distance from the scene camera. Store on RenderData
            *          
            *      2) Sort objects by distance from camera:
            *          a) Sort objects into buckets inside RenderTree, front-to-back (reduce overdraw by drawing closest first).
            *          b) Only re-sort a bucket when objects are added or the camera moves
            *          c) While sorting, build up separate bucket list of objects with a transparent material sorted back-to-front (for alpha to work)
            *          
            *  Extras:
            *      3) Reduce z-sorting needed in (2) by adding scene-graph culling (quad-tree, octree or BSP) later down the line.
            *      4) Reduce (3) further by frustum culling the graph-culling results
            * 
            *  NOTES:
                - when SceneObject.IsVisible is changed, queue an Add or Remove operation on the RenderTree depending on visibility. This will remove it from culling/sorting.
            */

            if (_disposeRequested)
            {
                Surfaces.Dispose();
                DisposeBeforeRender();
                return;
            }

            if (!_shouldPresent)
                return;

            Profiler.Begin();
            Device.DisposeMarkedObjects();
            OnPrePresent(time);

            if (_requestedMultiSampleLevel != MsaaLevel)
            {
                // TODO re-create all internal surfaces/textures to match the new sample level.
                // TODO adjust rasterizer mode accordingly (multisample enabled/disabled).
                MsaaLevel = _requestedMultiSampleLevel;
                _surfaceResizeRequired = true;
            }

            ProcessTasks(RenderTaskPriority.StartOfFrame);

            // Perform preliminary checks on active scene data.
            // Also ensure the backbuffer is always big enough for the largest scene render surface.
            foreach (SceneRenderData data in Scenes)
            {
                data.ProcessChanges();

                foreach (RenderCamera camera in data.Cameras)
                {
                    camera.Skip = false;

                    if (camera.Surface == null)
                    {
                        camera.Skip = true;
                        continue;
                    }

                    if (camera.Surface.Width > BiggestWidth)
                    {
                        _surfaceResizeRequired = true;
                        BiggestWidth = camera.Surface.Width;
                    }

                    if (camera.Surface.Height > BiggestHeight)
                    {
                        _surfaceResizeRequired = true;
                        BiggestHeight = camera.Surface.Height;
                    }
                }
            }

            // Update surfaces if dirty. This may involve resizing or changing their format.
            if (_surfaceResizeRequired)
            {
                Surfaces.Rebuild(BiggestWidth, BiggestHeight);
                _surfaceResizeRequired = false;
            }

            
            foreach (SceneRenderData sceneData in Scenes)
            {
                if (!sceneData.IsVisible)
                    continue;

                Device.Queue.BeginEvent("Draw Scene");
                sceneData.PreRenderInvoke(this);
                sceneData.Profiler.Begin();

                // Sort cameras into ascending order-depth.
                sceneData.Cameras.Sort((a, b) =>
                {
                    if (a.OrderDepth > b.OrderDepth)
                        return 1;
                    else if (a.OrderDepth < b.OrderDepth)
                        return -1;
                    else
                        return 0;
                });

                foreach (RenderCamera camera in sceneData.Cameras)
                {
                    if (camera.Skip)
                        continue;

                    Device.Queue.Profiler = camera.Profiler;
                    camera.Profiler.Begin();
                    _chain.Render(sceneData, camera, time);
                    camera.Profiler.End(time);
                    Profiler.Accumulate(camera.Profiler.Previous);
                    sceneData.Profiler.Accumulate(camera.Profiler.Previous);
                    Device.Queue.Profiler = null;
                }

                sceneData.Profiler.End(time);
                sceneData.PostRenderInvoke(this);
                Device.Queue.EndEvent();
            }

            Surfaces.ResetFirstCleared();

            // Present all output surfaces
            OutputSurfaces.For(0, 1, (index, surface) =>
            {
                surface.Present();
                return false;
            });

            OnPostPresent(time);

            ProcessTasks(RenderTaskPriority.EndOfFrame);
            Profiler.End(time);
        }

        internal void RenderSceneLayer(GraphicsQueue cmd, LayerRenderData layerData, RenderCamera camera)
        {
            // TODO To start with we're just going to draw ALL objects in the render tree.
            // Sorting and culling will come later

            foreach (KeyValuePair<Renderable, RenderDataBatch> p in layerData.Renderables)
            {
                // Update transforms.
                // TODO replace below with render prediction to interpolate between the current and target transform.
                foreach (ObjectRenderData data in p.Value.Data)
                    data.RenderTransform = data.TargetTransform;

                // If batch rendering isn't supported, render individually.
                if (!p.Key.BatchRender(cmd, this, camera, p.Value))
                {
                    foreach (ObjectRenderData data in p.Value.Data)
                        p.Key.Render(cmd, this, camera, data);
                }
            }
        }

        /// <summary>
        /// Occurs when the renderer is being initialized.
        /// </summary>
        /// <param name="settings"></param>
        protected override sealed void OnInitialize(EngineSettings settings)
        {
            DisplayManager = OnInitializeDisplayManager(settings.Graphics);
            _chain = new RenderChain(this);

            try
            {
                DisplayManager.Initialize(this, settings.Graphics);
                Log.WriteLine($"Initialized display manager");
            }
            catch (Exception ex)
            {
                Log.Error("Failed to initialize renderer");
                Log.Error(ex, true);
            }

            settings.Graphics.Log(Log, "Graphics");
            MsaaLevel = _requestedMultiSampleLevel = MsaaLevel;
            settings.Graphics.MSAA.OnChanged += MSAA_OnChanged;

            try
            {
                Device = OnInitializeDevice(settings.Graphics, DisplayManager);
                Log.WriteLine("Initialized graphics device");
            }
            catch (Exception ex)
            {
                Log.Error("Failed to initialize graphics device");
                Log.Error(ex, true);
            }

            OnInitializeRenderer(settings);

            uint maxBufferSize = (uint)ByteMath.FromMegabytes(5.5);
            StagingBuffer = Device.CreateStagingBuffer(true, true, maxBufferSize);
            SpriteBatch = new SpriteBatcher(this, 3000, 20);

            LoadDefaultShaders();

            Surfaces.Initialize(BiggestWidth, BiggestHeight);
            Fonts = new SpriteFontManager(Log, this);
            Fonts.Initialize();
        }

        private void LoadDefaultShaders()
        {
            ShaderCompileResult result = Device.LoadEmbeddedShader("Molten.Assets", "gbuffer.mfx");
            FxStandardMesh = result["gbuffer"];
            FxStandardMesh_NoNormalMap = result["gbuffer-sans-nmap"];
        }

        protected abstract void OnInitializeRenderer(EngineSettings settings);

        private void MSAA_OnChanged(AntiAliasLevel oldValue, AntiAliasLevel newValue)
        {
            _requestedMultiSampleLevel = newValue;
        }

        internal SceneRenderData CreateRenderData()
        {
            SceneRenderData rd = new SceneRenderData();
            RenderAddScene task = RenderAddScene.Get();
            task.Data = rd;
            PushTask(RenderTaskPriority.StartOfFrame, task);
            return rd;
        }

        public void DestroyRenderData(SceneRenderData data)
        {
            RenderRemoveScene task = RenderRemoveScene.Get();
            task.Data = data;
            PushTask(RenderTaskPriority.StartOfFrame, task);
        }

        public void PushTask(RenderTaskPriority priority, RenderTask task)
        {
            _tasks[priority].Enqueue(task);
        }

        /// <summary>
        /// Pushes a compute-based shader as a task.
        /// </summary>
        /// <param name="shader">The compute shader to be run inside the task.</param>
        /// <param name="groupsX">The number of X compute thread groups.</param>
        /// <param name="groupsY">The number of Y compute thread groups.</param>
        /// <param name="groupsZ">The number of Z compute thread groups.</param>
        /// <param name="callback">A callback to run once the task is completed.</param>
        public void PushTask(RenderTaskPriority priority, HlslShader shader, uint groupsX, uint groupsY, uint groupsZ, ComputeTaskCompletionCallback callback = null)
        {
            PushComputeTask(priority, shader, new Vector3UI(groupsX, groupsY, groupsZ), callback);
        }

        public void PushComputeTask(RenderTaskPriority priority, HlslShader shader, Vector3UI groups, ComputeTaskCompletionCallback callback = null)
        {
            ComputeTask task = ComputeTask.Get();
            task.Shader = shader;
            task.Groups = groups;
            task.CompletionCallback = callback;
            PushTask(priority, task);
        }

        /// <summary>
        /// Invoked during the first stage of service initialization to allow any api-related objects to be created/initialized prior to renderer initialization.
        /// </summary>
        /// <param name="settings">The <see cref="GraphicsSettings"/> bound to the current engine instance.</param>
        protected abstract GraphicsManager OnInitializeDisplayManager(GraphicsSettings settings);

        protected abstract GraphicsDevice OnInitializeDevice(GraphicsSettings settings, GraphicsManager manager);

        /// <summary>
        /// Occurs before the render engine begins rendering all of the active scenes to the active output(s).
        /// </summary>
        /// <param name="time">A timing instance.</param>
        protected abstract void OnPrePresent(Timing time);

        /// <summary>
        /// Occurs after render presentation is completed and profiler timing has been finalized for the current frame. Useful if you need to do some per-frame cleanup/resetting.
        /// </summary>
        /// <param name="time">A timing instance.</param>
        protected abstract void OnPostPresent(Timing time);

        /// <summary>
        /// Occurs when the current <see cref="RenderService"/> instance/implementation is being disposed.
        /// </summary>
        protected override sealed void OnDispose()
        {
            _disposeRequested = true;
        }

        protected void DisposeBeforeRender()
        {
            base.OnDispose();

            // Dispose of any registered output services.
            OutputSurfaces.For(0, 1, (index, surface) =>
            {
                surface.Dispose();
                return false;
            });

            _chain.Dispose();
            SpriteBatch.Dispose();
            StagingBuffer.Dispose();

            OnDisposeBeforeRender();

            Log.Dispose();
        }

        protected abstract void OnDisposeBeforeRender();

        /// <summary>
        /// Gets profiling data attached to the renderer.
        /// </summary>
        public RenderProfiler Profiler { get; } = new RenderProfiler();

        /// <summary>
        /// Gets the display manager bound to the renderer.
        /// </summary>
        public GraphicsManager DisplayManager { get; private set; }

        /// <summary>
        /// Gets the <see cref="GraphicsDevice"/> bound to the current <see cref="RenderService"/>.
        /// </summary>
        public GraphicsDevice Device { get; private set; }

        /// <summary>
        /// Gets a list of all the output <see cref="ISwapChainSurface"/> instances attached to the renderer. These are automatically presented to the graphics device by the renderer, if active.
        /// </summary>
        public ThreadedList<ISwapChainSurface> OutputSurfaces { get; } = new ThreadedList<ISwapChainSurface>();

        /// <summary>
        /// Gets a list of all the scenes current attached to the renderer.
        /// </summary>
        protected internal List<SceneRenderData> Scenes { get; } = new List<SceneRenderData>();

        /// <summary>
        /// Gets the width of the biggest render surface used so far.
        /// </summary>
        protected uint BiggestWidth { get; private set; } = 1;

        /// <summary>
        /// Gets the height of the biggest render surface used so far.
        /// </summary>
        protected uint BiggestHeight { get; private set; } = 1;

        /// <summary>
        /// Gets the renderer's <see cref="OverlayProvider"/> implementation.
        /// </summary>
        public OverlayProvider Overlay { get; }

        public SurfaceManager Surfaces { get; }

        public abstract ShaderCompiler Compiler { get; }

        internal SpriteBatcher SpriteBatch { get; private set; }

        /// <summary>
        /// Gets the internal <see cref="SpriteFontManager"/> bound to the current <see cref="RenderService"/>.
        /// </summary>
        internal SpriteFontManager Fonts { get; private set; }
    }
}
