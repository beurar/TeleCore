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
    public class TextureBrowser : DataBrowser<WrappedTexture>
    {
        private static List<KeyValuePair<string, Texture2D>> AllContent;

        protected override IEnumerable<ModContentPack> BaseMods => ModDirectoryData.GetAllModsWithTextures();

        public TextureBrowser(UIElementMode mode) : base(mode)
        {
        }

        protected override List<WrappedTexture> CheckSearch(QuickSearchFilter filter, bool filterChanged)
        {
            if (AllContent == null || filterChanged)
            {
                ReloadAssets();
            }

            //
            return AllContent
                .Where(t =>filter.Matches($"{t.Key} {t.Value.name}"))
                .Select(t => new WrappedTexture(t.Key, t.Value)).ToList();
        }

        protected void ReloadAssets()
        {
            AllContent?.Clear();
            AllContent = AllowedMods.SelectMany(m => m.GetContentHolder<Texture2D>().contentList).ToList();
        }

        protected override void DrawElementOption(Rect optionRect, WrappedTexture texture, int i)
        {
            var tex = texture.Texture;
            WidgetRow row = new WidgetRow(Rect.x, optionRect.y, gap: 4f);
            row.Label($"[{i + 1}]");
            row.Icon(tex);
            row.Label($"{tex.name}");


            //TooltipHandler.TipRegion(new Rect(ScrollRectInner.x, curY, ScrollRectInner.width, _OptionSize), PathLabelMarked(textureList[i].Path, searchWidget.filter.searchText));

            var pathLabelResizec = PathLabelResized(DataList[i].Path, optionRect.width);
            var pathLabelMarked = PathLabelMarked(pathLabelResizec, SearchWidget.filter.searchText);
            var pathLabelSize = Text.CalcSize(pathLabelMarked);
            var pathLabelX = pathLabelSize.x > optionRect.width
                ? optionRect.x - (pathLabelSize.x - optionRect.width)
                : optionRect.x;
            Rect pathLabelRect = new Rect(pathLabelX, optionRect.y + WidgetRow.IconSize, pathLabelSize.x, optionRect.height);
            GUI.color = TColor.White075;
            TWidgets.DoTinyLabel(pathLabelRect, pathLabelMarked);
            GUI.color = Color.white;
        }

        //
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
    }
}
