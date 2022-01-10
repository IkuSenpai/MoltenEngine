﻿using System;  namespace Molten.Graphics {     public class RendererGL : RenderService     {         DisplayManagerGL _displayManager;         ResourceManagerGL _resourceManager;          protected override void OnInitializeAdapter(GraphicsSettings settings)         {             _displayManager = new DisplayManagerGL();             _displayManager.Initialize(Log, settings);         }          protected override void OnInitialize(EngineSettings settings, Logger mainLog)         {             base.OnInitialize(settings, mainLog);              Device = new DeviceGL(Log, settings.Graphics, _displayManager);             _resourceManager = new ResourceManagerGL(this);         }          protected override void OnDisposeBeforeRender()         {             Device?.Dispose();             _displayManager?.Dispose();         }          protected override SceneRenderData OnCreateRenderData()         {             throw new NotImplementedException();         }          protected override IRenderChain GetRenderChain()         {             return new RenderChainGL(this);         }          protected override void OnPreRenderScene(SceneRenderData sceneData, Timing time)         {             throw new NotImplementedException();         }          protected override void OnPostRenderScene(SceneRenderData sceneData, Timing time)         {             throw new NotImplementedException();         }          protected override void OnPreRenderCamera(SceneRenderData sceneData, RenderCamera camera, Timing time)         {             throw new NotImplementedException();         }          protected override void OnPostRenderCamera(SceneRenderData sceneData, RenderCamera camera, Timing time)         {             throw new NotImplementedException();         }          protected override void OnRebuildSurfaces(int requiredWidth, int requiredHeight)         {             throw new NotImplementedException();         }          protected override void OnPrePresent(Timing time)         {             throw new NotImplementedException();         }          protected override void OnPostPresent(Timing time)         {             throw new NotImplementedException();         }          public string Namer => null;          public override IComputeManager Compute => null;          public override string Name => "OpenGL";          public override IDisplayManager DisplayManager => throw new NotImplementedException();          public override IResourceManager Resources => _resourceManager;          internal DeviceGL Device { get; private set; }     } } 