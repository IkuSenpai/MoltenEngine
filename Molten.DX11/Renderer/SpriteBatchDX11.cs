﻿using SharpDX.Direct3D;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;

namespace Molten.Graphics
{
    public class SpriteBatchDX11 : SpriteBatch
    {
        BufferSegment _segment;
        int _vertexCount;
        int _drawnFrom;
        int _drawnTo;
        int _spriteCapacity;

        Material _defaultMaterial;
        Material _defaultNoTextureMaterial;
        Material _defaultLineMaterial;
        Material _defaultCircleMaterial;
        Material _defaultTriMaterial;

        Matrix4F _viewProjection;
        Action<GraphicsPipe, SpriteCluster>[] _clusterFlushes;

        internal SpriteBatchDX11(RendererDX11 renderer, int spriteBufferSize = 2000)
        {
            _spriteCapacity = spriteBufferSize;
            _segment = renderer.DynamicVertexBuffer.Allocate<SpriteVertex>(_spriteCapacity);
            _segment.SetVertexFormat(typeof(SpriteVertex));

            string source = null;
            string namepace = "Molten.Graphics.Assets.sprite.sbm";
            using (Stream stream = EmbeddedResource.GetStream(namepace, typeof(RendererDX11).Assembly))
            {
                using (StreamReader reader = new StreamReader(stream))
                    source = reader.ReadToEnd();
            }

            if (!string.IsNullOrWhiteSpace(source))
            {
                ShaderCompileResult result = renderer.ShaderCompiler.Compile(source, namepace);
                _defaultMaterial = result["material", "sprite-texture"] as Material;
                _defaultNoTextureMaterial = result["material", "sprite-no-texture"] as Material;
                _defaultLineMaterial = result["material", "line"] as Material;
                _defaultCircleMaterial = result["material", "circle"] as Material;
                _defaultTriMaterial = result["material", "triangle"] as Material;
            }

            _clusterFlushes = new Action<GraphicsPipe, SpriteCluster>[4]
            {
                FlushSpriteCluster,
                FlushLineCluster,
                FlushTriangleCluster,
                FlushCircleCluster,
            };
        }

        /// <summary>Disposes of the spritebatch.</summary>
        protected override void OnDispose()
        {
            _segment.Release();
        }

        internal void Begin(Viewport viewBounds)
        {
            ConfigureNewClip(viewBounds.Bounds, false); // Initial clip zone
        }

        /// <summary>Flushes the sprite batch by rendering it's contents.</summary>
        /// <param name="sortMode"></param>
        internal void End(GraphicsPipe pipe, ref Matrix4F viewProjection, RenderSurfaceBase destination)
        {
            //if nothing was added to the batch, don't bother with any draw operations.
            if (_clusterCount == 0)
                return;

            _viewProjection = viewProjection;
            pipe.SetVertexSegment(_segment, 0);

            // Run through all clip zones
            SpriteClipZone clip;
            for (int c = 0; c < _clipCount; c++)
            {
                clip = _clipZones[c];

                if (clip.ClusterCount == 0)
                    continue;

                // Reset to-from counters.
                _drawnFrom = 0;
                _drawnTo = 0;

                if (!clip.Active)
                    clip.ClipBounds = destination.Viewport.Bounds;

                pipe.Rasterizer.SetScissorRectangle(clip.ClipBounds);

                // Flush cluster within current clip-zone.
                int clustersDone = 0;
                bool finishedDrawing = false;
                do
                {
                    _segment.Map(pipe, (buffer, stream) =>
                    {
                        SpriteCluster cluster;
                        do
                        {
                            int cID = clip.ClusterIDs[clustersDone];
                            cluster = _clusterBank[cID];

                            int from = cluster.drawnTo;
                            int remaining = cluster.SpriteCount - from;
                            int canFit = _spriteCapacity - _vertexCount;
                            int to = Math.Min(cluster.SpriteCount, from + canFit);

                            // Assign the start vertex to the cluster
                            cluster.startVertex = _vertexCount;

                            // Process until the end of the cluster, or until the buffer is full
                            int copyCount = to - from;
                            if (copyCount > 0)
                            {
                                stream.WriteRange(cluster.Sprites, from, copyCount);
                                _vertexCount += copyCount;

                                // Update cluster counters
                                cluster.drawnFrom = from;
                                cluster.drawnTo = to;
                            }
                            _drawnTo = clustersDone;


                            // Are we done?
                            if (cluster.drawnTo == cluster.SpriteCount)
                                finishedDrawing = ++clustersDone == clip.ClusterCount;
                            else
                                break;

                        } while (!finishedDrawing);
                    });

                    FlushInternal(pipe, clip);
                } while (!finishedDrawing);
            }

            Reset();
        }

        private void FlushInternal(GraphicsPipe pipe, SpriteClipZone clip)
        {
            SpriteCluster cluster;

            for (int i = _drawnFrom; i <= _drawnTo; i++)
            {
                cluster = _clusterBank[clip.ClusterIDs[i]];
                _clusterFlushes[(int)cluster.Format](pipe, cluster);
            }

            // Reset all counters
            _vertexCount = 0;
            _drawnFrom = _drawnTo;
        }

        private void FlushSpriteCluster(GraphicsPipe pipe, SpriteCluster cluster)
        {
            Material mat = cluster.Material as Material;

            if (cluster.Texture != null)
            {
                mat = mat ?? _defaultMaterial;
                Vector2F texSize = new Vector2F(cluster.Texture.Width, cluster.Texture.Height);
                mat.SpriteBatch.TextureSize.Value = texSize;
                mat.Textures.DiffuseTexture.Value = cluster.Texture;
            }
            else
            {
                mat = mat ?? _defaultNoTextureMaterial;
            }

            mat.Object.Wvp.Value = _viewProjection;

            int startVertex = cluster.startVertex;
            int vertexCount = cluster.drawnTo - cluster.drawnFrom;
            pipe.Draw(mat, vertexCount, PrimitiveTopology.PointList, startVertex);
        }

        private void FlushLineCluster(GraphicsPipe pipe, SpriteCluster cluster)
        {
            _defaultLineMaterial.Object.Wvp.Value = _viewProjection;
            int startVertex = cluster.startVertex;
            int vertexCount = cluster.drawnTo - cluster.drawnFrom;
            pipe.Draw(_defaultLineMaterial, vertexCount, PrimitiveTopology.PointList, startVertex);
        }

        private void FlushTriangleCluster(GraphicsPipe pipe, SpriteCluster cluster)
        {
            _defaultTriMaterial.Object.Wvp.Value = _viewProjection;
            int startVertex = cluster.startVertex;
            int vertexCount = cluster.drawnTo - cluster.drawnFrom;
            pipe.Draw(_defaultTriMaterial, vertexCount, PrimitiveTopology.PointList, startVertex);
        }

        private void FlushCircleCluster(GraphicsPipe pipe, SpriteCluster cluster)
        {
            _defaultCircleMaterial.Object.Wvp.Value = _viewProjection;
            int startVertex = cluster.startVertex;
            int vertexCount = cluster.drawnTo - cluster.drawnFrom;
            pipe.Draw(_defaultCircleMaterial, vertexCount, PrimitiveTopology.PointList, startVertex);
        }

        protected override void Reset()
        {
            base.Reset();
            _drawnFrom = 0;
            _drawnTo = 0;
        }
    }
}