﻿using Molten.Collections;

namespace Molten.Graphics
{
    public abstract class GraphicsResource : GraphicsObject, IGraphicsResource
    {
        ThreadedQueue<IGraphicsResourceTask> _applyTaskQueue;

        protected GraphicsResource(GraphicsDevice device, GraphicsResourceFlags flags) : 
            base(device, (flags.Has(GraphicsResourceFlags.UnorderedAccess) ? GraphicsBindTypeFlags.Output : GraphicsBindTypeFlags.None) |
                (flags.Has(GraphicsResourceFlags.NoShaderAccess) ? GraphicsBindTypeFlags.None : GraphicsBindTypeFlags.Input))
        {
            Flags = flags;
            _applyTaskQueue = new ThreadedQueue<IGraphicsResourceTask>();
        }

        /// <summary>
        /// Queues a <see cref="IGraphicsResourceTask"/> on the current <see cref="GraphicsResource"/>.
        /// </summary>
        /// <param name="priority"></param>
        /// <param name="op"></param>
        protected void QueueTask(GraphicsPriority priority, IGraphicsResourceTask op)
        {
            switch (priority)
            {
                default:
                case GraphicsPriority.Immediate:
                    if (op.Process(Device.Queue, this))
                        Version++;
                    break;

                case GraphicsPriority.Apply:
                    _applyTaskQueue.Enqueue(op);
                    break;

                case GraphicsPriority.StartOfFrame:
                    {
                        RunResourceTask task = RunResourceTask.Get();
                        task.Task = op;
                        task.Resource = this;
                        Device.Renderer.PushTask(RenderTaskPriority.StartOfFrame, task);
                    }
                    break;

                case GraphicsPriority.EndOfFrame:
                    {
                        RunResourceTask task = RunResourceTask.Get();
                        task.Task = op;
                        task.Resource = this;
                        Device.Renderer.PushTask(RenderTaskPriority.EndOfFrame, task);
                    }
                    break;
            }
        }

        /// <summary>Applies any pending changes to the resource, from the specified priority queue.</summary>
        /// <param name="cmd">The graphics queue to use when process changes.</param>
        protected void ApplyChanges(GraphicsQueue cmd)
        {
            if (_applyTaskQueue.Count > 0)
            {
                IGraphicsResourceTask op = null;
                bool invalidated = false;
                while (_applyTaskQueue.TryDequeue(out op))
                    invalidated = op.Process(cmd, this);

                // If the resource was invalided, let the pipeline know it needs to be reapplied by incrementing version.
                if (invalidated)
                    Version++;
            }
        }

        /// <summary>
        /// Takes the next task from the task queue if it matches the specified type.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="result">The task that was dequeued, if any.</param>
        /// <returns></returns>
        protected bool DequeueTaskIfType<T>(out T result)
            where T : IGraphicsResourceTask
        {
            if(_applyTaskQueue.Count > 0 && _applyTaskQueue.IsNext<T>())
            {
                if(_applyTaskQueue.TryDequeue(out IGraphicsResourceTask task))
                {
                    result = (T)task;
                    return true;
                }
            }

            result = default;
            return false;
        }

        protected override void OnApply(GraphicsQueue cmd)
        {
            ApplyChanges(cmd);
            _applyTaskQueue.Clear();
        }

        internal void Clear()
        {
            _applyTaskQueue.Clear();
        }

        /// <summary>
        /// The total size of the resource, in bytes.
        /// </summary>
        public abstract uint SizeInBytes { get; }

        /// <summary>
        /// Gets the resource flags that provided given when the current <see cref="GraphicsResource"/> was created.
        /// </summary>
        public GraphicsResourceFlags Flags { get; }

        internal GraphicsStream Stream { get; set; }

        /// <summary>
        /// Gets the underlying native resource handle.
        /// </summary>
        public abstract unsafe void* Handle { get; }

        /// <summary>Gets the native shader resource view attached to the object.</summary>
        public abstract unsafe void* SRV { get; }

        /// <summary>Gets the native unordered-acess/storage view attached to the object.</summary>
        public abstract unsafe void* UAV { get; }

        /// <summary>
        /// Gets or [protected] sets the <see cref="GraphicsFormat"/> of the resource.
        /// </summary>
        public abstract GraphicsFormat ResourceFormat { get; protected set; }
    }
}
