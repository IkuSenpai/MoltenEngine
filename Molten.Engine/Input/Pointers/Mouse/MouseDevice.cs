﻿using Molten.Graphics;

namespace Molten.Input
{
    //public delegate void MouseEventHandler(MouseDevice mouse, in MouseButtonState state);

    /// <summary>
    /// Represents an implementation of a mouse or pointer device.
    /// </summary>
    public abstract class MouseDevice : PointingDevice
    {
        /// <summary>
        /// Invoked when the mouse performs a vertical scroll action.
        /// </summary>
        public event PointingDeviceHandler OnVScroll;

        /// <summary>
        /// Invoked when the mouse performs a horizontal scroll action.
        /// </summary>
        public event PointingDeviceHandler OnHScroll;

        bool _cursorVisible;

        protected override bool ProcessState(ref PointerState newState, ref PointerState prevState)
        {
            bool result =  base.ProcessState(ref newState, ref prevState);

            switch (newState.Action)
            {
                case InputAction.VerticalScroll:
                    ScrollWheel.SetValues((int)newState.Delta.Y);
                    OnVScroll?.Invoke(this, newState);
                    break;

                case InputAction.HorizontalScroll:
                    ScrollWheel.SetValues((int)newState.Delta.X);
                    OnHScroll?.Invoke(this, newState);
                    break;
            }

            return result;
        }

        protected override bool GetIsDown(ref PointerState state)
        {
            if (state.Button != PointerButton.None)
            {
                return state.Action == InputAction.Pressed ||
                    state.Action == InputAction.Held ||
                    state.Action == InputAction.Moved;
            }

            return false;
        }

        protected override void OnUpdate(Timing time) { }

        /// <summary>
        /// Invoked when cursor visibility has changed.
        /// </summary>
        /// <param name="visible"></param>
        protected abstract void OnSetCursorVisibility(bool visible);

        /// <summary>
        /// Gets the vertical scroll wheel, if one is present. Returns null if not.
        /// </summary>
        public abstract InputScrollWheel ScrollWheel { get; protected set; }

        /// <summary>
        /// Gets the horizontal scroll wheel, if one is present. Returns null if not.
        /// </summary>
        public abstract InputScrollWheel HScrollWheel { get; protected set; }

        /// <summary>Gets or sets whether or not the native mouse cursor is visible.</summary>
        public bool IsCursorVisible
        {
            get => _cursorVisible;
            set
            {
                if(_cursorVisible != value)
                {
                    _cursorVisible = value;
                    OnSetCursorVisibility(value);
                }
            }
        }

        public override PointingDeviceType PointerType => PointingDeviceType.Mouse;
    }
}