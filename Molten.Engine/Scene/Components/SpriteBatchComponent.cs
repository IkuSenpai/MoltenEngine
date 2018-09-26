﻿using Molten.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Molten
{
    public class SpriteBatchComponent : SceneComponent
    {
        ISpriteRenderable _renderable;
        bool _visible = true;
        bool _inScene = false;
        ObjectRenderData _data;

        protected override void OnInitialize(SceneObject obj)
        {
            _data = new ObjectRenderData();

            AddToScene(obj);
            obj.OnRemovedFromScene += Obj_OnRemovedFromScene;
            obj.OnAddedToScene += Obj_OnAddedToScene;

            base.OnInitialize(obj);
        }

        private void AddToScene(SceneObject obj)
        {
            if (obj.Engine.Renderer != null)
            {
                if (_renderable == null)
                    _renderable = obj.Engine.Renderer.Resources.CreateSpriteRenderable();
                else if (_inScene || _renderable.SpriteCache == null)
                    return;
            }
            else
            {
                return;
            }

            // Add mesh to render data if possible.
            if (_visible && obj.Scene != null)
            {
                obj.Scene.RenderData.AddObject(_renderable, _data, obj.Layer.Data);
                _inScene = true;
            }
        }

        private void RemoveFromScene(SceneObject obj)
        {
            if (!_inScene || _renderable == null ||_renderable.SpriteCache == null)
                return;

            if (obj.Scene != null || _visible)
            {
                obj.Scene.RenderData.RemoveObject(_renderable, _data, obj.Layer.Data);
                _inScene = false;
            }
        }

        protected override void OnDestroy(SceneObject obj)
        {
            obj.OnRemovedFromScene -= Obj_OnRemovedFromScene;
            obj.OnAddedToScene -= Obj_OnAddedToScene;
            RemoveFromScene(obj);

            // Reset State
            _renderable = null;
            _visible = true;

            base.OnDestroy(obj);
        }

        private void Obj_OnAddedToScene(SceneObject obj, Scene scene, SceneLayer layer)
        {
            AddToScene(obj);
        }

        private void Obj_OnRemovedFromScene(SceneObject obj, Scene scene, SceneLayer layer)
        {
            RemoveFromScene(obj);
        }

        public override void OnUpdate(Timing time)
        {
            _data.TargetTransform = Object.Transform.Global;
        }

        /// <summary>The mesh that should be drawn at the location of the component's parent object.</summary>
        public SpriteBatchCache SpriteCache
        {
            get => _renderable.SpriteCache;
            set
            {
                if (_renderable != value)
                {
                    RemoveFromScene(Object);
                    _renderable.SpriteCache = value;
                    AddToScene(Object);
                }
            }
        }

        public override bool IsVisible
        {
            get => _visible;
            set
            {
                if (_visible != value)
                {
                    _visible = value;

                    if (_visible)
                        AddToScene(Object);
                    else
                        RemoveFromScene(Object);
                }
            }
        }
    }
}