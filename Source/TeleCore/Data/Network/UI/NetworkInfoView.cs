using System;
using System.Collections.Generic;
using System.Linq;
using Multiplayer.API;
using RimWorld;
using TeleCore.Network.Data;
using TeleCore.Network.Flow;
using TeleCore.Network.Utility;
using UnityEngine;
using Verse;

namespace TeleCore.Network.UI;

public class NetworkInfoView
{
    //
    private readonly INetworkPart _part;

    //
    private Vector2 filterScroller = Vector2.zero;

    public NetworkInfoView(INetworkPart part)
    {
        //
        _part = part;

        //
        SetExtensions();
        ExtendoRange = new FloatRange(10, Gizmo_NetworkOverview.selSettingHeight * ExtendoTabs.Count);
    }

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
        var roleString = $"{_part.Config.roles}";
        var titleSize = Text.CalcSize(titleString);
        var titleRect = new Rect(rect.position, titleSize); // CONTENT_Rect.TopPartPixels(TITLE_Size.y);
        nextY = titleRect.yMax;

        var contentRect = rect.BottomPartPixels(rect.height - nextY);
        var roleTextSize = new Vector2(rect.width / 2, Text.CalcHeight(roleString, rect.width / 2));
        var roleTextRect = new Rect(contentRect.x + roleTextSize.x, titleRect.y, roleTextSize.x, roleTextSize.y);

        Widgets.Label(titleRect, titleString);
        Text.Anchor = TextAnchor.UpperRight;
        Widgets.Label(roleTextRect, roleString);
        Text.Anchor = default;
        Text.Font = default;

        //Container Readout
        var ContainerGroupRect = contentRect.BottomPartPixels(26).LeftHalf();
        var containerRect = ContainerGroupRect.BottomPartPixels(16);
        var requestSliderRect = ContainerGroupRect.TopPartPixels(10);

        //Custom Behaviour
        if ((_part.Config.roles & NetworkRole.Requester) == NetworkRole.Requester)
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
            var BarRect = containerRect.ContractedBy(2f);
            var xPos = BarRect.x;
            Widgets.DrawBoxSolid(containerRect, TColor.Black);
            Widgets.DrawBoxSolid(BarRect, TColor.White025);
            foreach (NetworkValueDef type in _part.Volume.Stack.Values)
            {
                var percent = _part.Volume.StoredPercentOf(type);
                var typeRect = new Rect(xPos, BarRect.y, BarRect.width * percent, BarRect.height);
                var color = type.valueColor;
                xPos += BarRect.width * percent;
                Widgets.DrawBoxSolid(typeRect, color);
            }

            //Hover Readout
            FlowUI<NetworkValueDef>.DrawFlowBoxReadout(containerRect, _part.Volume);
        }

        var padding = 5;
        var iconSize = 30;
        var width = iconSize + 2 * padding;
        var height = 2 * width;
        var buildOptionsRect = new Rect(contentRect.xMax - width, contentRect.yMax - height, width, height);
        var controllerRect = buildOptionsRect.ContractedBy(padding).TopPartPixels(iconSize);
        var pipeRect = buildOptionsRect.ContractedBy(padding).BottomPartPixels(iconSize);

        //Do network build options
        TWidgets.DrawBoxHighlight(buildOptionsRect);
        var controllDesignator = GenData.GetDesignatorFor<Designator_Build>(_part.Config.networkDef.controllerDef);
        var pipeDesignator = GenData.GetDesignatorFor<Designator_Build>(_part.Config.networkDef.transmitterDef);
        if (Widgets.ButtonImage(controllerRect, controllDesignator.icon as Texture2D))
            controllDesignator.ProcessInput(Event.current);

        if (Widgets.ButtonImage(pipeRect, pipeDesignator.icon as Texture2D)) pipeDesignator.ProcessInput(Event.current);
    }

    #endregion

    #region Properties

    public bool HasExtensions
    {
        get
        {
            //Requester overview
            if (_part.HasContainer) return true;
            if ((_part.Config.roles & NetworkRole.Requester) == NetworkRole.Requester) return true;
            return false;
        }
    }

    //
    public NetworkVolume NetworkVolume => _part.Volume;

    //Extendo Tabs
    public Dictionary<string, Action<Rect>> ExtendoTabs { get; private set; }

    public string ExtendedTab { get; private set; }
    public FloatRange ExtendoRange { get; private set; }

    #endregion

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
            ExtendoTabs[ExtendedTab].Invoke(rect.AtZero());
        }, false, false, 0);
    }

    //
    private void SetExtensions()
    {
        ExtendoTabs = new Dictionary<string, Action<Rect>>();
        if ((_part.Config.roles & NetworkRole.Requester) == NetworkRole.Requester)
        {
            ExtendoTabs.Add("Requester Settings", delegate
            {
                //TODO: Replace with extended filter settings (setting how much can be received/taken)
                //_part.RequestWorker.DrawSettings(rect);
            });
        }

        if (_part.HasContainer)
            ExtendoTabs.Add("Container Settings", delegate(Rect rect)
            {
                Widgets.DrawWindowBackground(rect);
                FlowUI<NetworkValueDef>.DrawFlowBoxReadout(rect, _part.Volume);

                //Right Click Input
                if (TWidgets.MouseClickIn(rect, 1) && DebugSettings.godMode)
                {
                    var menu = new FloatMenu(DebugOptions.ToList(), $"{_part.Config.roles}", true);
                    menu.vanishIfMouseDistant = true;
                    Find.WindowStack.Add(menu);
                }
                //
                //TWidgets.AbsorbInput(rect);
            });

        if (_part.Config.roles.HasFlag(NetworkRole.Storage))
            ExtendoTabs.Add("Filter Settings", delegate(Rect rect)
            {
                var readoutRect = rect.LeftPart(0.75f).ContractedBy(5).Rounded();
                var clipboardRect = new Rect(readoutRect.xMax + 5, readoutRect.y, 22f, 22f);
                var clipboardInsertRect = new Rect(clipboardRect.xMax + 5, readoutRect.y, 22f, 22f);

                var listingRect = readoutRect.ContractedBy(2).Rounded();

                Widgets.DrawWindowBackground(rect);
                TWidgets.DrawColoredBox(readoutRect, TColor.BlueHueBG, TColor.MenuSectionBGBorderColor, 1);

                if (_part.Volume.AcceptedTypes.NullOrEmpty())
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
                    (_part.Volume.AcceptedTypes.Count + 1) * Text.LineHeight);

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

                    var row = new WidgetRow(scrollViewRect.xMax, scrollViewRect.y, UIDirection.LeftThenDown);
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
                var part = (float) (_part.Volume.MaxCapacity / (float) _part.Volume.AcceptedTypes.Count);
                _floatMenuOptions.Add(new FloatMenuOption("Add ALL", delegate { Debug_AddAll(part); }));

                _floatMenuOptions.Add(new FloatMenuOption("Remove ALL", Debug_Clear));

                foreach (var type in _part.Volume.AcceptedTypes)
                    _floatMenuOptions.Add(new FloatMenuOption($"Add {type}", delegate { Debug_AddType(type, part); }));
            }

            return _floatMenuOptions;
        }
    }

    [SyncMethod]
    private void Debug_AddAll(double part)
    {
        foreach (var type in NetworkVolume.AcceptedTypes)
            NetworkVolume.TryAdd(type, part);
    }

    [SyncMethod]
    private void Debug_Clear()
    {
        NetworkVolume.Clear();
    }

    [SyncMethod]
    private void Debug_AddType(NetworkValueDef type, float part)
    {
        NetworkVolume.TryAdd(type, part);
    }

    #endregion
}