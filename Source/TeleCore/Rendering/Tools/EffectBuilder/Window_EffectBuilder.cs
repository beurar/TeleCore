using UnityEngine;
using Verse;

namespace TeleCore
{
    internal class Window_EffectBuilder : Window
    {
        internal EffectBuilderWindowContainer content;

        public sealed override Vector2 InitialSize => new Vector2(UI.screenWidth, UI.screenHeight);
        public override float Margin => 5f;

        public Window_EffectBuilder()
        {
            forcePause = true;
            absorbInputAroundWindow = true;
            layer = WindowLayer.Super;

            //
            content = new EffectBuilderWindowContainer(this, new Rect(Vector2.zero, InitialSize), UIElementMode.Static);
        }

        public override void PreOpen()
        {
            base.PreOpen();
            content.Notify_Reopened();
        }

        public override void DoWindowContents(Rect inRect)
        {
            UIEventHandler.Begin();
            content.DrawElement(inRect);
            UIDragNDropper.DrawCurDrag();
            UIEventHandler.End();
        }
    }
}
