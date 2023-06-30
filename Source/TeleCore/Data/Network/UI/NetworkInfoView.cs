using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Multiplayer.API;
using RimWorld;
using TeleCore.Defs;
using TeleCore.Generics.Container;
using TeleCore.Network.Data;
using TeleCore.Network.Flow;
using TeleCore.Network.Utility;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TeleCore.Network.UI;

public class NetworkInfoView
{
    //
    private INetworkPart _part;
    private Dictionary<string, Action<Rect>> tabDrawActions;

    //
    private Vector2 filterScroller = Vector2.zero;

    #region Properties

    public bool HasExtensions
    {
        get
        {
            //Requester overview
            if (_part.HasContainer) return true;
            if ((_part.Config.role & NetworkRole.Requester) == NetworkRole.Requester) return true;
            return false;
        }
    }

    //
    public Flow.FlowBox FlowBox => _part.FlowBox;

    //Extendo Tabs
    public Dictionary<string, Action<Rect>> ExtendoTabs => tabDrawActions;
    public string ExtendedTab { get; private set; }
    public FloatRange ExtendoRange { get; private set; }

    #endregion

    public NetworkInfoView(INetworkPart part) : base()
    {
        //
        _part = part;

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
        Find.WindowStack.ImmediateWindow(_part.GetHashCode(), rect, WindowLayer.GameUI, delegate
        {
            if (ExtendedTab == null) return;
            tabDrawActions[ExtendedTab].Invoke(rect.AtZero());
        }, false, false, 0);
    }

