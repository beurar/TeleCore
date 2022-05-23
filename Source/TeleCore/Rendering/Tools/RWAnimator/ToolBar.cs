using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public class ToolBar : UIElement
    {
        public ToolBar(UIElementMode mode) : base(mode)
        {
        }

        public ToolBar(Rect rect, UIElementMode mode) : base(rect, mode)
        {
        }

        public ToolBar(Vector2 pos, Vector2 size, UIElementMode mode) : base(pos, size, mode)
        {
        }

        protected override void Notify_AddedElement(UIElement newElement)
        {
            newElement.ToggleOpen();
        }

        protected override void DrawContentsBeforeRelations(Rect inRect)
        {
            Widgets.BeginGroup(inRect);
            List<ListableOption> list = new List<ListableOption>();
            foreach (UIElement element in TextureElements)
            {
                list.Add(new ListableOption(element.Label, () => { element.ToggleOpen(); }));
            }
            OptionListingUtility.DrawOptionListing(new Rect(0, 0, inRect.width, inRect.height), list);
            Widgets.EndGroup();
        }
    }
}
