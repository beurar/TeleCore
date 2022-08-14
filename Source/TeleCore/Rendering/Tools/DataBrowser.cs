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
    public class DataBrowser<T> : UIElement
    {
        private const float _OptionSize = 40;

        //
        private readonly QuickSearchWidget searchWidget;

        //
        private List<T> dataList;
        private Vector2 scrollPos = Vector2.zero;

        private int startIndex, endIndex;
        private int indexRange;

        //
        private bool openFilter = false;

        public override string Label => "Data Browser";

        protected QuickSearchWidget SearchWidget => searchWidget;
        protected virtual IEnumerable<ModContentPack> BaseMods => null;
        protected IEnumerable<ModContentPack> AllowedMods => BaseMods?.Where(m => TeleCoreMod.Settings.AllowsModInDataBrowser(typeof(T), m));
        protected List<T> DataList => dataList;

        private Rect MainRect => Rect.BottomPartPixels(Rect.height - TopRect.height);
        private Rect SearchWidgetRect => MainRect.TopPartPixels(QuickSearchWidget.WidgetHeight);
        private Rect SearchAreaRect => MainRect.BottomPartPixels(MainRect.height - QuickSearchWidget.WidgetHeight).ContractedBy(1);
        private Rect ScrollRect => SearchAreaRect.BottomPartPixels(SearchAreaRect.height - _OptionSize);
        private Rect ScrollRectInner => new Rect(ScrollRect.x, ScrollRect.y, ScrollRect.width, dataList.Count * _OptionSize);
        private Rect InfoRect => SearchAreaRect.TopPartPixels(_OptionSize / 2);

        public DataBrowser(UIElementMode mode) : base(mode)
        {
            Title = "Data Browser";
            bgColor = TColor.BGP3;

            //
            searchWidget = new QuickSearchWidget();
        }

        protected override void DrawTopBarExtras(Rect topRect)
        {
            Rect filterButton = topRect.RightPartPixels(topRect.height).ContractedBy(2);
            if (Widgets.ButtonImage(filterButton, TeleContent.BurgerMenu))
            {
                openFilter = !openFilter;

                //Reload
                if (openFilter == false)
                {
                    //
                    CheckSearch(searchWidget.filter, true);
                }
            }
        }

        protected virtual void DrawElementOption(Rect optionRect, T element, int index)
        {

        }

        protected override void DrawContentsBeforeRelations(Rect inRect)
        {
            //
            searchWidget.OnGUI(SearchWidgetRect, DoSearchOnGUI);

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
                foreach (var mod in BaseMods)
                {
                    bool var = TeleCoreMod.Settings.AllowsModInDataBrowser(typeof(T), mod);
                    if (i % 2 == 0)
                        listing.DoBGForNext(TColor.White005);

                    listing.CheckboxLabeled(mod.Name, ref var);

                    //Update Settings
                    TeleCoreMod.Settings.SetDataBrowserSettings(typeof(T), mod.Name, var);
                    i++;
                }
                listing.End();
                return;
            }

            if (dataList == null) return;
            //

            float curY = 0;
            Widgets.BeginScrollView(ScrollRect, ref scrollPos, ScrollRectInner, false);
            startIndex = (int)(scrollPos.y / _OptionSize);
            indexRange = Math.Min((int)(ScrollRect.height / _OptionSize) + 1, dataList.Count);
            endIndex = startIndex + indexRange;
            if (startIndex >= 0 && endIndex <= dataList.Count)
            {
                for (int i = startIndex; i < endIndex; i++)
                {
                    curY = ScrollRect.y + i * _OptionSize;
                    var optionRect = new Rect(Rect.x, curY, Rect.width, _OptionSize);
                    DrawElementOption(optionRect, dataList[i], i);

                    //Dragger
                    if (Mouse.IsOver(optionRect))
                    {
                        DragAndDropData = dataList[i];
                        Widgets.DrawHighlight(optionRect);
                    }
                }
            }

            Widgets.EndScrollView();

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.LowerRight;
            Widgets.Label(InfoRect, $"[Showing {indexRange} of {dataList.Count} Items]");
            Text.Anchor = default;
            Text.Font = GameFont.Small;

        }

        private void DoSearchOnGUI()
        {
           dataList = CheckSearch(searchWidget.filter, true);
        }

        protected virtual List<T> CheckSearch(QuickSearchFilter filter, bool filterChanged)
        {
            return null;
        }
    }
}