    //
    private void SetExtensions()
    {
        tabDrawActions = new Dictionary<string, Action<Rect>>();
        if ((_part.Config.role & NetworkRole.Requester) == NetworkRole.Requester)
        {
            ExtendoTabs.Add("Requester Settings", delegate(Rect rect)
            {
                //TODO: Replace with extended filter settings (setting how much can be received/taken)
                //_part.RequestWorker.DrawSettings(rect);
            });
        }

        if (_part.HasContainer)
        {
            ExtendoTabs.Add("Container Settings", delegate(Rect rect)
            {
                Widgets.DrawWindowBackground(rect);
                NetworkUI.DrawFlowBoxReadout(rect, _part.FlowBox);

                //Right Click Input
                if (TWidgets.MouseClickIn(rect, 1) && DebugSettings.godMode)
                {
                    FloatMenu menu = new FloatMenu(DebugOptions.ToList(), $"{_part.Config.role}", true);
                    menu.vanishIfMouseDistant = true;
                    Find.WindowStack.Add(menu);
                }

                //
                //TWidgets.AbsorbInput(rect);
            });
        }

        if (_part.Config.role.HasFlag(NetworkRole.Storage))
        {
            ExtendoTabs.Add("Filter Settings", delegate(Rect rect)
            {
                var readoutRect = rect.LeftPart(0.75f).ContractedBy(5).Rounded();
                var clipboardRect = new Rect(readoutRect.xMax + 5, readoutRect.y, 22f, 22f);
                var clipboardInsertRect = new Rect(clipboardRect.xMax + 5, readoutRect.y, 22f, 22f);

                var listingRect = readoutRect.ContractedBy(2).Rounded();

                Widgets.DrawWindowBackground(rect);
                TWidgets.DrawColoredBox(readoutRect, TColor.BlueHueBG, TColor.MenuSectionBGBorderColor, 1);

                if (_part.FlowBox.AcceptedTypes.NullOrEmpty())
                    return;

                //
                Listing_Standard listing = new();
                listing.Begin(listingRect);
                listing.Label("Filter");
                listing.GapLine(4);
                listing.End();

                var scrollOutRect = new Rect(listingRect.x, listingRect.y + listing.curY, listingRect.width,
                    listingRect.height - listing.curY);
                var scrollViewRect = new Rect(listingRect.x, listingRect.y + listing.curY, listingRect.width,
                    (_part.FlowBox.AcceptedTypes.Count + 1) * Text.LineHeight);

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

                    //TODO: ADD FLOWBOX FILTER SETTINGS
                    /*float curY = scrollViewRect.y + 24;
                    foreach (var acceptedType in _part.FlowBox.AcceptedTypes)
                    {
                        var settings = _part.Container.GetFilterFor(acceptedType);
                        var canReceive = settings.canReceive;
                        var canStore = settings.canStore;

                        WidgetRow itemRow = new WidgetRow(scrollViewRect.xMax, curY, UIDirection.LeftThenDown);
                        itemRow.Checkbox(ref canStore, _part.Container.Filter.CanChange, size3.x);
                        itemRow.Highlight(size3.x);
                        itemRow.Checkbox(ref canReceive, _part.Container.Filter.CanChange);
                        itemRow.Highlight(24);
                        itemRow.Label($"{acceptedType.LabelCap.CapitalizeFirst().Colorize(acceptedType.valueColor)}: ",
                            84);
                        itemRow.Highlight(84);

                        _part.Container.SetFilterFor(acceptedType, new FlowValueFilterSettings()
                        {
                            canReceive = canReceive,
                            canStore = canStore
                        });

                        //
                        curY += 24;
                    }*/

                    Text.Font = GameFont.Small;
                }
                Widgets.EndScrollView();

                //TODO: Add filter for flowbox
                /*var filterClipboardID = StringCache.NetworkFilterClipBoard + $"_{Container.ParentThing.ThingID}";
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
                        var clipBoard =
                            ClipBoardUtility.TryGetClipBoard<Dictionary<NetworkValueDef, FlowValueFilterSettings>>(
                                filterClipboardID);
                        foreach (var b in clipBoard)
                        {
                            Container.SetFilterFor(b.Key, b.Value);
                        }
                    }
                }
                else
                {
                    Widgets.DrawTextureFitted(clipboardInsertRect, TeleContent.Paste, 1);
                }*/

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
        float nextY = 0;
        
        //Title
        var titleString = $"{_part.Config.networkDef}";
        var roleString = $"{_part.Config.role}";
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
        if ((_part.Config.role & NetworkRole.Requester) == NetworkRole.Requester)
        {
            //Mode
            var selectorRect = contentRect.LeftHalf().TopPartPixels(25);

            /*if (Widgets.ButtonText(selectorRect, _part.RequestWorker.Mode.ToStringSafe()))
            {
                FloatMenu menu = new FloatMenu(new List<FloatMenuOption>()
                {
                    new(RequesterMode.Automatic.ToStringSafe(),
                        delegate { _part.RequestWorker.SetMode(RequesterMode.Automatic); }),
                    new(RequesterMode.Manual.ToStringSafe(),
                        delegate { _part.RequestWorker.SetMode(RequesterMode.Manual); }),
                }, "Set Mode", true);
                menu.vanishIfMouseDistant = true;
                Find.WindowStack.Add(menu);
            }*/

            //Threshold
            // var getVal = parentComp.Requester.RequestedRange;
            // Widgets.DrawLineHorizontal(requestSliderRect.x, requestSliderRect.y + requestSliderRect.height / 2, requestSliderRect.width);
            //
            // TWidgets.DrawBarMarkerAt(requestSliderRect, getVal);
            // var setVal = (float)Math.Round(GUI.HorizontalSlider(requestSliderRect, getVal, 0.1f, 1f, GUIStyle.none, GUIStyle.none), 1);  // Widgets.HorizontalSlider(requestArrowRect, getVal, 0, 1f, true, roundTo: 0.01f);
            //
            //Do min-to-max range

            // var requestRange = _part.RequestWorker.ReqRange; // = setVal;
            // TWidgets.FloatRange(requestSliderRect, _part.GetHashCode(), ref requestRange, 0.01f, 1f,
            //     customSliderTex: TeleContent.CustomSlider);
            // _part.RequestWorker.SetRange(requestRange);

        }

        if (_part.HasContainer)
        {
            Rect BarRect = containerRect.ContractedBy(2f);
            float xPos = BarRect.x;
            Widgets.DrawBoxSolid(containerRect, TColor.Black);
            Widgets.DrawBoxSolid(BarRect, TColor.White025);
            foreach (NetworkValueDef type in _part.FlowBox.Stack.Values)
            {
                float percent = _part.FlowBox.StoredPercentOf(type);
                Rect typeRect = new Rect(xPos, BarRect.y, BarRect.width * percent, BarRect.height);
                Color color = type.valueColor;
                xPos += BarRect.width * percent;
                Widgets.DrawBoxSolid(typeRect, color);
            }

            //Hover Readout
            NetworkUI.DrawFlowBoxReadout(containerRect, _part.FlowBox);
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
        var controllDesignator = GenData.GetDesignatorFor<Designator_Build>(_part.Config.networkDef.controllerDef);
        var pipeDesignator = GenData.GetDesignatorFor<Designator_Build>(_part.Config.networkDef.transmitterDef);
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
                float part = (float)(_part.FlowBox.MaxCapacity / (float)_part.FlowBox.AcceptedTypes.Count);
                _floatMenuOptions.Add(new FloatMenuOption("Add ALL", delegate { Debug_AddAll(part); }));

                _floatMenuOptions.Add(new FloatMenuOption("Remove ALL", Debug_Clear));

                foreach (var type in _part.FlowBox.AcceptedTypes)
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
        foreach (var type in FlowBox.AcceptedTypes)
        {
            FlowBox.TryAdd(type, part);
        }
    }

    [SyncMethod]
    private void Debug_Clear()
    {
        FlowBox.Clear();
    }

    [SyncMethod]
    private void Debug_AddType(FlowValueDef type, float part)
    {
        FlowBox.TryAdd(type, part);
    }

    #endregion
}