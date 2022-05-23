using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public class TextureBrowser : UIElement
    {
        private const float _OptionSize = 40;

        //
        private QuickSearchWidget searchWidget;

        //
        private List<WrappedTexture> textureList;
        private Vector2 scrollPos = Vector2.zero;

        private int startIndex, endIndex;
        private int indexRange;

        //
        private bool openFilter = false;

        public override string Label => "Texture Browser";

        private Rect MainRect => Rect.BottomPartPixels(Rect.height - TopRect.height);
        private Rect SearchWidgetRect => MainRect.TopPartPixels(QuickSearchWidget.WidgetHeight);
        private Rect SearchAreaRect => MainRect.BottomPartPixels(MainRect.height - QuickSearchWidget.WidgetHeight).ContractedBy(1);
        private Rect ScrollRect => SearchAreaRect.BottomPartPixels(SearchAreaRect.height - _OptionSize);
        private Rect ScrollRectInner => new Rect(ScrollRect.x, ScrollRect.y, ScrollRect.width, textureList.Count * _OptionSize);
        private Rect InfoRect => SearchAreaRect.TopPartPixels(_OptionSize/2);

        public TextureBrowser(UIElementMode mode) : base(mode)
        {
            Title = "Texture Browser";
            bgColor = TColor.BGP3;
            
            //
            searchWidget = new QuickSearchWidget();
        }

        internal IEnumerable<ModContentPack> AllowedMods => ModDirectoryData.GetAllModsWithTextures().Where(m => TeleCoreMod.Settings.animationSettings.AllowsModInBrowser(m));

        protected override void DrawTopBarExtras(Rect topRect)
        {
            Rect filterButton = topRect.RightPartPixels(topRect.height).ContractedBy(2);
            if(Widgets.ButtonImage(filterButton, TeleContent.BurgerMenu))
            {
                openFilter = !openFilter;

                //Reload
                if (openFilter == false)
                {
                    //
                    TeleCoreMod.Settings.Write();
                    ReloadAssets();
                    CheckSearch();
                }
            }
        }

        protected override void DrawContentsBeforeRelations(Rect inRect)
        {
            //
            searchWidget.OnGUI(SearchWidgetRect, CheckSearch);

            GUI.color = TColor.MenuSectionBGBorderColor;
            Widgets.DrawLineHorizontal(SearchWidgetRect.x, SearchWidgetRect.yMax + 4, SearchWidgetRect.width);
            GUI.color = Color.white;

            if (openFilter)
            {
                Rect filterTop = SearchAreaRect.TopPartPixels(35).Rounded();
                Rect selectionRect = SearchAreaRect.BottomPartPixels(SearchAreaRect.height - filterTop.height).Rounded();
                Widgets.DrawBoxSolid(SearchAreaRect, TColor.WindowBGFillColor);

                Text.Anchor = TextAnchor.MiddleCenter;
                Text.Font = GameFont.Medium;
                GUI.color = TColor.White075;
                Widgets.Label(filterTop, "Select Mods To Browse From");
                GUI.color = Color.white;
                Text.Font = default;
                Text.Anchor = default;

                Listing_Standard listing = new Listing_Standard();
                listing.Begin(selectionRect);

                int i = 0;
                foreach (var mod in ModDirectoryData.GetAllModsWithTextures())
                {
                    bool var = TeleCoreMod.Settings.animationSettings.AllowsModInBrowser(mod);
                    if(i % 2 == 0)
                        listing.DoBGForNext(TColor.White005);

                    listing.CheckboxLabeled(mod.Name, ref var);

                    //Update Settings
                    TeleCoreMod.Settings.animationSettings.SetAllowedModInBrowser(mod, var);
                    i++;
                }
                listing.End();
                return;
            }

            if (textureList == null) return;
            //

            float curY = 0;
            Widgets.BeginScrollView(ScrollRect, ref scrollPos, ScrollRectInner, false);
            startIndex = (int)(scrollPos.y / _OptionSize);
            indexRange = Math.Min((int)(ScrollRect.height / _OptionSize) + 1, textureList.Count);
            endIndex = startIndex + indexRange;
            if (startIndex >= 0 && endIndex <= textureList.Count)
            {
                for (int i = startIndex; i < endIndex; i++)
                {
                    curY = ScrollRect.y + i * _OptionSize;
                    Texture2D tex = (Texture2D) textureList[i].Texture;
                    WidgetRow row = new WidgetRow(Rect.x, curY, gap: 4f);
                    row.Label($"[{i+1}]");
                    row.Icon(tex);
                    row.Label($"{tex.name}");

                    //TooltipHandler.TipRegion(new Rect(ScrollRectInner.x, curY, ScrollRectInner.width, _OptionSize), PathLabelMarked(textureList[i].Path, searchWidget.filter.searchText));

                    var pathLabelResizec = PathLabelResized(textureList[i].Path, ScrollRectInner.width);
                    var pathLabelMarked = PathLabelMarked(pathLabelResizec, searchWidget.filter.searchText);
                    var pathLabelSize = Text.CalcSize(pathLabelMarked);
                    var pathLabelX = pathLabelSize.x > ScrollRectInner.width ? ScrollRectInner.x - (pathLabelSize.x - ScrollRectInner.width) : ScrollRectInner.x;
                    Rect pathLabelRect = new Rect(pathLabelX, curY + WidgetRow.IconSize, pathLabelSize.x, _OptionSize);
                    GUI.color = TColor.White075;
                    TWidgets.DoTinyLabel(pathLabelRect, pathLabelMarked);
                    GUI.color = Color.white;
                    
                    
                    var optionRect = new Rect(Rect.x, curY, Rect.width, _OptionSize);
                    if (Mouse.IsOver(optionRect))
                    {
                        DragAndDropData = textureList[i];
                        Widgets.DrawHighlight(optionRect);
                    }
                }
            }

            Widgets.EndScrollView();

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.LowerRight;
            Widgets.Label(InfoRect, $"[Showing {indexRange} of {textureList.Count} Items]"); //\n[{startIndex}...{endIndex}]
            Text.Anchor = default;
            Text.Font = GameFont.Small;

        }

        private string PathLabelResized(string pathLabel, float width)
        {
            return pathLabel;
        }

        private string PathLabelMarked(string pathLabel, string searchText)
        {
            if (searchText.NullOrEmpty()) return pathLabel;

            var index = pathLabel.IndexOf(searchText, StringComparison.InvariantCultureIgnoreCase);
            if (index == -1) return pathLabel;
            
            var subPart = pathLabel.Substring(index, searchText.Length);
            return pathLabel.Replace(subPart, subPart.Colorize(Color.cyan));
        }

        private static List<KeyValuePair<string, Texture2D>> AllContent;

        private void ReloadAssets()
        {
            AllContent?.Clear();
            AllContent = AllowedMods.SelectMany(m => m.GetContentHolder<Texture2D>().contentList).ToList();
        }

        private void CheckSearch()
        {
            if (AllContent == null)
            {
                ReloadAssets();
            }

            //
            textureList = AllContent.
                Where(t => searchWidget.filter.Matches($"{t.Key} {t.Value.name}"))
                .Select(t => new WrappedTexture(t.Key, t.Value)).ToList();
        }
    }
}
