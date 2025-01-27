﻿using Molten.Input;
using Molten.UI;

namespace Molten
{
    public class CameraInputTracker
    {
        float _dragThreshold = 10; // Pixels
        Vector2F _dragDistance;
        Vector2F _delta;
        Vector2I _iDelta;
        Vector2F _curPos;
        Vector2F _dragDefecit;

        CameraComponent _parent;
        IPickable<Vector2F> _pressed = null;
        IPickable<Vector2F> _held = null;
        IPickable<Vector2F> _hovered = null;
        IPickable<Vector2F> _dragging = null;

        /// <summary>
        /// The button set ID, or finger ID.
        /// </summary>
        public int SetID { get; }

        /// <summary>
        /// Gets the button being tracked by the current <see cref="CameraInputTracker"/>.
        /// </summary>
        public PointerButton Button { get; }

        /// <summary>
        /// Gets the pointing device that the current <see cref="CameraInputTracker"/> is tracking.
        /// </summary>
        public PointingDevice Device { get; }

        /// <summary>
        /// The current position of the tracked pointer.
        /// </summary>
        public Vector2F Position => _curPos;

        /// <summary>
        /// The distance moved during the current frame update.
        /// </summary>
        public Vector2F Delta => _delta;

        /// <summary>
        /// An integer version of <see cref="Delta"/>, rounded down.
        /// </summary>
        public Vector2I IntegerDelta => _iDelta;

        /// <summary>
        /// The total distance moved from the initial 'press' location.
        /// </summary>
        public Vector2F DeltaSincePress => _delta;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="pDevice"></param>
        /// <param name="setID"></param>
        /// <param name="button"></param>
        internal CameraInputTracker(CameraComponent parent, PointingDevice pDevice, int setID, PointerButton button, ref Rectangle inputConstraintBounds)
        {
            _parent = parent;
            Device = pDevice;
            SetID = setID;
            Button = button;
            _curPos = pDevice.Position - (Vector2F)inputConstraintBounds.TopLeft;
        }

        internal void Update(Timing time, ref Rectangle constraintBounds)
        {
            Vector2F relPos = Device.Position - (Vector2F)constraintBounds.TopLeft;

            _delta = relPos - _curPos;
            _curPos = relPos;

            _dragDefecit.X = _delta.X + _dragDefecit.X;
            _dragDefecit.Y = _delta.Y + _dragDefecit.Y;

            _iDelta.X = (int)_dragDefecit.X;
            _iDelta.Y = (int)_dragDefecit.Y;

            _dragDefecit.X -= _iDelta.X;
            _dragDefecit.Y -= _iDelta.Y;
 
            IPickable<Vector2F> prevHover = _hovered;

            _hovered = Pick(_curPos, time);

            // Trigger on-leave of previous hover element.
            if (_hovered != prevHover)
                prevHover?.OnLeave(this);

            // Update currently-hovered element
            if (_hovered != null)
            {
                if (prevHover != _hovered)
                    _hovered.OnEnter(this);

                _hovered.OnHover(this);

                if (Device is MouseDevice mouse)
                {
                    // Handle scroll wheel event
                    if (mouse.ScrollWheel.Delta != 0)
                    {
                        // If the current element did not respond to scrolling, go to it's parent.
                        // Repeat this until a parent element responds to scrolling, or we reach the top of the UI tree.
                        IPickable<Vector2F> scrolled = _hovered;
                        while (scrolled != null)
                        {
                            if (scrolled.OnScrollWheel(mouse.ScrollWheel))
                                break;
                            
                            scrolled = scrolled.ParentPickable;
                        }
                    }
                }
            }

            switch (Button)
            {
                case PointerButton.Left:
                    HandleLeftClick();
                    break;

                case PointerButton.Right:
                    // TODO context menu handling
                    break;

                case PointerButton.Middle:
                    // Focus element but don't handle click actions
                    break;
            }
        }

        private IPickable<Vector2F> Pick(Vector2F pos, Timing time)
        {
            if (_parent.Object != null && _parent.Object.Scene != null)
            {
                SceneLayer layer;
                for (int i = _parent.Object.Scene.Layers.Count - 1; i >= 0; i--)
                {
                    layer = _parent.Object.Scene.Layers[i];
                    IReadOnlyList<IPickable<Vector2F>> pickables = layer.GetTracked<IPickable<Vector2F>>();
                    if (pickables == null)
                        continue;

                    for (int j = pickables.Count - 1; j >= 0; j--)
                    {
                        IPickable<Vector2F> picked = pickables[j].Pick(pos, time);
                        if (picked != null)
                            return picked;
                    }
                }
            }

            return null;
        }

        private void HandleLeftClick()
        {
            if (Device.IsDown(Button, SetID))
            {
                if (_pressed == null)
                {
                    if (_hovered != null)
                    {
                        _pressed = _hovered;

                        // Check if focused control needs unfocusing.
                        if (_parent.FocusedPickable != _pressed)
                            _parent.FocusedPickable?.Unfocus();

                        // Trigger press-start event
                        _pressed.Focus();

                        // Register a double press event, but also register a normal pressed event, regardless.
                        if (Device.IsTapped(Button, InputActionType.Double, SetID))
                            _pressed?.OnDoublePressed(this);
                        
                        _pressed.OnPressed(this);
                    }

                    _dragDistance = new Vector2F();
                }
                else
                {
                    // Update dragging
                    _dragDistance += _delta;

                    float distDragged = Math.Abs(_dragDistance.Length());
                    if (distDragged >= _dragThreshold)
                    {
                        if (_pressed != null)
                        {
                            if (_dragging == null)
                            {
                                if (_pressed.Contains(Position))
                                {
                                    _dragging = _pressed;

                                    // TODO perform start of drag-drop if element allows being drag-dropped
                                }
                            }

                            _dragging?.OnDragged(this);
                        }
                    }
                }
            }
            else // Handle button release
            {
                Release();
            }
        }

        /// <summary>
        /// Releases tracker state and calls the appropriate <see cref="UIElement"/> callbacks to correctly release state.
        /// </summary>
        internal void Release()
        {
            if (_pressed != null)
            {
                bool inside = _pressed.Contains(_curPos);
                _pressed.OnReleased(this, !inside);

                if (_dragging != null)
                {
                    // TODO perform drop action of drag-drop, if element allows being drag-dropped and target can receive drag-drop actions.
                }
            }

            _pressed = null;
            _dragging = null;
            _held = null;
        }
    }
}

