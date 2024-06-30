using System;
using UnityEngine;

namespace ModCommonUI.Impl
{
}

namespace ModCommonUI
{
    public partial class UISubManager
    {
        public UISubManager? Parent
        {
            get => _parent;
            set
            {
                _parent = value;
                if (value != null)
                {
                    rt.SetParent(value.rt);
                }
            }
        }

        private UISubManager? _parent;

        public void ResetToCenter()
        {
            if (_parent != null)
            {
                _controlRange.position = _parent._controlRange.position;
            }
        }

        public void MoveM(Vector2 offset)
        {
            _controlRange.position += offset;
        }

        public void MoveF(Vector2 offset)
        {
            if (_parent != null)
            {
                _controlRange.position += offset * _parent.ControlRange.size;
            }
        }

        public void ResizeM(Vector2 size)
        {
            _controlRange.size = size;
        }

        public void ResizeF(Vector2 size)
        {
            if (_parent != null)
            {
                _controlRange.size = size * _parent.ControlRange.size;
            }
        }

        public void Update()
        {
            if (_controlRange != new Rect(rt.position, rt.sizeDelta))
            {
                ControlRange = _controlRange;
            }
        }
    }
}