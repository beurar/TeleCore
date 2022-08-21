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
    public class SubMenuDesignator : DesignationCategoryDef
    {
        public SubBuildMenuDef menuDef;

        public override void ResolveReferences()
        {
            base.ResolveReferences();
            resolvedDesignators ??= new List<Designator>();
            resolvedDesignators.Add(new Designator_SubBuildMenu(menuDef));
        }
    }

    public class SubBuildMenuDef : Def
    {
        public List<SubThingGroupDef> thingGroups;
        public string superPackPath;
    }

    public class SubThingGroupDef : Def
    {
        public List<SubThingCategory> subCategories;
        public string groupIconPath;

        //Pack Def | BuildMenu | Des | Des_Sel | Tab | Tab_Sel
        public string subPackPath;
    }

    public class DesignationTexturePack
    {
        public Texture2D backGround;
        public Texture2D tab;
        public Texture2D tabSelected;
        public Texture2D designator;
        public Texture2D designatorSelected;

        public DesignationTexturePack(string packPath)
        {
            backGround = ContentFinder<Texture2D>.Get(Path.Combine(packPath, "BuildMenu"));
            tab = ContentFinder<Texture2D>.Get(Path.Combine(packPath, "Tab"));
            tabSelected = ContentFinder<Texture2D>.Get(Path.Combine(packPath, "Tab_Sel"));
            designator = ContentFinder<Texture2D>.Get(Path.Combine(packPath, "Des"));
            designatorSelected = ContentFinder<Texture2D>.Get(Path.Combine(packPath, "Des_SeL"));
        }
    }

    public class SubThingCategory : Def{}

    public class Designator_SubBuildMenu : Designator
    {
        private SubBuildMenuDef subMenuDef;

        public Designator_SubBuildMenu(SubBuildMenuDef menuDef)
        {
            this.subMenuDef = menuDef;
        }

        public override void Selected()
        {
            SubBuildMenu.ToggleOpen(subMenuDef);
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            return AcceptanceReport.WasRejected;
        }
    }

    public class SubBuildMenu : Window
    {
        private static string DefaultPackPath = "Menu/SubBuildMenu";

        private static Vector2 tabSize = new Vector2(118, 30);
        private static Vector2 searchBarSize = new Vector2(125, 25);
        private static float topBotMargin = 10f;
        private static float sideMargin = 3f;
        private static float iconSize = 30f;

        //
        private SubBuildMenuDef menuDef;
        private Vector2 scroller = Vector2.zero;
        private string searchText = "";
        private Gizmo mouseOverGizmo;
        private ThingDef inactiveDef;

        //
        private Dictionary<SubThingGroupDef, SubThingCategory> cachedSelection = new ();
        private Dictionary<SubThingGroupDef, DesignationTexturePack> texturePacks = new ();

        private SubThingGroupDef selGroup;
        private SubThingCategory SelectedCategory => cachedSelection[selGroup];
        private Designator CurrentDesignator => (Designator)(mouseOverGizmo ?? Find.DesignatorManager.SelectedDesignator);

        //
        public override Vector2 InitialSize => new(370, 526);
        public override float Margin => 8;

        public SubBuildMenu(SubBuildMenuDef menuDef)
        {
            this.menuDef = menuDef;

            //Window Settings
            draggable = true;
            preventCameraMotion = false;
            doCloseX = true;

            windowRect.x = 5f;
            windowRect.y = 5f;

            //Menu Settings
            selGroup = menuDef.thingGroups.First();

            //Generate
            foreach (SubThingGroupDef def in menuDef.thingGroups)
            {
                var path = (def.subPackPath ?? menuDef.superPackPath) ?? DefaultPackPath;
                texturePacks.Add(def, new DesignationTexturePack(path));
                cachedSelection.Add(def, def.subCategories[0]);
            }
        }

        public override void PostOpen()
        {
            base.PostOpen();
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect searchBar = new Rect(new Vector2(InitialSize.x - searchBarSize.x, 0f), searchBarSize);
            DoSearchBar(searchBar);
            //SetupBG
            Rect menuRect = new Rect(0f, searchBarSize.y, windowRect.width, 526f);
            Widgets.DrawTextureRotated(menuRect, texturePacks[selGroup].backGround, 0f);
            //Reduce Content Rect
            menuRect = new Rect(sideMargin, menuRect.y + topBotMargin, menuRect.width - sideMargin, menuRect.height - (topBotMargin * 2));
            Widgets.BeginGroup(menuRect);
            GroupSidebar(3);
            Rect extraDes = new Rect(2, menuRect.height - 75, iconSize, iconSize);
            DrawDesignator(extraDes, DesignatorUtility.FindAllowedDesignator<Designator_Deconstruct>());
            extraDes.y = extraDes.yMax + 5;
            DrawDesignator(extraDes, DesignatorUtility.FindAllowedDesignator<Designator_Cancel>());
            Rect DesignatorRect = new Rect(iconSize + sideMargin, 0f, menuRect.width - (iconSize + sideMargin), menuRect.height);
            Widgets.BeginGroup(DesignatorRect);
            var subCats = SelectedFaction.subCategories;
            Vector2 curXY = Vector2.zero;
            foreach (var cat in subCats)
            {
                Rect tabRect = new Rect(curXY, tabSize);
                Rect clickRect = new Rect(tabRect.x + 5, tabRect.y, tabRect.width - (10), tabRect.height);
                Texture2D tex = cat == SelectedCategory || Mouse.IsOver(clickRect) ? TexturePacks[SelectedFaction].TabSelected : TexturePacks[SelectedFaction].Tab;
                Widgets.DrawTextureFitted(tabRect, tex, 1f);
                if (TRThingDefList.HasUnDiscovered(SelectedFaction, cat))
                {
                    TRWidgets.DrawTextureInCorner(tabRect, TiberiumContent.Undiscovered, 7, TextAnchor.UpperRight, new Vector2(-6, 3));
                    //DrawUndiscovered(tabRect, new Vector2(-6, 3));
                    //Widgets.DrawTextureFitted(tabRect, TiberiumContent.Tab_Undisc, 1f);
                }

                Text.Anchor = TextAnchor.MiddleCenter;
                Text.Font = GameFont.Small;
                string catLabel = cat.LabelCap;
                if (Text.CalcSize(catLabel).y > tabRect.width)
                { Text.Font = GameFont.Tiny; }
                Widgets.Label(tabRect, catLabel);
                Text.Font = GameFont.Tiny;
                Text.Anchor = 0;

                AdjustXY(ref curXY, tabSize.x - 10f, tabSize.y, tabSize.x * 3);
                if (Widgets.ButtonInvisible(clickRect))
                {
                    SearchText = "";
                    SetSelectedCat(cat);
                }
            }
            DrawFactionCat(new Rect(0f, curXY.y, DesignatorRect.width, DesignatorRect.height - curXY.y), SelectedFaction, SelectedCategory);
            Widgets.EndGroup();
            Widgets.EndGroup();
        }

        private void DoSearchBar(Rect textArea)
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;

            if (searchText.NullOrEmpty())
            {
                GUI.color = new Color(1, 1, 1, 0.75f);
                Widgets.Label(textArea.ContractedBy(2), "Search..");
                GUI.color = Color.white;
            }
            searchText = Widgets.TextArea(textArea, searchText, false);
            Text.Anchor = 0;
        }

        private void GroupSidebar(float yPos)
        {
            List<SubThingGroupDef> list = menuDef.thingGroups;
            for (int i = 0; i < list.Count; i++)
            {
                SubThingGroupDef des = list[i];
                Rect partRect = new Rect(0f, yPos + ((iconSize + 6) * i), iconSize, iconSize);
                bool sel = Mouse.IsOver(partRect) || selGroup == des;
                GUI.color = sel ? Color.white : new Color(1f, 1f, 1f, 0.4f);
                Widgets.DrawTextureFitted(partRect, IconForFaction(des), 1f);
                GUI.color = Color.white;
                if (SubMenuThingDefList.HasUnDiscovered(des))
                {
                    TWidgets.DrawTextureInCorner(partRect, TeleContent.Undiscovered, 8, TextAnchor.UpperRight);
                    //DrawUndiscovered(partRect);
                }

                if (Widgets.ButtonInvisible(partRect))
                {
                    searchText = "";
                    selGroup = des;
                }
            }
        }

        private void InactiveDesignator(ThingDef def, Rect main, Vector2 size, ref Vector2 XY)
        {
            Rect rect = new Rect(new Vector2(XY.x, XY.y), size);
            GUI.color = Color.grey;
            bool mouseOver = Mouse.IsOver(rect);
            Texture2D tex = mouseOver ? texturePacks[selGroup].designatorSelected : TexturePacks[SelectedFaction].Designator;
            Widgets.DrawTextureFitted(rect, tex, 1f);
            Widgets.DrawTextureFitted(rect.ContractedBy(2), def.uiIcon, 1);
            GUI.color = Color.white;
            if (Mouse.IsOver(rect))
                inactiveDef = def;

            AdjustXY(ref XY, size.x, size.x, main.width, 5);
        }

        //
        private void DrawDesignator(Rect rect, Designator designator)
        {
            if (Widgets.ButtonImage(rect, designator.icon))
            {
                designator.ProcessInput(null);
            }
        }

        private void AdjustXY(ref Vector2 XY, float xIncrement, float yIncrement, float maxWidth, float minX = 0f)
        {
            if (XY.x + (xIncrement * 2) > maxWidth)
            {
                XY.y += yIncrement;
                XY.x = minX;
            }
            else
            {
                XY.x += xIncrement;
            }
        }

        private Dictionary<SubThingGroupDef, Texture2D> iconByGroup = new();
        private Texture2D IconForFaction(SubThingGroupDef group)
        {
            if (iconByGroup.TryGetValue(group, out var tex))
            {
                return tex;
            }

            tex = ContentFinder<Texture2D>.Get(group.groupIconPath, false) ?? BaseContent.BadTex;
            iconByGroup.Add(group, tex);
            return tex;
        }

        //Data
        public void Select(ThingDef def)
        {
            //SelectedFaction = def.factionDesignation;
            //cachedSelection[SelectedFaction] = def.TRCategory;
        }

        private static Dictionary<SubBuildMenuDef, Window> windowsByDef = new();

        public static void ToggleOpen(SubBuildMenuDef subMenuDef)
        {
            if (!windowsByDef.TryGetValue(subMenuDef, out Window window))
            {
                windowsByDef.Add(subMenuDef, new SubBuildMenu(subMenuDef));
            }
            if (window.IsOpen)
                window.Close();
            else
            {
                Find.WindowStack.Add(window);
            }
        }
    }
}
