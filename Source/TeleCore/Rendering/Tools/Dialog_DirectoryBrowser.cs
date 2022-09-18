using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public class Dialog_DirectoryBrowser : Window
    {
        private string titleString;
        private DirectoryInfo rootLimit;

        private DirectoryInfo currentDir;
        private Action<DirectoryInfo> selAction;

        public override Vector2 InitialSize => new Vector2(1000, 500);

        public Dialog_DirectoryBrowser()
        {
            titleString = StringCache.DirectoryBrowserTitle;
            forcePause = true;
            doCloseX = true;
            doCloseButton = false;
            closeOnClickedOutside = false;
            absorbInputAroundWindow = true;
            layer = WindowLayer.Super;
        }

        public Dialog_DirectoryBrowser(Action<DirectoryInfo> selAction, string title = null, string rootLimit = null)
        {
            //
            titleString = title ?? StringCache.DirectoryBrowserTitle;
            forcePause = true;
            doCloseX = true;
            doCloseButton = false;
            closeOnClickedOutside = false;
            absorbInputAroundWindow = true;
            layer = WindowLayer.Super;

            this.selAction = selAction;
            if (rootLimit != null)
            {
                this.rootLimit = new DirectoryInfo(rootLimit);
            }
        }

        public Dialog_DirectoryBrowser(Action<DirectoryInfo> selAction, string title = null, DirectoryInfo rootLimit = null)
        {
            //
            titleString = title ?? StringCache.DirectoryBrowserTitle;
            forcePause = true;
            doCloseX = true;
            doCloseButton = false;
            closeOnClickedOutside = false;
            absorbInputAroundWindow = true;
            layer = WindowLayer.Super;

            this.selAction = selAction;
            this.rootLimit = rootLimit;
        }

        public override void PostOpen()
        {
            base.PostOpen();
            //
            if (rootLimit == null)
            {
                TLog.Warning("Opened Directory Selection Without Setting Root");
                rootLimit = new DirectoryInfo(GenFilePaths.ModsFolderPath);
                currentDir = new DirectoryInfo(GenFilePaths.ModsFolderPath);
                return;
            }

            currentDir = new DirectoryInfo(rootLimit.FullName);
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect titleRect = inRect.TopPart(0.10f);
            Rect selectionRect = inRect.BottomPart(.90f);
            Rect selRect = selectionRect.ContractedBy(5).Rounded();
            Rect directoryRect = selRect.TopPartPixels(selRect.height - 30);
            Rect confirmButton = selRect.BottomPartPixels(30).RightPartPixels(100);

            Text.Font = GameFont.Medium;
            Widgets.Label(titleRect, titleString);
            Text.Font = default;

            DrawDirectory(directoryRect, currentDir);

            //Confirm button
            if (Widgets.ButtonText(confirmButton, "Confirm"))
            {
                selAction?.Invoke(currentDir);
                Close();
            }
        }

        internal void DrawDirectory(Rect inRect, DirectoryInfo directory)
        {
            var subDirs = directory.GetDirectories();
            //
            var conRect = inRect.ContractedBy(8);
            Rect topRect = conRect.TopPartPixels(50);
            Rect extraRect = topRect.TopPartPixels(25);
            Rect curDirReadoutRect = topRect.BottomPartPixels(25).ExpandedBy(8, 0);
            Rect selectionRect = inRect.BottomPartPixels(inRect.height - topRect.height);

            Rect navBackRect = curDirReadoutRect.LeftPartPixels(curDirReadoutRect.height);
            Rect navCurRect = new Rect(navBackRect.xMax + 5, navBackRect.y, topRect.width - navBackRect.width, curDirReadoutRect.height);

            TWidgets.DrawColoredBox(selectionRect, TColor.BlueHueBG, Color.gray, 1);
            selectionRect = selectionRect.ContractedBy(8);

            TWidgets.DrawColoredBox(curDirReadoutRect, TColor.BGDarker, TColor.MenuSectionBGBorderColor, 1);
            GUI.color = TColor.MenuSectionBGBorderColor;
            Widgets.DrawLineVertical(navBackRect.xMax+2, navBackRect.y, navBackRect.height);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(extraRect, $"Root: {rootLimit.Parent?.Name}\\{rootLimit.Name}");
            Widgets.Label(navCurRect, $"{ModDirectoryData.PathCapped(currentDir, rootLimit)}");
            Text.Anchor = default;
            if (currentDir.Name != rootLimit.Name)
            {
                if (Widgets.ButtonImage(navBackRect, TeleContent.OpenMenu))
                {
                    currentDir = directory.Parent;
                }
            }

            Widgets.BeginGroup(selectionRect);
            {
                var curY = 0;
                foreach (var subDir in subDirs)
                {
                    Rect dirSelRect = new Rect(0, curY, selectionRect.width, 20);
                    Widgets.Label(dirSelRect, $"{subDir.Name}");
                    
                    Widgets.DrawHighlightIfMouseover(dirSelRect);
                    if (Widgets.ButtonInvisible(dirSelRect, false))
                    {
                        currentDir = subDir;
                    }
                    curY += 20;
                }
            }
            Widgets.EndGroup();
        }
    }
}
