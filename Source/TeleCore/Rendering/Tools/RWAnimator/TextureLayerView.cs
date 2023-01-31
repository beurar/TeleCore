using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace TeleCore
{
    internal class TextureLayerView : UIElement
    {
        private UIElement parentContainer;
        private ElementScroller internalScroller;

        public TextureElement ActiveElement => internalScroller.SelectedElement as TextureElement;

        public TextureLayerView(UIElement parentContainer) : base(UIElementMode.Static)
        {
            this.parentContainer = parentContainer;
            internalScroller = new ElementScroller(parentContainer, UIElementMode.Static);
        }

        public void Notify_SelectIndex(int index)
        {
            internalScroller.Notify_SelectIndex(index);
        }

        protected override void HandleEvent_Custom(Event ev, bool inContext)
        {
            base.HandleEvent_Custom(ev);
        }

        protected override void DrawContentsBeforeRelations(Rect inRect)
        {
            Rect rect = new Rect(inRect.x - 1, inRect.y, inRect.width + 2, inRect.height);
            internalScroller.DrawElement(rect);
        }
    }
}
