using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Multiplayer.API;
using RimWorld;
using TeleCore.Static;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TeleCore
{
    public class Gizmo_NetworkInfo : Gizmo
    {
        private NetworkComponent parentComp;
        private string[] cachedStrings;

        private UILayout UILayout;
        private int mainWidth = 200;
        private int selSettingHeight = 22;
        private int gizmoPadding = 5;

        private Dictionary<string, Action<Rect>> extensionSettings;

        public NetworkContainer Container => parentComp.Container;

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

        public Gizmo_NetworkInfo(NetworkComponent parent) : base()
        {
            this.order = -250f;
            this.parentComp = parent;
            if (HasExtension)
            {
                //TODO: MapUI ticks? clear these when map is cleared but tick it like UI
                TFind.TickManager.RegisterUITickAction(Tick);
                SetExtensions();
                SetUpExtensionUIData();
            }

            cachedStrings = new[]
            {
                $"{parentComp.NetworkDef}",
                $"{parentComp.NetworkRole}",
                $"Add NetworkValue",
                "Set Mode"
            };
            UILayout = new UILayout();
            //
            var currentInspectTab = Find.MainTabsRoot.OpenTab.TabWindow as MainTabWindow_Inspect;
            Vector2 pos = new Vector2(0, currentInspectTab.PaneTopY);
            Vector2 size = currentInspectTab.RequestedTabSize;
            Vector2 titleSize = Text.CalcSize(cachedStrings[0]);

            var mainSize = new Vector2(mainWidth + gizmoPadding, size.y);
            Rect bgRect = new Rect(pos.x, pos.y, mainSize.x, mainSize.y);
            Rect settingsRect = new Rect(bgRect.x, bgRect.y - mainSize.y, bgRect.width, bgRect.height);

            var r = bgRect.AtZero();

            var settingCloseSize = new Vector2(settingsRect.width - (gizmoPadding * 2), 16);
            var halfWidth = (settingsRect.width / 2) - (settingCloseSize.x / 2);
            Rect closeSettingsRect = new Rect(settingsRect.x + halfWidth, settingsRect.y - settingCloseSize.y, settingCloseSize.x, settingCloseSize.y + gizmoPadding);

            Rect mainRect = r.ContractedBy(5f);
            Rect widgetRowRect = new Rect(mainRect.x, mainRect.y, mainRect.width, 20);
            Rect titleRect = new Rect(mainRect.x, widgetRowRect.yMax, titleSize.x, titleSize.y);
            Vector2 roleTextSize = new Vector2(mainRect.width / 2, Text.CalcHeight(cachedStrings[1], mainRect.width / 2));
            Rect roleReadoutRect = new Rect(mainRect.x + roleTextSize.x, widgetRowRect.yMax, roleTextSize.x, roleTextSize.y);
            Rect contentRect = new Rect(mainRect.x, titleRect., mainRect.width, mainRect.height - titleRect.height);
            Rect containerBarRect = new Rect(contentRect.x, contentRect.yMax - 16, contentRect.width / 2, 16);
            Rect requestSelectionRect = new Rect(contentRect.x, containerBarRect.y - 10, contentRect.width / 2, 10);
            var padding = 5;
            var iconSize = 30;
            var width = iconSize + 2 * padding;
            var height = 2 * width;
            Rect buildOptionsRect = new Rect(contentRect.xMax - width, mainRect.yMax - height, width, height);

            UILayout.Register("BGRect", bgRect); //
            UILayout.Register("SettingsRect", settingsRect); //
            UILayout.Register("CloseSettingsButtonRect", closeSettingsRect); //
            UILayout.Register("MainRect", mainRect); //
            UILayout.Register("WidgetRow", widgetRowRect);
            UILayout.Register("TitleRect", titleRect); //
            UILayout.Register("RoleReadoutRect", roleReadoutRect);
            UILayout.Register("ContentRect", contentRect); //
            UILayout.Register("ContainerRect", containerBarRect); //
            UILayout.Register("RequestSelectionRect", requestSelectionRect); //
            UILayout.Register("BuildOptionsRect", buildOptionsRect); //
            UILayout.Register("ControllerOptionRect", buildOptionsRect.ContractedBy(padding).TopPartPixels(iconSize)); //
            UILayout.Register("PipeOptionRect", buildOptionsRect.ContractedBy(padding).BottomPartPixels(iconSize)); //
        }

        private string selectedSetting = null;
        private FloatRange extensionSettingYRange;
        private float desiredY;
        private float currentExtendedY = 0;

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

            WidgetRow subFunctionRow = new WidgetRow();
            subFunctionRow.Init(rect.x, rect.y, UIDirection.RightThenDown, gap: 0);
            foreach (var role in parentComp.Props.networkRoles)
            {
                if (!role.HasSubValues) continue;
                if (subFunctionRow.ButtonBox(role.ToString(), TColor.BlueHueBG, Color.gray))
                {

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

                if (Widgets.ButtonText(selectorRect, parentComp.RequesterMode.ToString()))
                {
                    FloatMenu menu = new FloatMenu(new List<FloatMenuOption>()
                    {
                        new (RequesterMode.Automatic.ToString(), delegate { parentComp.RequesterMode = RequesterMode.Automatic;}),
                        new (RequesterMode.Manual.ToString(), delegate { parentComp.RequesterMode = RequesterMode.Manual;}),
                    }, cachedStrings[3], true);
                    menu.vanishIfMouseDistant = true;
                    Find.WindowStack.Add(menu);
                }

                //Threshold
                var requestArrowRect = UILayout["RequestSelectionRect"];
                var getVal = parentComp.RequestedCapacityPercent;
                Widgets.DrawLineHorizontal(requestArrowRect.x, requestArrowRect.y + requestArrowRect.height / 2, requestArrowRect.width);
                TWidgets.DrawBarMarkerAt(requestArrowRect, getVal);
                var setVal = (float)Math.Round(GUI.HorizontalSlider(requestArrowRect, getVal, 0.1f, 1f, GUIStyle.none, GUIStyle.none), 1);  // Widgets.HorizontalSlider(requestArrowRect, getVal, 0, 1f, true, roundTo: 0.01f);
                parentComp.RequestedCapacityPercent = setVal;
            }

            if (parentComp.HasContainer)
            {
                //
                Rect containerRect = UILayout["ContainerRect"];
                Rect BarRect = containerRect.ContractedBy(2f);
                float xPos = BarRect.x;
                Widgets.DrawBoxSolid(containerRect, TColor.Black);
                Widgets.DrawBoxSolid(BarRect, TColor.White025);
                foreach (NetworkValueDef type in Container.AllStoredTypes)
                {
                    float percent = (Container.ValueForType(type) / Container.Capacity);
                    Rect typeRect = new Rect(xPos, BarRect.y, BarRect.width * percent, BarRect.height);
                    Color color = type.valueColor;
                    xPos += BarRect.width * percent;
                    Widgets.DrawBoxSolid(typeRect, color);
                }

                //Draw Hovered Readout
                if (!Container.Empty && Mouse.IsOver(containerRect))
                {
                    var mousePos = Event.current.mousePosition;
                    var containerReadoutSize = TWidgets.GetNetworkValueReadoutSize(Container);
                    Rect rectAtMouse = new Rect(mousePos.x, mousePos.y - containerReadoutSize.y, containerReadoutSize.x, containerReadoutSize.y);
                    Widgets.DrawMenuSection(rectAtMouse);
                    TWidgets.DrawNetworkValueReadout(rectAtMouse, Container);
                }
            }

            //Do network build options
            TWidgets.DrawBoxHighlight(UILayout["BuildOptionsRect"]);
            var controllDesignator = StaticData.GetDesignatorFor<Designator_Build>(parentComp.NetworkDef.controllerDef);
            var pipeDesignator = StaticData.GetDesignatorFor<Designator_Build>(parentComp.NetworkDef.transmitterDef);
            if (Widgets.ButtonImage(UILayout["ControllerOptionRect"], controllDesignator.icon))
            {
                controllDesignator.ProcessInput(Event.current);
            }

            if (Widgets.ButtonImage(UILayout["PipeOptionRect"], pipeDesignator.icon))
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
                    Widgets.DrawWindowBackground(rect);

                    var contentRect = rect.ContractedBy(5);
                    Widgets.BeginGroup(contentRect);
                    contentRect = contentRect.AtZero();

                    var curX = 5;
                    var allowedTypes = parentComp.Props.AllowedValuesByRole[NetworkRole.Requester];
                    foreach (var type in allowedTypes)
                    {
                        Rect typeRect = new Rect(curX, contentRect.height - 15, 10, 10);
                        Rect typeSliderSetting = new Rect(curX, contentRect.height - (20 + 100), 10, 100);
                        Rect typeFilterRect = new Rect(curX, typeSliderSetting.y - 10, 10, 10);
                        Widgets.DrawBoxSolid(typeRect, type.valueColor);

                        var previous = parentComp.RequestedTypes[type];
                        var previousValue = previous.Item2;
                        var previousBool = previous.Item1;

                        //
                        var newValue = TWidgets.VerticalSlider(typeSliderSetting, previousValue, 0, Container.Capacity, 0.01f);
                        Widgets.Checkbox(typeFilterRect.position, ref previousBool, 10);

                        parentComp.RequestedTypes[type] = (previousBool, newValue);

                        var totalRequested = parentComp.RequestedTypes.Values.Sum(v => v.Item2);
                        if (totalRequested > Container.Capacity)
                        {
                            if (previousValue < newValue)
                            {
                                foreach (var type2 in allowedTypes)
                                {
                                    if (type2 == type) continue;
                                    var val = parentComp.RequestedTypes[type2].Item2;
                                    val = Mathf.Lerp(val, 0, 1f - newValue);
                                    parentComp.RequestedTypes[type2] = (parentComp.RequestedTypes[type2].Item1, val);
                                    //val = Mathf.Lerp(0, val, 1f - Mathf.InverseLerp(0, Container.Capacity, newValue));
                                    //parentComp.RequestedTypes[type2] = Mathf.Clamp(parentComp.RequestedTypes[type2] - (diff / (parentComp.RequestedTypes.Count - 1)), 0, Container.Capacity);
                                }
                            }
                        }
                        curX += 20 + 5;
                    }
                    Widgets.EndGroup();

                    //TWidgets.AbsorbInput(rect);
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
                    Listing_Standard listing = new();
                    listing.Begin(listingRect);
                    listing.Label("Allowed Types");
                    listing.GapLine(4);
                    listing.End();

                    var scrollOutRect = new Rect(listingRect.x, listingRect.y + listing.curY, listingRect.width, listingRect.height - listing.curY);
                    var scrollViewRect = new Rect(listingRect.x, listingRect.y + listing.curY, listingRect.width, (parentComp.Container.AcceptedTypes.Count + 1) * Text.LineHeight);

                    Widgets.DrawBoxSolid(scrollOutRect, TColor.BGDarker);
                    Widgets.BeginScrollView(scrollOutRect, ref filterScroller, scrollViewRect, false);
                    {
                        Listing_Standard listingScroll = new Listing_Standard();
                        listingScroll.Begin(scrollViewRect);

                        foreach (var acceptedType in parentComp.Container.AcceptedTypes)
                        {
                            var boolVal = parentComp.Container.AcceptsType(acceptedType);
                            listingScroll.CheckboxLabeled($"{acceptedType.LabelCap.CapitalizeFirst().Colorize(acceptedType.valueColor)}: ", ref boolVal);
                            parentComp.Container.Notify_FilterChanged(acceptedType, boolVal);
                        }

                        listingScroll.End();
                    }
                    Widgets.EndScrollView();

                    //Copy
                    if (Widgets.ButtonImageFitted(clipboardRect, TeleContent.Copy, Color.white))
                    {
                        ClipBoardUtility.TrySetClipBoard(StringCache.NetworkFilterClipBoard, Container.Filter.Copy());
                        SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                    }

                    //Paste Option
                    var clipBoard = ClipBoardUtility.TryGetClipBoard<Dictionary<NetworkValueDef, bool>>(StringCache.NetworkFilterClipBoard);
                    bool clipBoardInsertActive = clipBoard != null && clipBoard.All(t => Container.AcceptedTypes.Contains(t.Key));
                    GUI.color = clipBoardInsertActive ? Color.white : Color.gray;
                    if (clipBoardInsertActive)
                    {
                        if (Widgets.ButtonImage(clipboardInsertRect, TeleContent.Paste))
                        {
                            foreach (var b in clipBoard)
                            {
                                Container.Notify_FilterChanged(b.Key, b.Value);
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
                Container.TryAddValue(type, part, out _);
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
            Container.TryAddValue(type, part, out _);
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
    }
}
