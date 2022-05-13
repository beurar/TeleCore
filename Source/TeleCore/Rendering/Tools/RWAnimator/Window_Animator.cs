using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace TeleCore
{
    //TODO: Add way to quick test animation ingame via button and then get back to animation tool
    public class Window_Animator : Window
    {
        //
        internal WindowContainer windowContents;

        public sealed override Vector2 InitialSize => new Vector2(UI.screenWidth, UI.screenHeight);
        public override float Margin => 5f;

        public Window_Animator()
        {
            forcePause = true;
            doCloseX = true;
            doCloseButton = false;
            closeOnClickedOutside = false;
            absorbInputAroundWindow = true;
            closeOnAccept = false;
            closeOnCancel = false;

            layer = WindowLayer.Super;

            //
            windowContents = new WindowContainer(new Rect(Vector2.zero, InitialSize), UIElementMode.Static);
        }

        public override void PreOpen()
        {
            base.PreOpen();
            windowContents.Notify_Reopened();
        }

        public override void DoWindowContents(Rect inRect)
        {
            windowContents.DrawElement(inRect);
        }
    }
}
