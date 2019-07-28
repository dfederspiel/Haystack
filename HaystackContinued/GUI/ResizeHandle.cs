using UnityEngine;

namespace HaystackReContinued
{
    public class ResizeHandle
    {
        private bool resizing;
        private Vector2 lastPosition = new Vector2(0, 0);
        private const float resizeBoxSize = 18;
        private const float resizeBoxMargin = 2;

        internal void Draw(ref Rect winRect)
        {

            var resizer = new Rect(winRect.width - resizeBoxSize - resizeBoxMargin, resizeBoxMargin, resizeBoxSize, resizeBoxSize);
            GUI.Box(resizer, "//", Resources.resizeBoxStyle);

            if (!Event.current.isMouse)
            {
                return;
            }

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                resizer.Contains(Event.current.mousePosition))
            {
                this.resizing = true;
                this.lastPosition.x = Input.mousePosition.x;
                this.lastPosition.y = Input.mousePosition.y;

                Event.current.Use();
            }
        }

        internal void DoResize(ref Rect winRect)
        {
            if (!this.resizing)
            {
                return;
            }

            if (Input.GetMouseButton(0))
            {
                var deltaX = Input.mousePosition.x - this.lastPosition.x;
                var deltaY = Input.mousePosition.y - this.lastPosition.y;

                //Event.current.delta does not make resizing very smooth.

                this.lastPosition.x = Input.mousePosition.x;
                this.lastPosition.y = Input.mousePosition.y;

                winRect.xMax += deltaX;
                winRect.yMin -= deltaY;

                if (Event.current.isMouse)
                {
                    Event.current.Use();
                }
            }

            if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
            {
                this.resizing = false;

                Event.current.Use();
            }
        }
    } // ResizeHandle
}