using System.Collections.Generic;
using TeleCore.Rendering;
using UnityEngine;
using Verse;

namespace TeleCore
{
    internal class EffectBuilderWindowContainer : UIElement
    {
        private Window parentWindow;

        private readonly UITopBar topBar;
        private readonly EffectCanvas canvas;

        //
        private readonly FleckMoteBrowser effectBrowser;
        private readonly DefBrowser defBrowser;

        public EffectBuilderWindowContainer(Window parent, Rect rect, UIElementMode mode) : base(rect, mode)
        {
            //
            //bgColor = TColor.WindowBGFillColor;
            //borderColor = TColor.WindowBGBorderColor;

            this.parentWindow = parent;
            this.bgColor = TColor.BGDarker;
            this.borderColor = Color.clear;
            //
            canvas = new EffectCanvas(Vector2.zero, new Vector2(rect.width - 2, rect.height - 2), UIElementMode.Static);
            effectBrowser = new FleckMoteBrowser(new Vector2(900, 0), new Vector2(300, 900), UIElementMode.Dynamic);
            defBrowser = new DefBrowser(new Vector2(700,0), new Vector2(300, 900), UIElementMode.Dynamic);

            //
            var buttonMenus = new List<TopBarButtonMenu>();
            //File
            var fileOptions = new List<TopBarButtonOption>();
            fileOptions.Add(new TopBarButtonOption("New", () =>
            {

            }));
            fileOptions.Add(new TopBarButtonOption("Save/Load", () =>
            {

            }));

            buttonMenus.Add(new TopBarButtonMenu("File", fileOptions));

            //View
            var viewOptions = new List<TopBarButtonOption>();
            buttonMenus.Add(new TopBarButtonMenu("View", viewOptions));

            topBar = new UITopBar(buttonMenus);
            topBar.AddCloseButton(() =>
            {
                parentWindow.Close();
            });
        }

        public void Notify_Reopened()
        {

        }

        protected override void DrawTopBarExtras(Rect topRect)
        {

        }

        protected override void DrawContentsBeforeRelations(Rect inRect)
        {
            Widgets.BeginGroup(inRect);
            {
                //Rect canvasRect = new Rect(0, 0, 900, 900);
                //Rect objectBrowserRect = new Rect(canvasRect.xMax-1, canvasRect.y, 300, canvasRect.height + 1);

                //
                canvas.DrawElement();
                effectBrowser.DrawElement();
                defBrowser.DrawElement();
            }
            Widgets.EndGroup();

            //
            topBar.DrawElement(TopRect);
        }
    }
}
