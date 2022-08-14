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
    public class FleckMoteBrowser : DataBrowser<Def>
    {
        private List<Def> DataCached;

        protected override IEnumerable<ModContentPack> BaseMods => ModDirectoryData.GetAllModsWithDefs();

        public FleckMoteBrowser(UIElementMode mode) : base(mode)
        {
        }

        protected override List<Def> CheckSearch(QuickSearchFilter filter, bool filterChanged)
        {
            if (DataCached.NullOrEmpty() || filterChanged)
            {
                DataCached ??= new List<Def>();
                DataCached.Clear();
                foreach (var contentPack in AllowedMods)
                {
                    foreach (var def in contentPack.AllDefs)
                    {
                        if (def is ThingDef {mote: { }} or FleckDef)
                        {
                            DataCached.Add(def);
                        }
                    }
                }
            }
            return DataCached;
        }

        protected override void DrawElementOption(Rect optionRect, Def def, int i)
        {
            WidgetRow row = new WidgetRow(Rect.x, optionRect.y, gap: 4f);
            row.Label($"[{i + 1}]");
            if (def is ThingDef mote)
            {
                row.Label("[Mote]");
                row.Icon(mote.uiIcon);
                row.Label(mote.defName);
                return;
            }

            if (def is FleckDef fleck)
            {
                row.Label("[Fleck]");
                var texture = TWidgets.TextureForFleckMote(def);
                if (texture != null)
                {
                    row.Icon(texture);
                }
                row.Label(fleck.defName);
            }
        }
    }
}
