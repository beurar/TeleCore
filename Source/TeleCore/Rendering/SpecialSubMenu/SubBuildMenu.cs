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
        //public DesignationTexturePack texturePack;

        public override void ResolveReferences()
        {
            base.ResolveReferences();
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                resolvedDesignators.Clear();
                resolvedDesignators ??= new List<Designator>();
                resolvedDesignators.Add(new Designator_SubBuildMenu(menuDef));
                TLog.Message($"Added custom SubBuildMenu designator for {menuDef}");
            });
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
            backGround = ContentFinder<Texture2D>.Get(packPath + "/BuildMenu");
            tab = ContentFinder<Texture2D>.Get(packPath + "/Tab");
            tabSelected = ContentFinder<Texture2D>.Get(packPath + "/Tab_Sel");
            designator = ContentFinder<Texture2D>.Get(packPath + "/Des");
            designatorSelected = ContentFinder<Texture2D>.Get(packPath + "/Des_Sel");
        }
    }

    public class SubThingCategory : Def{}

    public class Designator_SubBuildMenu : Designator
    {
        private SubBuildMenuDef subMenuDef;

        public Designator_SubBuildMenu(SubBuildMenuDef menuDef)
        {
            //defaultLabel = "Menu";
            //defaultDesc = "MenuDesc";
            order = -1;
            
            TLog.Message($"Created {nameof(Designator_SubBuildMenu)} with {menuDef}");
            subMenuDef = menuDef;
        }

        public void Toggle_Menu()
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
        
        private SubThingGroupDef SelectedGroup { get; set; }
        private SubThingCategory SelectedCategory => cachedSelection[SelectedGroup];
        private Designator CurrentDesignator => (Designator)(mouseOverGizmo ?? Find.DesignatorManager.SelectedDesignator);
        private HashSet<ThingDef> HighLightOptions = new HashSet<ThingDef>();

        //
        public override Vector2 InitialSize => new(400, 550);
        public override float Margin => 8;

        public SubBuildMenu()
        {
            //Setup();
        }
        
        public SubBuildMenu(SubBuildMenuDef menuDef)
        {
            this.menuDef = menuDef;

            //Window Settings
            draggable = true;
            preventCameraMotion = false;
            doCloseX = true;

            windowRect.x = 5f;
            windowRect.y = 5f;
            doWindowBackground = false;
            doCloseButton = false;
            doCloseX = false;

            //Menu Settings
            SelectedGroup = menuDef.thingGroups.First();

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

        private ActionComposition _ac;
        
        private void ActionTickMethod(ActionPart p)
        {
            TLog.Message($"Ticking apart[{p?.StartTick}|{p?.Duration}] {_ac?.CurrentTick}");
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (Widgets.ButtonImage(new Rect(inRect.x + 4, inRect.y + 4, 18f, 18f), TexButton.CloseXSmall))
            {
                Close(); 
                ActionComposition ac = new ActionComposition("Test");
                //_ac ??= ac;
                //ac.AddPart((_) => { Messages.Message("Starting ActionComp", null);},0,0);

                ac.AddPart((p) =>
                {
                    TLog.Message("Simple empty count :)");
                    //TLog.Message($"Ticking apart[{p?.StartTick}|{p?.Duration}] {ac?.CurrentTick}");
                },0,15);
                
                /*
                ac.AddPart((p) =>
                {
                    
                    TLog.Message($"Ticking apart[{p?.StartTick}|{p?.Duration}] {ac?.CurrentTick}");
                }, null, 10);
                ac.AddPart((p) =>
                {
                    
                    TLog.Message($"Ticking apart[{p?.StartTick}|{p?.Duration}] {ac?.CurrentTick}");
                }, null, 20);
                */
                ac.Init();
                return;
            }

            //
            Rect searchBar = new Rect(new Vector2(inRect.xMax - searchBarSize.x, 0f), searchBarSize);
            DoSearchBar(searchBar);
            //SetupBG
            Rect menuRect = new Rect(0f, searchBarSize.y, windowRect.width, 526f);
            Widgets.DrawTextureRotated(menuRect, texturePacks[SelectedGroup].backGround, 0f);
            //Reduce Content Rect
            menuRect = new Rect(sideMargin, menuRect.y + topBotMargin, menuRect.width - sideMargin, menuRect.height - (topBotMargin * 2));
            Widgets.BeginGroup(menuRect);
            {
                GroupSidebar(3);
                Rect extraDes = new Rect(2, menuRect.height - 75, iconSize, iconSize);
                DrawDesignator(extraDes, DesignatorUtility.FindAllowedDesignator<Designator_Deconstruct>());
                extraDes.y = extraDes.yMax + 5;
                DrawDesignator(extraDes, DesignatorUtility.FindAllowedDesignator<Designator_Cancel>());
                Rect DesignatorRect = new Rect(iconSize + sideMargin, 0f, menuRect.width - (iconSize + sideMargin), menuRect.height);
                Widgets.BeginGroup(DesignatorRect);
                {
                    var subCats = SelectedGroup.subCategories;
                    Vector2 curXY = Vector2.zero;
                    foreach (var cat in subCats)
                    {
                        Rect tabRect = new Rect(curXY, tabSize);
                        Rect clickRect = new Rect(tabRect.x + 5, tabRect.y, tabRect.width - (10), tabRect.height);
                        Texture2D tex = cat == SelectedCategory || Mouse.IsOver(clickRect)
                            ? texturePacks[SelectedGroup].tabSelected
                            : texturePacks[SelectedGroup].tab;
                        Widgets.DrawTextureFitted(tabRect, tex, 1f);
                        if (SubMenuThingDefList.HasUnDiscovered(SelectedGroup, cat))
                        {
                            TWidgets.DrawTextureInCorner(tabRect, TeleContent.Undiscovered, 7, TextAnchor.UpperRight, new Vector2(-6, 3));
                            //DrawUndiscovered(tabRect, new Vector2(-6, 3));
                            //Widgets.DrawTextureFitted(tabRect, TiberiumContent.Tab_Undisc, 1f);
                        }

                        Text.Anchor = TextAnchor.MiddleCenter;
                        Text.Font = GameFont.Small;
                        string catLabel = cat.LabelCap;
                        if (Text.CalcSize(catLabel).y > tabRect.width)
                        {
                            Text.Font = GameFont.Tiny;
                        }

                        Widgets.Label(tabRect, catLabel);
                        Text.Font = GameFont.Tiny;
                        Text.Anchor = 0;

                        AdjustXY(ref curXY, tabSize.x - 10f, tabSize.y, tabSize.x * 3);
                        if (Widgets.ButtonInvisible(clickRect))
                        {
                            searchText = "";
                            SetSelectedCat(cat);
                        }
                    }
                    //
                    XYEndCheck(ref curXY, tabSize.y, tabSize.x * 3);
                    DrawFactionCat(new Rect(0f, curXY.y, DesignatorRect.width, DesignatorRect.height - curXY.y), SelectedGroup, SelectedCategory);
                }
                Widgets.EndGroup();
            }
            Widgets.EndGroup();
        }

        private void DrawFactionCat(Rect main, SubThingGroupDef groupDef, SubThingCategory category)
        {
            if (groupDef != null && category != null)
            {
                Widgets.BeginGroup(main);
                {
                    Vector2 size = new Vector2(80, 80);
                    Vector2 curXY = new Vector2(5f, 5f);
                    List<ThingDef> things = searchText.NullOrEmpty()
                        ? SubMenuThingDefList.Categorized[groupDef][category]
                        : ItemsBySearch(searchText);
                    Rect viewRect = new Rect(0f, 0f, main.width,
                        10 + ((float)(Math.Round((decimal)(things.Count / 4), 0, MidpointRounding.AwayFromZero) + 1) *
                              size.x));
                    Rect scrollerRect = new Rect(0f, 0f, main.width, main.height + 5);
                    Widgets.BeginScrollView(scrollerRect, ref scroller, viewRect, false);
                    {
                        mouseOverGizmo = null;
                        inactiveDef = null;
                        foreach (var def in things)
                        {
                            if (!DebugSettings.godMode &&
                                (def.HasTeleExtension(out var tex) && tex.subMenuDesignation.hidden)) continue;
                            if (SubMenuThingDefList.IsActive(def))
                            {
                                Designator(def, main, size, ref curXY);
                            }
                            else
                                InactiveDesignator(def, main, size, ref curXY);
                        }
                    }
                    Widgets.EndScrollView();
                }
                Widgets.EndGroup();
            }
        }

        private List<ThingDef> ItemsBySearch(string searchText)
        {
            return SubMenuThingDefList.Categorized[SelectedGroup].SelectMany(cat => cat.Value).Where(d => SubMenuThingDefList.IsActive(d) && d.label.ToLower().Contains(searchText.ToLower())).ToList();
        }


        private void Designator(ThingDef def, Rect main, Vector2 size, ref Vector2 XY)
        {
            Rect rect = new Rect(new Vector2(XY.x, XY.y), size);
            GUI.color = new Color(1, 1, 1, 0.80f);
            bool mouseOver = Mouse.IsOver(rect);
            Texture2D tex = mouseOver ? texturePacks[SelectedGroup].designatorSelected : texturePacks[SelectedGroup].designator;
            Widgets.DrawTextureFitted(rect, tex, 1f);
            GUI.color = mouseOver ? new Color(1, 1, 1, 0.45f) : Color.white;
            Widgets.DrawTextureFitted(rect.ContractedBy(2), def.uiIcon, 1);
            GUI.color = Color.white;
            if (def.HasTeleExtension(out var telex) && telex.subMenuDesignation.hidden)
            {
                Text.Font = GameFont.Medium;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, "DEV");
                Text.Anchor = default;
                Text.Font = GameFont.Small;
            }

            if (HighLightOptions.Contains(def))
            {
                Widgets.DrawTextureFitted(rect, TeleContent.Undiscovered, 1);
            }

            var optionDiscovered = SubMenuThingDefList.ConstructionOptionDiscovered(def);
            if (!optionDiscovered)
            {
                TWidgets.DrawTextureInCorner(rect, TeleContent.Undiscovered, 7, TextAnchor.UpperRight, new Vector2(-5, 5));
                //DrawUndiscovered(rect, new Vector2(-5, 5));
                //Widgets.DrawTextureFitted(rect, TiberiumContent.Des_Undisc, 1f);
            }

            if (mouseOver)
            {
                if (!optionDiscovered)
                {
                    SubMenuThingDefList.Discover_ConstructionOption(def);
                }

                mouseOverGizmo = /*def.devObject ? StaticData.GetDesignatorFor<Designator_BuildGodMode>(def) :*/ GenData.GetDesignatorFor<Designator_Build>(def);
                Text.Anchor = TextAnchor.UpperCenter;
                Widgets.Label(rect, def.LabelCap);
                Text.Anchor = 0;
                TooltipHandler.TipRegion(rect, def.LabelCap);
            }

            if (Widgets.ButtonInvisible(rect))
            {
                mouseOverGizmo.ProcessInput(null);
                Event.current.Use();
            }
            AdjustXY(ref XY, size.x, size.x, main.width, 5);
        }

        private void SetSelectedCat(SubThingCategory def)
        {
            cachedSelection[SelectedGroup] = def;
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
                bool sel = Mouse.IsOver(partRect) || SelectedGroup == des;
                GUI.color = sel ? Color.white : new Color(1f, 1f, 1f, 0.4f);
                Widgets.DrawTextureFitted(partRect, IconForGroup(des), 1f);
                GUI.color = Color.white;
                if (SubMenuThingDefList.HasUnDiscovered(des))
                {
                    TWidgets.DrawTextureInCorner(partRect, TeleContent.Undiscovered, 8, TextAnchor.UpperRight);
                    //DrawUndiscovered(partRect);
                }

                if (Widgets.ButtonInvisible(partRect))
                {
                    searchText = "";
                    SelectedGroup = des;
                }
            }
        }

        private void InactiveDesignator(ThingDef def, Rect main, Vector2 size, ref Vector2 XY)
        {
            Rect rect = new Rect(new Vector2(XY.x, XY.y), size);
            GUI.color = Color.grey;
            bool mouseOver = Mouse.IsOver(rect);
            Texture2D tex = mouseOver ? texturePacks[SelectedGroup].designatorSelected : texturePacks[SelectedGroup].designator;
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
                XY.x = minX;
                XY.y += yIncrement;
            }
            else
            {
                XY.x += xIncrement;
            }

        }

        private void XYEndCheck(ref Vector2 XY, float yIncrement, float maxWidth)
        {
            //
            if (XY.y < yIncrement && (XY.x <= maxWidth))
            {
                XY.y += yIncrement;
            }
        }

        private Dictionary<SubThingGroupDef, Texture2D> iconByGroup = new();
        private Texture2D IconForGroup(SubThingGroupDef group)
        {
            if (iconByGroup.TryGetValue(group, out var tex))
            {
                return tex;
            }

            if (group.groupIconPath == null)
                tex = BaseContent.BadTex;

            tex ??= ContentFinder<Texture2D>.Get(group.groupIconPath, false) ?? BaseContent.BadTex;
            iconByGroup.Add(group, tex);
            return tex;
        }

        //Data
        public void Select(ThingDef def)
        {
            //SelectedFaction = def.factionDesignation;
            //cachedSelection[SelectedFaction] = def.TRCategory;
        }

        internal static Dictionary<SubBuildMenuDef, Window> windowsByDef = new();

        public static void ToggleOpen(SubBuildMenuDef subMenuDef)
        {
            TLog.Message($"Trying to toggle {subMenuDef}");
            if (!windowsByDef.TryGetValue(subMenuDef, out Window window))
            {
                TLog.Message($"Making new window {subMenuDef}");
                 window = new SubBuildMenu(subMenuDef);
                windowsByDef.Add(subMenuDef, window);
            }

            TLog.Message($"Toggling window: {window.ID} | {window.IsOpen}");
            if (window.IsOpen)
            {
                window.Close();
            }
            else
            {
                Find.WindowStack.Add(window);
            }
        }
    }
}
