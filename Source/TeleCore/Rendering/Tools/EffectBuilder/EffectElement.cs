using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public class EffectElement : UIElement
    {
        private Def def;
        private ThingDef moteDef;
        private FleckDef fleckDef;

        //DataSet
        public bool IsMote => moteDef != null;
        public bool IsFleck => fleckDef != null;

        public EffectCanvas ParentCanvas => (EffectCanvas)_parent;
        public Vector2 DrawSize => new Vector2(20, 20);

        private Vector2 ZoomedSize => DrawSize * ParentCanvas.CanvasZoomScale;
        private Vector2 ZoomedPos => Position * ParentCanvas.CanvasZoomScale;
        private Vector2 TruePos => ParentCanvas.Origin + (ZoomedPos);
        private Vector2 RectPosition => TruePos - ZoomedSize / 2f;
        private Rect ElementRect => new Rect(RectPosition, ZoomedSize);

        public EffectElement(Rect rect, Def def) : base(rect, UIElementMode.Dynamic)
        {
            //
            bgColor = Color.clear;
            hasTopBar = false;

            //
            this.def = def;
            if (def is ThingDef mote)
                moteDef = mote;
            if (def is FleckDef fleck)
                fleckDef = fleck;

        }

        protected override void HandleEvent_Custom(Event ev, bool inContext = false)
        {
            /*
            if (!IsFocused) return;

            var mv = ev.mousePosition;

            //
            if (ElementRect.Contains(mv))
            {
                if (ev.type == EventType.MouseDown)
                {
                    if (ev.button == 0)
                    {
                        oldPos ??= Position;
                        UIEventHandler.StartFocusForced(this);
                    }
                }
            }

            //
            if (IsFocused && ev.type == EventType.MouseDrag)
            {
                if (oldPos != null)
                {
                    var dragDiff = CurrentDragDiff;
                    dragDiff /= ParentCanvas.CanvasZoomScale;
                    Position = oldPos.Value + dragDiff;
                }
            }

            //
            if (ev.type == EventType.MouseUp)
            {
                oldPos = null;
            }
            */
        }

        protected override void DrawContentsBeforeRelations(Rect inRect)
        {
            var labelSize = Text.CalcSize(def.defName);
            Rect drawRect = ElementRect;
            Rect labelRect = new Rect(drawRect.x, drawRect.y - 20, labelSize.x, labelSize.y);
            TWidgets.DoTinyLabel(labelRect, $"[{def.defName}]");
            Widgets.DrawTextureFitted(drawRect, TeleContent.UIDataNode, 1f);
        }
    }
}
