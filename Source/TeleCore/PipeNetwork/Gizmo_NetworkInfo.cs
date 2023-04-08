using System;
using System.Collections.Generic;
using System.Linq;
using Multiplayer.API;
using RimWorld;
using TeleCore.FlowCore;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TeleCore
{
    public class Gizmo_NetworkOverview : Gizmo, IDisposable
    {
        private readonly Comp_Network compNetwork;
        private readonly Dictionary<NetworkSubPart, NetworkInfoView> _viewByPart;

        //Sizes
        private const float mainWidth = 200f;
        private const int gizmoPadding = 5;
        
        //Extendo Consts
        private float currentExtendedY = 0;
        private float desiredExtendedY;
        internal const int selSettingHeight = 22;
        
        //Part Extendo Consts
        private readonly Vector2 partSelectionSize;
        private float curExtendedPartX = 0;
        private float desiredExtendedPartX;
        private FloatRange partSelRange;

        //
        public NetworkInfoView SelectedPart { get; private set; }
        
        public Gizmo_NetworkOverview(Comp_Network compParent) : base()
        {
            this.order = -250f;
            compNetwork = compParent;
            _viewByPart = new Dictionary<NetworkSubPart, NetworkInfoView>();
            
            //
            var maxTextSize = 50f;
            foreach (var part in compNetwork.NetworkParts)
            {
                _viewByPart.Add(part, new NetworkInfoView(part));
                maxTextSize = Mathf.Max(maxTextSize, Text.CalcSize(part.NetworkDef.labelShort).x + 10);
            }
            
            //Set First Selection
            SelectedPart = _viewByPart.First().Value;

            //
            partSelectionSize = new Vector2(maxTextSize, 25);
            partSelRange = new FloatRange(10, MaxPartSelX());
            
            //
            TFind.TickManager.RegisterMapUITickAction(Tick);
        }

        public override float GetWidth(float maxWidth)
        {
            return 0;
        }

        public float GetWidthSpecial()
        {
            return mainWidth + GetPartSelectionSize().x;
        }

        private void InspectorRef(Vector2 topLeft, out Vector2 newPos, out Vector2 size)
        {
            var inspector = Find.MainTabsRoot.OpenTab.TabWindow as MainTabWindow_Inspect;
            if (inspector == null)
            {
                newPos = topLeft;
                size = new Vector2(mainWidth, mainWidth);
                return;
            }
            var inspectorSize = inspector.RequestedTabSize;
            newPos = new Vector2(inspector.RequestedTabSize.x, inspector.PaneTopY);
            size = new Vector2(mainWidth, inspectorSize.y);
        }

        private Vector2 GetPartSelectionSize()
        {
            if(_viewByPart.Count <= 1) return Vector2.zero;
            return new Vector2(curExtendedPartX, 0);
            var inspector = Find.MainTabsRoot.OpenTab.TabWindow as MainTabWindow_Inspect;
            var inspectorSize = inspector.RequestedTabSize;
            var maxY = inspectorSize.y - 10;
            var fitted = Mathf.FloorToInt((partSelectionSize.y * _viewByPart.Count) / maxY);
            return new Vector2((fitted + 1) * curExtendedPartX, inspectorSize.y);
        }

        private float MaxPartSelX()
        {
            var inspector = Find.MainTabsRoot.OpenTab.TabWindow as MainTabWindow_Inspect;
            var inspectorSize = inspector.RequestedTabSize;
            var maxY = inspectorSize.y - 10;
            var fitted = Mathf.FloorToInt((partSelectionSize.y * _viewByPart.Count) / maxY);
            return (fitted + 1) * partSelectionSize.x;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            InspectorRef(topLeft, out var pos, out var size);
            Rect mainRect = new Rect(pos, size);

            //Draw Part Selection
            if(_viewByPart.Count > 1)
                DrawPartSelection(mainRect);
            
            //Draw Extendo Tabs
            DrawExtendoTabs(mainRect);
            
            //Draw Extended Setting
            DrawExtendedTab(mainRect);
            
            //Draw Main
            SelectedPart.DrawMainContent(mainRect);

            //
            var firstEv = Mouse.IsOver(mainRect) ? GizmoState.Mouseover : GizmoState.Clear;
            var eventRes = Event.current.isMouse && firstEv == GizmoState.Mouseover ? GizmoState.Interacted : firstEv;
            return new GizmoResult(eventRes);
        }
        
        private void DrawPartSelection(Rect mainRect)
        {
            Rect hoverArea = new Rect(mainRect.xMax , mainRect.y, Mathf.Max(15, curExtendedPartX), mainRect.height);
            Rect area = new Rect(mainRect.xMax , mainRect.y+5, curExtendedPartX, mainRect.height-10);
            Notify_PartSelHovered(Mouse.IsOver(hoverArea));

            Vector2 curPos = area.position;
            float t = Mathf.InverseLerp(partSelRange.min, partSelRange.max, curExtendedPartX);
            float partWidth = Mathf.Max(partSelectionSize.x * t, 10);
            Vector2 partSize = new Vector2(partWidth, partSelectionSize.y);
            foreach (var partView in _viewByPart)
            {
                var part = partView.Key;
                var partRect = new Rect(curPos, partSize);
                var textRect = new Rect(new Vector2(curPos.x-(partSelRange.max-curExtendedPartX), curPos.y), partSelectionSize);

                var isSelected = SelectedPart == partView.Value;
                var colorBG = isSelected ? TColor.OptionSelectedBGFillColor : TColor.WindowBGFillColor;
                var colorBorder = isSelected ? TColor.OptionSelectedBGBorderColor : TColor.WindowBGBorderColor;
                
                TWidgets.DrawColoredBox(partRect, colorBG, colorBorder, 1);
                TWidgets.DrawHighlightIfMouseOverColor(partRect, TColor.White05);
                TWidgets.HoverContainerReadout(partRect, part.Container);
                
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(textRect.ContractedBy(5,0), part.NetworkDef.labelShort);
                Text.Anchor = default;
                
                if (Widgets.ButtonInvisible(partRect))
                {
                    SelectedPart = partView.Value;
                }
                
                //
                curPos.y += partSelectionSize.y;
                if(curPos.y >= area.yMax)
                {
                    curPos.x += partSelectionSize.x;
                    curPos.y = area.y;
                }
            }
        }

        private void DrawExtendoTabs(Rect mainRect)
        {
            if (!SelectedPart.HasExtensions) return;
            
            var yMax = Math.Max(15, currentExtendedY) + 10;
            Rect extendTriggerArea = new Rect(mainRect.x, mainRect.y - (yMax - 5), mainRect.width, yMax);
            Rect extendedButton = new Rect(mainRect.x, mainRect.y - (currentExtendedY + 1), mainRect.width, currentExtendedY + 1);
            Notify_ExtendHovered(Mouse.IsOver(extendTriggerArea));

            Widgets.DrawWindowBackground(extendedButton);
            Text.Anchor = TextAnchor.MiddleCenter;
            var curY = extendedButton.y;
            foreach (var setting in SelectedPart.ExtendoTabs)
            {
                if (curY > extendedButton.yMax) continue;
                Rect labelRect = new Rect(extendedButton.x, curY, extendedButton.width, Math.Min(extendedButton.height, selSettingHeight));
                Widgets.Label(labelRect, setting.Key);
                Widgets.DrawHighlightIfMouseover(labelRect);
                if (Widgets.ButtonInvisible(labelRect))
                {
                    SelectedPart.SetExtendoTab(setting.Key);
                }
                curY += selSettingHeight;
            }
            Text.Anchor = default;
        }
        
        private void DrawExtendedTab(Rect mainRect)
        {
            if (SelectedPart.ExtendedTab == null) return;
            
            //Extend Rect
            Rect settingRect = new Rect(mainRect.x, mainRect.y - mainRect.height, mainRect.width, mainRect.height);
            var closeButtonSize = new Vector2(settingRect.width - (gizmoPadding * 2), 16);
            var offset = (settingRect.width / 2) - (closeButtonSize.x / 2);
            Rect closeButtonRect = new Rect(settingRect.x + offset, settingRect.y - closeButtonSize.y, closeButtonSize.x, closeButtonSize.y + gizmoPadding);

            Widgets.DrawWindowBackground(closeButtonRect);
            Widgets.DrawHighlightIfMouseover(closeButtonRect);

            Text.Anchor = TextAnchor.UpperCenter;
            //var matrix = GUI.matrix;
            //UI.RotateAroundPivot(90, closeButtonRect.center);
            Widgets.Label(closeButtonRect, "<CLOSE>");
            //GUI.matrix = matrix;
            Text.Anchor = default;

            if (Widgets.ButtonInvisible(closeButtonRect))
            {
                SelectedPart.SetExtendoTab(null);
                return;
            }

            //
            SelectedPart.DrawExtendedTab(settingRect);
        }
        
        private void Notify_ExtendHovered(bool isHovered)
        {
            desiredExtendedY = isHovered ? SelectedPart.ExtendoRange.TrueMax : SelectedPart.ExtendoRange.TrueMin;
        }

        private void Notify_PartSelHovered(bool isHovered)
        {
            desiredExtendedPartX = isHovered ? partSelRange.TrueMax : partSelRange.TrueMin;
        }

        private void Tick()
        {
            if (!Visible) return;
            if (Math.Abs(currentExtendedY - desiredExtendedY) > 0.01)
            {
                var val = desiredExtendedY > currentExtendedY ? 1.5f : -1.5f;
                currentExtendedY = Mathf.Clamp(currentExtendedY + val * SelectedPart.ExtendoTabs.Count, SelectedPart.ExtendoRange.TrueMin, SelectedPart.ExtendoRange.TrueMax);
            }
            
            if (Math.Abs(curExtendedPartX - desiredExtendedPartX) > 0.01)
            {
                var val = desiredExtendedPartX > curExtendedPartX ? 3f : -3f;
                curExtendedPartX = Mathf.Clamp(curExtendedPartX + (val * (partSelRange.TrueMax/partSelectionSize.x)), partSelRange.TrueMin, partSelRange.TrueMax);
            }
        }

        public void Dispose()
        {
            //TODO: Deregister Tick
        }
    }

    public class NetworkInfoView
    {
        //
        private NetworkSubPart parentComp;
        private Dictionary<string, Action<Rect>> tabDrawActions;
        
        //
        private Vector2 filterScroller = Vector2.zero;
        
        #region Properties
        
        public bool HasExtensions
        {
            get
            {
                //Requester overview
                if (parentComp.HasContainer) return true;
                if (parentComp.NetworkRole.HasFlag(NetworkRole.Requester)) return true;
                return false;
            }
        }

        //
        public NetworkContainer Container => parentComp.Container;
        
        //Extendo Tabs
        public Dictionary<string, Action<Rect>> ExtendoTabs => tabDrawActions;
        public string ExtendedTab { get; private set; }
        public FloatRange ExtendoRange { get; private set; }

        #endregion

        public NetworkInfoView(NetworkSubPart part) : base()
        {
            //
            parentComp = part;
            
            //
            SetExtensions();
            ExtendoRange = new FloatRange(10, Gizmo_NetworkOverview.selSettingHeight * ExtendoTabs.Count);
        }

        #region Extendo Parts

        public void SetExtendoTab(string tabKey)
        {
            ExtendedTab = tabKey;
        }

        public void DrawExtendedTab(Rect rect)
        {
            if (ExtendedTab == null) return;
            Find.WindowStack.ImmediateWindow(parentComp.GetHashCode(), rect, WindowLayer.GameUI, delegate
            {
                if(ExtendedTab == null) return;
                tabDrawActions[ExtendedTab].Invoke(rect.AtZero());
            }, false, false, 0);
        }
        
        //
        private void SetExtensions()
        {
            tabDrawActions = new Dictionary<string, Action<Rect>>();
            if (parentComp.NetworkRole.HasFlag(NetworkRole.Requester))
            {
                ExtendoTabs.Add("Requester Settings", delegate (Rect rect)
                {
                    parentComp.RequestWorker.DrawSettings(rect);
                });
            }
            
            if (parentComp.HasContainer)
            {
                ExtendoTabs.Add("Container Settings", delegate (Rect rect)
                {
                    Widgets.DrawWindowBackground(rect);
                    TWidgets.DrawValueContainerReadout(rect, parentComp.Container);

                    //Right Click Input
                    if (TWidgets.MouseClickIn(rect, 1) && DebugSettings.godMode)
                    {
                        FloatMenu menu = new FloatMenu(DebugOptions.ToList(), $"{parentComp.NetworkRole}", true);
                        menu.vanishIfMouseDistant = true;
                        Find.WindowStack.Add(menu);
                    }

                    //
                    //TWidgets.AbsorbInput(rect);
                });
            }

            if (parentComp.NetworkRole.HasFlag(NetworkRole.Storage))
            {
                ExtendoTabs.Add("Filter Settings", delegate (Rect rect)
                {
                    var readoutRect = rect.LeftPart(0.75f).ContractedBy(5).Rounded();
                    var clipboardRect = new Rect(readoutRect.xMax + 5, readoutRect.y, 22f, 22f);
                    var clipboardInsertRect = new Rect(clipboardRect.xMax + 5, readoutRect.y, 22f, 22f);

                    var listingRect = readoutRect.ContractedBy(2).Rounded();

                    Widgets.DrawWindowBackground(rect);
                    TWidgets.DrawColoredBox(readoutRect, TColor.BlueHueBG, TColor.MenuSectionBGBorderColor, 1);
                    
                    if (parentComp.Container.AcceptedTypes.NullOrEmpty())
                        return;
                    
                    //
                    Listing_Standard listing = new();
                    listing.Begin(listingRect);
                    listing.Label("Filter");
                    listing.GapLine(4);
                    listing.End();

                    var scrollOutRect = new Rect(listingRect.x, listingRect.y + listing.curY, listingRect.width, listingRect.height - listing.curY);
                    var scrollViewRect = new Rect(listingRect.x, listingRect.y + listing.curY, listingRect.width, (parentComp.Container.AcceptedTypes.Count + 1) * Text.LineHeight);

                    Widgets.DrawBoxSolid(scrollOutRect, TColor.BGDarker);
                    Widgets.BeginScrollView(scrollOutRect, ref filterScroller, scrollViewRect, false);
                    {
                        Text.Font = GameFont.Tiny;
                        var label1 = "Type";
                        var label2 = "Receive";
                        var label3 = "Store";
                        var size1 = Text.CalcSize(label1);
                        var size2 = Text.CalcSize(label2);
                        var size3 = Text.CalcSize(label3);

                        WidgetRow row = new WidgetRow(scrollViewRect.xMax, scrollViewRect.y, UIDirection.LeftThenDown);
                        row.Label(label3, size3.x);
                        row.Label(label2, size2.x);
                        row.Label(label1, scrollViewRect.width - (row.curX + size1.x));

                        float curY = scrollViewRect.y + 24;
                        foreach (var acceptedType in parentComp.Container.AcceptedTypes)
                        {
                            var settings = parentComp.Container.GetFilterFor(acceptedType);
                            var canReceive = settings.canReceive;
                            var canStore = settings.canStore;
                         
                            WidgetRow itemRow = new WidgetRow(scrollViewRect.xMax, curY, UIDirection.LeftThenDown);
                            itemRow.Checkbox( ref canStore, true, size3.x); 
                            itemRow.Highlight(size3.x);
                            itemRow.Checkbox( ref canReceive, true);
                            itemRow.Highlight(24);
                            itemRow.Label($"{acceptedType.LabelCap.CapitalizeFirst().Colorize(acceptedType.valueColor)}: ", 84);
                            itemRow.Highlight(84);

                            parentComp.Container.SetFilterFor(acceptedType, new FlowValueFilterSettings()
                            {
                                canReceive = canReceive,
                                canStore = canStore
                            });
                            
                            //
                            curY += 24;
                        }
                        
                        Text.Font = GameFont.Small;
                    }
                    Widgets.EndScrollView();

                    var filterClipboardID = StringCache.NetworkFilterClipBoard + $"_{Container.ParentThing.ThingID}";
                    //Copy
                    if (Widgets.ButtonImageFitted(clipboardRect, TeleContent.Copy, Color.white))
                    {
                        ClipBoardUtility.TrySetClipBoard(filterClipboardID, Container.GetFilterCopy());
                        SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                    }

                    //Paste Option
                    //TODO: DEBUG TEST VALUE
                    if (ClipBoardUtility.IsActive(filterClipboardID))
                    {
                        GUI.color = Color.gray;
                        if (Widgets.ButtonImage(clipboardInsertRect, TeleContent.Paste))
                        {
                            var clipBoard = ClipBoardUtility.TryGetClipBoard<Dictionary<NetworkValueDef, FlowValueFilterSettings>>(filterClipboardID);
                            foreach (var b in clipBoard)
                            {
                                Container.SetFilterFor(b.Key, b.Value);
                            }
                        }
                    }
                    else
                    {
                        Widgets.DrawTextureFitted(clipboardInsertRect, TeleContent.Paste, 1);
                    }
                    GUI.color = Color.white;
                });
            }
        }

        #endregion
        
        #region MainContent

        public void DrawMainContent(Rect rect)
        {
            Widgets.DrawWindowBackground(rect);
            rect = rect.ContractedBy(5);
            
            //
            Text.Font = GameFont.Tiny;

            /*if (HasSubValues)
            {
                WidgetRow subFunctionRow = new WidgetRow();
                subFunctionRow.Init(rect.x, rect.y, UIDirection.RightThenDown, gap: 0);
                foreach (var role in parentComp.Props.networkRoles)
                {
                    if (!role.HasSubValues) continue;
                    if (subFunctionRow.ButtonBox(role.ToString(), TColor.BlueHueBG, Color.gray))
                    {

                    }
                }
            }*/

            float nextY = 0;
            
            //Title
            var titleString = $"{parentComp.NetworkDef}";
            var roleString = $"{parentComp.NetworkRole}";
            Vector2 titleSize = Text.CalcSize(titleString);
            Rect titleRect = new Rect(rect.position, titleSize); // CONTENT_Rect.TopPartPixels(TITLE_Size.y);
            nextY = titleRect.yMax;

            Rect contentRect = rect.BottomPartPixels(rect.height - nextY);
            Vector2 roleTextSize = new Vector2(rect.width / 2, Text.CalcHeight(roleString, rect.width / 2));
            Rect roleTextRect = new Rect(contentRect.x + roleTextSize.x, titleRect.y, roleTextSize.x, roleTextSize.y);

            Widgets.Label(titleRect, titleString);
            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(roleTextRect, roleString);
            Text.Anchor = default;
            Text.Font = default;

            //Container Readout
            Rect ContainerGroupRect = contentRect.BottomPartPixels(26).LeftHalf();
            Rect containerRect = ContainerGroupRect.BottomPartPixels(16);
            Rect requestSliderRect = ContainerGroupRect.TopPartPixels(10);
            
            //Custom Behaviour
            if (parentComp.NetworkRole.HasFlag(NetworkRole.Requester))
            {
                //Mode
                var selectorRect = contentRect.LeftHalf().TopPartPixels(25);

                if (Widgets.ButtonText(selectorRect, parentComp.RequestWorker.Mode.ToString()))
                {
                    FloatMenu menu = new FloatMenu(new List<FloatMenuOption>()
                    {
                        new (RequesterMode.Automatic.ToString(), delegate { parentComp.RequestWorker.SetMode(RequesterMode.Automatic);}),
                        new (RequesterMode.Manual.ToString(), delegate { parentComp.RequestWorker.SetMode(RequesterMode.Manual);}),
                    },"Set Mode", true);
                    menu.vanishIfMouseDistant = true;
                    Find.WindowStack.Add(menu);
                }
                
                //Threshold
                // var getVal = parentComp.Requester.RequestedRange;
                // Widgets.DrawLineHorizontal(requestSliderRect.x, requestSliderRect.y + requestSliderRect.height / 2, requestSliderRect.width);
                //
                // TWidgets.DrawBarMarkerAt(requestSliderRect, getVal);
                // var setVal = (float)Math.Round(GUI.HorizontalSlider(requestSliderRect, getVal, 0.1f, 1f, GUIStyle.none, GUIStyle.none), 1);  // Widgets.HorizontalSlider(requestArrowRect, getVal, 0, 1f, true, roundTo: 0.01f);
                //
                //Do min-to-max range
                
                var requestRange = parentComp.RequestWorker.ReqRange; // = setVal;
                TWidgets.FloatRange(requestSliderRect, parentComp.GetHashCode(), ref requestRange, 0.01f, 1f, customSliderTex: TeleContent.CustomSlider);
                parentComp.RequestWorker.SetRange(requestRange);
                
            }

            if (parentComp.HasContainer)
            {
                Rect BarRect = containerRect.ContractedBy(2f);
                float xPos = BarRect.x;
                Widgets.DrawBoxSolid(containerRect, TColor.Black);
                Widgets.DrawBoxSolid(BarRect, TColor.White025);
                foreach (NetworkValueDef type in Container.StoredDefs)
                {
                    float percent = (Container.StoredValueOf(type) / Container.Capacity);
                    Rect typeRect = new Rect(xPos, BarRect.y, BarRect.width * percent, BarRect.height);
                    Color color = type.valueColor;
                    xPos += BarRect.width * percent;
                    Widgets.DrawBoxSolid(typeRect, color);
                }
                
                //Hover Readout
                TWidgets.HoverContainerReadout(containerRect, Container);
            }

            var padding = 5;
            var iconSize = 30;
            var width = iconSize + 2 * padding;
            var height = 2 * width;
            Rect buildOptionsRect = new Rect(contentRect.xMax - width, contentRect.yMax - height, width, height);
            Rect controllerRect = buildOptionsRect.ContractedBy(padding).TopPartPixels(iconSize);
            Rect pipeRect = buildOptionsRect.ContractedBy(padding).BottomPartPixels(iconSize);

            //Do network build options
            TWidgets.DrawBoxHighlight(buildOptionsRect);
            var controllDesignator = GenData.GetDesignatorFor<Designator_Build>(parentComp.NetworkDef.controllerDef);
            var pipeDesignator = GenData.GetDesignatorFor<Designator_Build>(parentComp.NetworkDef.transmitterDef);
            if (Widgets.ButtonImage(controllerRect, controllDesignator.icon as Texture2D))
            {
                controllDesignator.ProcessInput(Event.current);
            }

            if (Widgets.ButtonImage(pipeRect, pipeDesignator.icon as Texture2D))
            {
                pipeDesignator.ProcessInput(Event.current);
            }
        
        }

        #endregion

        #region Debug
        
        private List<FloatMenuOption> _floatMenuOptions;

        public List<FloatMenuOption> DebugOptions
        {
            get
            {
                if (_floatMenuOptions == null)
                {
                    _floatMenuOptions = new List<FloatMenuOption>();
                    float part = Container.Capacity / Container.AcceptedTypes.Count;
                    _floatMenuOptions.Add(new FloatMenuOption("Add ALL", delegate { Debug_AddAll(part); }));

                    _floatMenuOptions.Add(new FloatMenuOption("Remove ALL", Debug_Clear));

                    foreach (var type in Container.AcceptedTypes)
                    {
                        _floatMenuOptions.Add(new FloatMenuOption($"Add {type}", delegate { Debug_AddType(type, part); }));
                    }
                }
                return _floatMenuOptions;
            }
        }
        
        [SyncMethod]
        private void Debug_AddAll(float part)
        {
            foreach (var type in Container.AcceptedTypes)
            {
                Container.TryAddValue(type, part);
            }
        }

        [SyncMethod]
        private void Debug_Clear()
        {
            Container.Clear();
        }

        [SyncMethod]
        private void Debug_AddType(NetworkValueDef type, float part)
        {
            Container.TryAddValue(type, part);
        }
        
        #endregion
    }
    
    /*public class Gizmo_NetworkInfo : Gizmo
    {
        private Comp_Network compNetwork;
        private bool usesSubValues;
        private string[] cachedStrings;

        private string selectedSetting = null;
        private FloatRange extensionSettingYRange;
        private float desiredY;
        private float currentExtendedY = 0;

        private UILayout UILayout;
        private int mainWidth = 200;
        private int gizmoPadding = 5;

        private Dictionary<string, Action<Rect>> extensionSettings;
        
        public NetworkContainer Container => parentComp.Container;

        public bool HasSubValues => usesSubValues;

        private bool HasExtension
        {
            get
            {
                //Requester overview
                if (parentComp.HasContainer) return true;
                if (parentComp.NetworkRole.HasFlag(NetworkRole.Requester)) return true;
                return false;
            }
        }

        public Gizmo_NetworkInfo(Comp_Network compParent) : base()
        {
            compNetwork = compParent;
            
            //
            this.order = -250f;
        }

        public Gizmo_NetworkInfo(NetworkSubPart parent) : base()
        {
            this.order = -250f;
            this.parentComp = parent;
            if (HasExtension)
            {
                TFind.TickManager.RegisterMapUITickAction(Tick);
                SetExtensions();
                SetUpExtensionUIData();
            }

            usesSubValues = parentComp.Props.networkRoles.Any(n => n.HasSubValues);

            cachedStrings = new[]
            {
                $"{parentComp.NetworkDef}",
                $"{parentComp.NetworkRole}",
                $"Add NetworkValue",
                "Set Mode"
            };
            var currentInspectTab = Find.MainTabsRoot.OpenTab.TabWindow as MainTabWindow_Inspect;

            Vector2 POS = new Vector2(0, currentInspectTab.PaneTopY);
            Vector2 SIZE = currentInspectTab.RequestedTabSize;

            var MAINSIZE = new Vector2(mainWidth + gizmoPadding, SIZE.y);
            Rect BGRECT = new Rect(POS.x, POS.y, MAINSIZE.x, MAINSIZE.y);
            Rect MAINRECT = BGRECT.AtZero().ContractedBy(5f);

            //Popout Settings
            Rect SETTINGS_Rect = new Rect(BGRECT.x, BGRECT.y - MAINSIZE.y, BGRECT.width, BGRECT.height);
            var SETTINGS_CloseButtonSize = new Vector2(SETTINGS_Rect.width - (gizmoPadding * 2), 16); ;
            var SETTINGS_XOffsetAlignedCenter = (SETTINGS_Rect.width / 2) - (SETTINGS_CloseButtonSize.x / 2); ;
            Rect SETTINGS_CloseButtonRect = new Rect(SETTINGS_Rect.x + SETTINGS_XOffsetAlignedCenter, SETTINGS_Rect.y - SETTINGS_CloseButtonSize.y, SETTINGS_CloseButtonSize.x, SETTINGS_CloseButtonSize.y + gizmoPadding);

            var nextY = MAINRECT.y;
            
            //WidgetRow if available
            Rect WIDGETROW_Rect = Rect.zero;
            if (HasSubValues)
            {
                WIDGETROW_Rect = new Rect(MAINRECT.x, nextY, MAINRECT.width, 14);
                nextY = WIDGETROW_Rect.yMax;
            }

            //Title
            Vector2 TITLE_Size = Text.CalcSize(cachedStrings[0]);
            Rect TITLE_Rect = new Rect(new Vector2(MAINRECT.x, nextY), TITLE_Size); // CONTENT_Rect.TopPartPixels(TITLE_Size.y);
            nextY = TITLE_Rect.yMax;

            var CONTENT_Rect = MAINRECT.BottomPartPixels(MAINRECT.height - nextY);

            Vector2 ROLETEXT_Size = new Vector2(MAINRECT.width / 2, Text.CalcHeight(cachedStrings[1], MAINRECT.width / 2));
            Rect ROLETEXT_Rect = new Rect(CONTENT_Rect.x + ROLETEXT_Size.x, TITLE_Rect.y, ROLETEXT_Size.x, ROLETEXT_Size.y);

            //Container Readout
            Rect ContainerGroupRect = CONTENT_Rect.BottomPartPixels(26).LeftHalf();
            Rect CONTAINER_Rect = ContainerGroupRect.BottomPartPixels(16);
            Rect REQUESTSELECTION_Rect = ContainerGroupRect.TopPartPixels(10);

            var padding = 5;
            var iconSize = 30;
            var width = iconSize + 2 * padding;
            var height = 2 * width;
            //Designators
            Rect DESIGNATORS_Rect = new Rect(CONTENT_Rect.xMax - width, CONTENT_Rect.yMax - height, width, height);

            UILayout = new UILayout();
            UILayout.Register("BGRect", BGRECT); //
            UILayout.Register("SettingsRect", SETTINGS_Rect); //
            UILayout.Register("CloseSettingsButtonRect", SETTINGS_CloseButtonRect); //
            UILayout.Register("MainRect", MAINRECT); //
            UILayout.Register("WidgetRow", WIDGETROW_Rect);
            UILayout.Register("TitleRect", TITLE_Rect); //
            UILayout.Register("RoleReadoutRect", ROLETEXT_Rect);
            UILayout.Register("ContentRect", CONTENT_Rect); //
            UILayout.Register("ContainerRect", CONTAINER_Rect); //
            UILayout.Register("RequestSelectionRect", REQUESTSELECTION_Rect); //

            UILayout.Register("BuildOptionsRect", DESIGNATORS_Rect); //
            UILayout.Register("ControllerOptionRect", DESIGNATORS_Rect.ContractedBy(padding).TopPartPixels(iconSize)); //
            UILayout.Register("PipeOptionRect", DESIGNATORS_Rect.ContractedBy(padding).BottomPartPixels(iconSize)); //
        }

        private void Notify_ExtendHovered(bool isHovered)
        {
            desiredY = isHovered ? extensionSettingYRange.TrueMax : extensionSettingYRange.TrueMin;
        }

        private void Tick()
        {
            if (!Visible) return;
            if (Math.Abs(currentExtendedY - desiredY) > 0.01)
            {
                var val = desiredY > currentExtendedY ? 1.5f : -1.5f;
                currentExtendedY = Mathf.Clamp(currentExtendedY + val * extensionSettings.Count, extensionSettingYRange.TrueMin, extensionSettingYRange.TrueMax);
            }
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            UILayout.SetOrigin(new Vector2(topLeft.x - 15, 0));
            Rect setRect = UILayout["BGRect"];

            //Extension Button
            if (HasExtension && selectedSetting == null)
            {
                UILayout.SetOrigin(new Vector2(topLeft.x - 15, setRect.y));
                DrawSettingSelection();
            }

            Find.WindowStack.ImmediateWindow(this.GetHashCode(), setRect, WindowLayer.GameUI, delegate
            {
                UILayout.SetOrigin(new Vector2(0, 0));
                DrawMainContent(setRect.AtZero());

            }, false, false, 0);

            if (selectedSetting != null)
            {
                UILayout.SetOrigin(new Vector2(topLeft.x - 15, 0));
                DrawSelectedSetting();
            }
            return new GizmoResult(GizmoState.Mouseover);
        }

        private void DrawSettingSelection()
        {
            var mainRect = UILayout["MainRect"];
            var yMax = Math.Max(15, currentExtendedY) + 10;
            Rect extendTriggerArea = new Rect(mainRect.x, mainRect.y - (yMax - 5), mainRect.width, yMax);
            Rect extendedButton = new Rect(mainRect.x, mainRect.y - (currentExtendedY + 1), mainRect.width, currentExtendedY + 1);
            Notify_ExtendHovered(Mouse.IsOver(extendTriggerArea));

            Widgets.DrawWindowBackground(extendedButton);
            Text.Anchor = TextAnchor.MiddleCenter;
            var curY = extendedButton.y;
            foreach (var setting in extensionSettings)
            {
                if (curY > extendedButton.yMax) continue;
                Rect labelRect = new Rect(extendedButton.x, curY, extendedButton.width, Math.Min(extendedButton.height, selSettingHeight));
                Widgets.Label(labelRect, setting.Key);
                Widgets.DrawHighlightIfMouseover(labelRect);
                if (Widgets.ButtonInvisible(labelRect))
                {
                    selectedSetting = setting.Key;
                }
                curY += selSettingHeight;
            }
            Text.Anchor = default;
        }

        private void DrawMainContent(Rect rect)
        {
            Widgets.DrawWindowBackground(rect);

            //
            Text.Font = GameFont.Tiny;

            if (HasSubValues)
            {
                WidgetRow subFunctionRow = new WidgetRow();
                subFunctionRow.Init(rect.x, rect.y, UIDirection.RightThenDown, gap: 0);
                foreach (var role in parentComp.Props.networkRoles)
                {
                    if (!role.HasSubValues) continue;
                    if (subFunctionRow.ButtonBox(role.ToString(), TColor.BlueHueBG, Color.gray))
                    {

                    }
                }
            }

            Widgets.Label(UILayout["TitleRect"], cachedStrings[0]);
            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(UILayout["RoleReadoutRect"], cachedStrings[1]);
            Text.Anchor = default;
            Text.Font = default;

            //Custom Behaviour
            if (parentComp.NetworkRole.HasFlag(NetworkRole.Requester))
            {
                //Mode
                var contentRect = UILayout["ContentRect"];
                var selectorRect = contentRect.LeftHalf().TopPartPixels(25);

                if (Widgets.ButtonText(selectorRect, parentComp.RequestWorker.Mode.ToString()))
                {
                    FloatMenu menu = new FloatMenu(new List<FloatMenuOption>()
                    {
                        new (RequesterMode.Automatic.ToString(), delegate { parentComp.RequestWorker.SetMode(RequesterMode.Automatic);}),
                        new (RequesterMode.Manual.ToString(), delegate { parentComp.RequestWorker.SetMode(RequesterMode.Manual);}),
                    }, cachedStrings[3], true);
                    menu.vanishIfMouseDistant = true;
                    Find.WindowStack.Add(menu);
                }

                //Threshold
                var requestSliderRect = UILayout["RequestSelectionRect"];
                /*
                var getVal = parentComp.Requester.RequestedRange;
                Widgets.DrawLineHorizontal(requestSliderRect.x, requestSliderRect.y + requestSliderRect.height / 2, requestSliderRect.width);
                
                TWidgets.DrawBarMarkerAt(requestSliderRect, getVal);
                var setVal = (float)Math.Round(GUI.HorizontalSlider(requestSliderRect, getVal, 0.1f, 1f, GUIStyle.none, GUIStyle.none), 1);  // Widgets.HorizontalSlider(requestArrowRect, getVal, 0, 1f, true, roundTo: 0.01f);
                 #1#
                //Do min-to-max range
                var requestRange = parentComp.RequestWorker.ReqRange; // = setVal;
                TWidgets.FloatRange(requestSliderRect, parentComp.GetHashCode(), ref requestRange, 0.01f, 1f, customSliderTex: TeleContent.CustomSlider);
                parentComp.RequestWorker.SetRange(requestRange);
            }

            if (parentComp.HasContainer)
            {
                //
                Rect containerRect = UILayout["ContainerRect"];
                Rect BarRect = containerRect.ContractedBy(2f);
                float xPos = BarRect.x;
                Widgets.DrawBoxSolid(containerRect, TColor.Black);
                Widgets.DrawBoxSolid(BarRect, TColor.White025);
                foreach (NetworkValueDef type in Container.StoredDefs)
                {
                    float percent = (Container.StoredValueOf(type) / Container.Capacity);
                    Rect typeRect = new Rect(xPos, BarRect.y, BarRect.width * percent, BarRect.height);
                    Color color = type.valueColor;
                    xPos += BarRect.width * percent;
                    Widgets.DrawBoxSolid(typeRect, color);
                }

                //Draw Hovered Readout
                if (Container.FillState != ContainerFillState.Empty && Mouse.IsOver(containerRect))
                {
                    var mousePos = Event.current.mousePosition;
                    var containerReadoutSize = TWidgets.GetNetworkValueReadoutSize(Container);
                    Rect rectAtMouse = new Rect(mousePos.x, mousePos.y - containerReadoutSize.y, containerReadoutSize.x, containerReadoutSize.y);
                    TWidgets.DrawNetworkValueReadout(rectAtMouse, Container);
                }
            }

            //Do network build options
            TWidgets.DrawBoxHighlight(UILayout["BuildOptionsRect"]);
            var controllDesignator = GenData.GetDesignatorFor<Designator_Build>(parentComp.NetworkDef.controllerDef);
            var pipeDesignator = GenData.GetDesignatorFor<Designator_Build>(parentComp.NetworkDef.transmitterDef);
            if (Widgets.ButtonImage(UILayout["ControllerOptionRect"], controllDesignator.icon as Texture2D))
            {
                controllDesignator.ProcessInput(Event.current);
            }

            if (Widgets.ButtonImage(UILayout["PipeOptionRect"], pipeDesignator.icon as Texture2D))
            {
                pipeDesignator.ProcessInput(Event.current);
            }
        }

        private void DrawSelectedSetting()
        {
            var settingRect = UILayout["SettingsRect"];
            var closeButtonRect = UILayout["CloseSettingsButtonRect"];

            //
            Widgets.DrawWindowBackground(closeButtonRect);
            Widgets.DrawHighlightIfMouseover(closeButtonRect);

            Text.Anchor = TextAnchor.UpperCenter;
            //var matrix = GUI.matrix;
            //UI.RotateAroundPivot(90, closeButtonRect.center);
            Widgets.Label(closeButtonRect, "<CLOSE>");
            //GUI.matrix = matrix;
            Text.Anchor = default;

            if (Widgets.ButtonInvisible(closeButtonRect))
            {
                selectedSetting = null;
                return;
            }

            Find.WindowStack.ImmediateWindow(this.parentComp.GetHashCode(), settingRect, WindowLayer.GameUI, delegate
            {
                if (selectedSetting == null) return;
                extensionSettings[selectedSetting].Invoke(settingRect.AtZero());
            }, false, false, 0);

        }

        public override float GetWidth(float maxWidth)
        {
            return mainWidth;
        }

        private void SetUpExtensionUIData()
        {
            extensionSettingYRange = new FloatRange(10, selSettingHeight * extensionSettings.Count);
            
        }

        private void SetExtensions()
        {
            extensionSettings = new Dictionary<string, Action<Rect>>();
            if (parentComp.NetworkRole.HasFlag(NetworkRole.Requester))
            {
                extensionSettings.Add("Requester Settings", delegate (Rect rect)
                {
                    parentComp.RequestWorker.DrawSettings(rect);
                });
            }
            
            if (parentComp.HasContainer)
            {
                extensionSettings.Add("Container Settings", delegate (Rect rect)
                {
                    Widgets.DrawWindowBackground(rect);
                    TWidgets.DrawNetworkValueReadout(rect, parentComp.Container);

                    //Right Click Input
                    if (TWidgets.MouseClickIn(rect, 1) && DebugSettings.godMode)
                    {
                        FloatMenu menu = new FloatMenu(RightClickFloatMenuOptions.ToList(), cachedStrings[2], true);
                        menu.vanishIfMouseDistant = true;
                        Find.WindowStack.Add(menu);
                    }

                    //
                    //TWidgets.AbsorbInput(rect);
                });
            }

            if (parentComp.NetworkRole.HasFlag(NetworkRole.Storage))
            {
                extensionSettings.Add("Filter Settings", delegate (Rect rect)
                {
                    var readoutRect = rect.LeftPart(0.75f).ContractedBy(5).Rounded();
                    var clipboardRect = new Rect(readoutRect.xMax + 5, readoutRect.y, 22f, 22f);
                    var clipboardInsertRect = new Rect(clipboardRect.xMax + 5, readoutRect.y, 22f, 22f);

                    var listingRect = readoutRect.ContractedBy(2).Rounded();

                    Widgets.DrawWindowBackground(rect);
                    TWidgets.DrawColoredBox(readoutRect, TColor.BlueHueBG, TColor.MenuSectionBGBorderColor, 1);
                    
                    if (parentComp.Container.AcceptedTypes.NullOrEmpty())
                        return;
                    
                    //
                    Listing_Standard listing = new();
                    listing.Begin(listingRect);
                    listing.Label("Filter");
                    listing.GapLine(4);
                    listing.End();

                    var scrollOutRect = new Rect(listingRect.x, listingRect.y + listing.curY, listingRect.width, listingRect.height - listing.curY);
                    var scrollViewRect = new Rect(listingRect.x, listingRect.y + listing.curY, listingRect.width, (parentComp.Container.AcceptedTypes.Count + 1) * Text.LineHeight);

                    Widgets.DrawBoxSolid(scrollOutRect, TColor.BGDarker);
                    Widgets.BeginScrollView(scrollOutRect, ref filterScroller, scrollViewRect, false);
                    {
                        Text.Font = GameFont.Tiny;
                        var label1 = "Type";
                        var label2 = "Receive";
                        var label3 = "Store";
                        var size1 = Text.CalcSize(label1);
                        var size2 = Text.CalcSize(label2);
                        var size3 = Text.CalcSize(label3);

                        WidgetRow row = new WidgetRow(scrollViewRect.xMax, scrollViewRect.y, UIDirection.LeftThenDown);
                        row.Label(label3, size3.x);
                        row.Label(label2, size2.x);
                        row.Label(label1, scrollViewRect.width - (row.curX + size1.x));

                        float curY = scrollViewRect.y + 24;
                        foreach (var acceptedType in parentComp.Container.AcceptedTypes)
                        {
                            var settings = parentComp.Container.GetFilterFor(acceptedType);
                            var canReceive = settings.canReceive;
                            var canStore = settings.canStore;
                         
                            WidgetRow itemRow = new WidgetRow(scrollViewRect.xMax, curY, UIDirection.LeftThenDown);
                            itemRow.Checkbox( ref canStore, true, size3.x); 
                            itemRow.Highlight(size3.x);
                            itemRow.Checkbox( ref canReceive, true);
                            itemRow.Highlight(24);
                            itemRow.Label($"{acceptedType.LabelCap.CapitalizeFirst().Colorize(acceptedType.valueColor)}: ", 84);
                            itemRow.Highlight(84);

                            parentComp.Container.SetFilterFor(acceptedType, new FlowValueFilterSettings()
                            {
                                canReceive = canReceive,
                                canStore = canStore
                            });
                            
                            //
                            curY += 24;
                        }
                        
                        Text.Font = GameFont.Small;
                    }
                    Widgets.EndScrollView();

                    var filterClipboardID = StringCache.NetworkFilterClipBoard + $"_{Container.ParentThing.ThingID}";
                    //Copy
                    if (Widgets.ButtonImageFitted(clipboardRect, TeleContent.Copy, Color.white))
                    {
                        ClipBoardUtility.TrySetClipBoard(filterClipboardID, Container.GetFilterCopy());
                        SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                    }

                    //Paste Option
                    //TODO: DEBUG TEST VALUE
                    if (ClipBoardUtility.IsActive(filterClipboardID))
                    {
                        GUI.color = Color.gray;
                        if (Widgets.ButtonImage(clipboardInsertRect, TeleContent.Paste))
                        {
                            var clipBoard = ClipBoardUtility.TryGetClipBoard<Dictionary<NetworkValueDef, FlowValueFilterSettings>>(filterClipboardID);
                            foreach (var b in clipBoard)
                            {
                                Container.SetFilterFor(b.Key, b.Value);
                            }
                        }
                    }
                    else
                    {
                        Widgets.DrawTextureFitted(clipboardInsertRect, TeleContent.Paste, 1);
                    }
                    GUI.color = Color.white;
                });
            }
        }

        private Vector2 filterScroller = Vector2.zero;

        [SyncMethod]
        private void Debug_AddAll(float part)
        {
            foreach (var type in Container.AcceptedTypes)
            {
                Container.TryAddValue(type, part);
            }
        }

        [SyncMethod]
        private void Debug_Clear()
        {
            Container.Clear();
        }

        [SyncMethod]
        private void Debug_AddType(NetworkValueDef type, float part)
        {
            Container.TryAddValue(type, part);
        }

        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
        {
            get
            {
                float part = Container.Capacity / Container.AcceptedTypes.Count;
                yield return new FloatMenuOption("Add ALL", delegate { Debug_AddAll(part); });

                yield return new FloatMenuOption("Remove ALL", Debug_Clear);

                foreach (var type in Container.AcceptedTypes)
                {
                    yield return new FloatMenuOption($"Add {type}", delegate
                    {
                        Debug_AddType(type, part);
                    });
                }
            }
        }
    }*/
}
