using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace TeleCore
{
    internal class WindowContainer : UIElement
    {
        private readonly TimeLineControl timeLine;
        private readonly TextureCanvas canvas;
        private readonly SpriteSheetEditor spriteSheetEditor;
        private readonly ObjectBrowser browser;
        //private readonly ToolBar toolBar;
        private readonly UITopBar topBar;
        //
        private readonly AnimationFileSaveLoader fileSaveLoaderWindow;


        public WindowContainer(Rect rect, UIElementMode mode) : base(rect, mode)
        {
            //
            //bgColor = TColor.WindowBGFillColor;
            //borderColor = TColor.WindowBGBorderColor;

            this.bgColor = TColor.BGDarker;
            this.borderColor = Color.clear;
            //
            timeLine = new TimeLineControl();
            //toolBar = new ToolBar(UIElementMode.Static);
            canvas = new TextureCanvas(UIElementMode.Static);
            canvas.TimeLine = timeLine;
            timeLine.Canvas = canvas;
            //
            spriteSheetEditor = new SpriteSheetEditor(UIElementMode.Static);
            spriteSheetEditor.SetVisibility(UIElementState.Closed);

            //
            browser = new ObjectBrowser(UIElementMode.Static);
            browser.SetVisibility(UIElementState.Closed);

            fileSaveLoaderWindow = new AnimationFileSaveLoader(canvas);

            //
            var buttonMenus = new List<TopBarButtonMenu>();
            //File
            var fileOptions = new List<TopBarButtonOption>();
            fileOptions.Add(new TopBarButtonOption("New",canvas.Reset));
            fileOptions.Add(new TopBarButtonOption("Save/Load", () =>
            {
                Find.WindowStack.Add(fileSaveLoaderWindow);
            }));

            buttonMenus.Add(new TopBarButtonMenu("File", fileOptions));

            //View
            var viewOptions = new List<TopBarButtonOption>();
            viewOptions.Add(new TopBarButtonOption("Texture Slicer", () =>
            {
                spriteSheetEditor.ToggleOpen();
            }));
            viewOptions.Add(new TopBarButtonOption("Texture Browser", () =>
            {
                browser.ToggleOpen();
            }));

            buttonMenus.Add(new TopBarButtonMenu("View", viewOptions));

            topBar = new UITopBar(buttonMenus);

            //toolBar.AddElement(canvas);
            //toolBar.AddElement(spriteSheetEditor, new Vector2(100, 100));
            //toolBar.AddElement(browser);
            //toolBar.AddElement(fileSaveLoader);
        }

        public void Notify_Reopened()
        {
            canvas?.Reset();
        }

        protected override void DrawContentsBeforeRelations(Rect inRect)
        {
            UIEventHandler.CurrentLayer = 0;
            Rect topRect = inRect.TopPart(0.80f).Rounded();
            Rect canvasRect = topRect.LeftPartPixels(900);
            Rect toolBarRect = new Rect(inRect.width - 125, inRect.y, 125, canvas.MetaDataViewRect.height);
            Rect timeLineRect = inRect.BottomPart(0.20f).Rounded();

            //Object Browser
            Rect objectBrowserRect = new Rect(canvas.MetaDataViewRect.x, canvasRect.y - 1, 300, canvasRect.height + 1);
            if (canvas.DrawMetaDataSetting && canvas.Initialized)
                objectBrowserRect = new Rect(canvas.MetaDataViewRect.x, canvas.MetaDataViewRect.yMax - 1, 300, canvasRect.height - canvas.MetaDataViewRect.height + 1);

            //
            Rect spriteSheetEditorRect = new Rect(objectBrowserRect.xMax - 1, canvas.MetaDataViewRect.yMax - 1, inRect.width - objectBrowserRect.xMax, topRect.height - canvas.MetaDataViewRect.height + 1);

            UIEventHandler.Notify_MouseOnScreen(Event.current.mousePosition);

            canvas.DrawElement(canvasRect);
            //toolBar.DrawElement(toolBarRect);
            timeLine.DrawElement(timeLineRect);

            browser.DrawElement(objectBrowserRect);
            spriteSheetEditor.DrawElement(spriteSheetEditorRect);

            //
            topBar.DrawElement(TopRect);

            UIDragNDropper.DrawCurDrag();

            UIEventHandler.Notify_MouseOnScreen(Vector2.zero);
        }
    }
}
