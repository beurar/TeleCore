using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TeleCore.Rendering.Tools.EffectBuilder
{
    internal class EffectCanvas : BasicCanvas
    {
        public EffectCanvas(UIElementMode mode) : base(mode)
        {

        }

        protected override IEnumerable<FloatMenuOption> RightClickOptions()
        {
            foreach (var rightClickOption in base.RightClickOptions())
            {
                yield return rightClickOption;
            }

            //
            yield return new FloatMenuOption("Add Node", () =>
            {
                
            });
        }

        protected override void DrawTopBarExtras(Rect topRect)
        {
            base.DrawTopBarExtras(topRect);
        }

        protected override void DrawOnCanvas(Rect inRect)
        {
            base.DrawOnCanvas(inRect);
        }
    }
}
