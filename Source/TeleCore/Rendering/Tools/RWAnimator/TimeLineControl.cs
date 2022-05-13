using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TeleCore
{
    public interface IKeyFramedElement
    {
        //public KeyFrameData EditData { get; }
        public KeyFrameData CurrentData { get; }

        public UIElement Owner { get; }
    }

    public class TimeLineControl : UIElement
    {
        //
        private const int _PixelsPerTick = 4;

        //TimeLineSettings
        private int currentFrameInt;
        private bool isPaused = true;
        private IntRange _tempReplayBounds;

        //Animation Data
        public AnimationPartValue CurrentPart => Canvas.CurrentAnimationPart;

        public Dictionary<IKeyFramedElement, Dictionary<int, KeyFrame>> AnimationPartFrames => CurrentPart.InternalFrames;
        public bool HasAnimation => Canvas.AnimationData.CurrentAnimations.Any();
        public int AnimationLength => CurrentPart.frames;

        public List<UIElement> Elements => Canvas.Children;

        //UI
        private float zoomFactor = 1f;
        private FloatRange zoomRange = new FloatRange(0.25f, 2f);

        private Vector2 elementScrollPos = Vector2.zero;
        private Vector2 timeLineScrollPos = Vector2.zero;

        private float TimeSelectorPctPos => CurrentFrame / (float)AnimationLength;

        private float CurrentZoom
        {
            get => zoomFactor;
            set => zoomFactor = Mathf.Clamp(value, zoomRange.min, zoomRange.max);
        }

        private float CurrentFrameXOffset => CurrentFrame * PixelPerTickAdjusted;
        private float PixelPerTickAdjusted => _PixelsPerTick * CurrentZoom;
        private float TimeLineLength => AnimationLength * PixelPerTickAdjusted;

        private IntRange ReplayBounds
        {
            get => CurrentPart.ReplayBounds;
            set => CurrentPart.ReplayBounds = value;
        }

        public int CurrentFrame
        {
            get => currentFrameInt;
            private set => currentFrameInt = Mathf.Clamp(value, 0, AnimationLength);
        }

        public TextureCanvas Canvas { get; set; }
        public IKeyFramedElement SelectedElement => Canvas.ActiveTexture;

        public TimeLineControl() : base(UIElementMode.Static)
        {
            TFind.TickManager.RegisterUITickAction(delegate
            {
                if (isPaused) return;
                if (CurrentFrame >= ReplayBounds.max)
                {
                    CurrentFrame = ReplayBounds.min;
                    return;
                }
                CurrentFrame++;
            });
        }


        //KeyFrames
        //Adding
        public void SetKeyFrameFor(IKeyFramedElement element)
        {
            TLog.Message($"Setting New KeyFrame for {element}");
            UpdateKeyframeFor(element, GetDataFor(element));
        }

        //Removing - Remove known keyframe at current frame
        public void RemoveKeyFrameFor(IKeyFramedElement element)
        {
            if (AnimationPartFrames[element].ContainsKey(CurrentFrame))
            {
                AnimationPartFrames[element].Remove(CurrentFrame);
            }
        }

        //Updating - Set new KeyFrameData at current frame for element
        public void UpdateKeyframeFor(IKeyFramedElement element, KeyFrameData data)
        {
            KeyFrame updatedFrame = new KeyFrame(data, CurrentFrame);
            //If frame exists, set new keyframe with new data at frame
            if (AnimationPartFrames[element].ContainsKey(CurrentFrame))
            {
                AnimationPartFrames[element][CurrentFrame] = updatedFrame;
                return;
            }
            //Set new keyframe at current frame
            AnimationPartFrames[element].Add(CurrentFrame, updatedFrame);
        }

        public bool IsAtKeyFrame(IKeyFramedElement element, out KeyFrame frameAt)
        {
            return AnimationPartFrames[element].TryGetValue(CurrentFrame, out frameAt);
        }

        //
        public bool GetKeyFrames(IKeyFramedElement element, out KeyFrame? frame1, out KeyFrame? frame2, out float dist)
        {
            frame1 = frame2 = null;
            var frames = AnimationPartFrames[element];
            var framesMin = frames.Where(t => t.Key <= CurrentFrame);
            var framesMax = frames.Where(t => t.Key >= CurrentFrame);
            if (framesMin.TryMaxBy(t=> t.Key, out var value1))
                frame1 = value1.Value;

            if (framesMax.TryMinBy(t=> t.Key, out var value2))
                frame2 = value2.Value;
            dist = Mathf.InverseLerp(frame1?.Frame ?? 0, frame2?.Frame ?? 0, CurrentFrame);

            return frame1 != null && frame2 != null;
        }

        public KeyFrame GetKeyFrame(IKeyFramedElement element)
        {
            if (IsAtKeyFrame(element, out KeyFrame frame)) return frame;
            if (GetKeyFrames(element, out var frame1, out var frame2, out var lerpVal))
                return new KeyFrame(frame1.Value.Data.Interpolated(frame2.Value.Data, lerpVal), CurrentFrame);
            if (frame1.HasValue)
                return frame1.Value;
            if (frame2.HasValue)
                return frame2.Value;
            return KeyFrame.Invalid;
        }

        public KeyFrameData GetDataFor(IKeyFramedElement element)
        {
            //if (!AnimationPartFrames.ContainsKey(element)) return element.KeyFrameData;
            if (IsAtKeyFrame(element, out KeyFrame frame)) return frame.Data;
            if (GetKeyFrames(element, out var frame1, out var frame2, out var lerpVal))
                return frame1.Value.Data.Interpolated(frame2.Value.Data, lerpVal);

            if (frame1.HasValue)
                return frame1.Value.Data;
            if (frame2.HasValue)
                return frame2.Value.Data;

            return new KeyFrameData(Vector2.zero, 0, Vector2.one);
        }

        protected override void HandleEvent_Custom(Event ev, bool inContext)
        {
            base.HandleEvent_Custom(ev);

            if (ev.type == EventType.KeyDown)
            {
                if (ev.keyCode == KeyCode.LeftArrow)
                    CurrentFrame--;
                if (ev.keyCode == KeyCode.RightArrow)
                    CurrentFrame++;
            }
            /*
            if (ev.isScrollWheel)
            {
                CurrentZoom += Input.mouseScrollDelta.y/10f;
            }
            if (ev.type == EventType.MouseDown && ev.button == 2)
            {
                CurrentZoom = 1f;
            }
            */
        }

        protected override void DrawContentsBeforeRelations(Rect inRect)
        {
            int topSize = 40;
            int leftSize = 125;
            int elementSize = 25;
            int timeLineContract = 10;

            Rect leftRect = inRect.LeftPartPixels(leftSize);
            Rect rightRect = inRect.RightPartPixels(inRect.width - leftSize);

            Rect topLeft = leftRect.TopPartPixels(topSize);
            Rect botLeft = leftRect.BottomPartPixels(inRect.height - topSize);

            Rect topRight = rightRect.TopPartPixels(topSize);
            Rect botRight = rightRect.BottomPartPixels(inRect.height - topSize);

            if (!HasAnimation)
            {
                /*
                var workingRect = inRect.LeftPartPixels(260);
                TWidgets.DrawColoredBox(workingRect, TColor.BGDarker, TColor.MenuSectionBGBorderColor, 1);
                workingRect = workingRect.ContractedBy(5);
                Listing_Standard listing = new Listing_Standard();
                listing.Begin(workingRect.LeftPartPixels(250));

                if (!Canvas.Initialized)
                {
                    GUI.color = Color.red;
                    listing.Label("Animation Set Not Ready");
                    GUI.color = Color.white;
                    listing.End();
                    return;
                }

                listing.Label("New Animation Settings");
                listing.GapLine();
                listing.TextFieldLabeled("Tag:" ,ref animationPartName, anchor:TextAnchor.MiddleLeft);
                listing.TextFieldNumericLabeled("Length:", ref timeSeconds, ref _timeSecondsBuffer, anchor:TextAnchor.MiddleLeft);

                listing.Gap();
                if (listing.ButtonText("Add"))
                {
                    Canvas.AnimationDataDef.Notify_CreateNewAnimationPart(animationPartName, timeSeconds);
                    animationPartName = "Animation_Tag";
                    timeSeconds = 5;
                    _timeSecondsBuffer = "5";
                }

                listing.End();
                */
                return;
            }

            TimeControlButtons(TopRect.RightPartPixels(TopRect.width - leftSize));

            //Element List Scroller
            int elementListCount = Elements.Count;
            Rect elementListViewRect = new Rect(botLeft.x, botLeft.y, botLeft.width, (elementListCount * elementSize));

            //Time Line Scroller
            Rect timeLineSelectorRect = new Rect(topRight.x, topRight.y, TimeLineLength, topRight.height);
            Rect timelineViewRect = new Rect(topRight.x, topRight.y, TimeLineLength, rightRect.height).ExpandedBy(timeLineContract, 0);
            Rect timelineBotViewRect = new Rect(botRight.x, botRight.y, TimeLineLength, elementListViewRect.height);

            //
            float curY = 0;
            Widgets.BeginScrollView(botLeft, ref elementScrollPos, elementListViewRect, false);
            {
                curY = elementListViewRect.y;
                foreach (var element in AnimationPartFrames.Keys)
                {
                    Rect left = new Rect(botLeft.x, curY, botLeft.width, elementSize);
                    ElementListing(left, element);
                    curY += elementSize;
                }
            }
            Widgets.EndScrollView();

            //
            Widgets.DrawBoxSolid(rightRect, TColor.BGDarker);
            Widgets.ScrollHorizontal(rightRect, ref timeLineScrollPos, timelineViewRect);
            Widgets.BeginScrollView(rightRect, ref timeLineScrollPos, timelineViewRect, false);
            {
                DrawTimeSelector(timeLineSelectorRect, timelineViewRect.height);
            }
            Widgets.EndScrollView();

            timeLineScrollPos = new Vector2(timeLineScrollPos.x, elementScrollPos.y);
            GUI.BeginScrollView(botRight, timeLineScrollPos, timelineBotViewRect, GUIStyle.none, GUIStyle.none);
            {
                curY = timelineBotViewRect.y;
                foreach (var element in AnimationPartFrames.Keys)
                {
                    Rect right = new Rect(timelineBotViewRect.x, curY, TimeLineLength, elementSize).ContractedBy(timeLineContract, 0);
                    ElementTimeLine(right, element);
                    curY += elementSize;
                }
            }
            GUI.EndScrollView();

            TWidgets.DrawBox(rightRect, TColor.MenuSectionBGBorderColor, 1);
        }

        private void DrawTimeSelector(Rect timeBar, float totalHeight)
        {
            GUI.color = TColor.MenuSectionBGBorderColor;
            Widgets.DrawLineHorizontal(timeBar.x, timeBar.yMax, timeBar.width);
            GUI.color = Color.white;

            //Draw Selector
            _tempReplayBounds = ReplayBounds;
            DrawTimeSelectorCustom(timeBar, GetHashCode(), ref _tempReplayBounds, ref currentFrameInt, totalHeight, 0, AnimationLength);
            ReplayBounds = _tempReplayBounds;

            //Draw Tick Lines
            float curX = timeBar.x;
            for (int i = 0; i <= AnimationLength; i++)
            {
                bool bigOne = i % 60 == 0;
                var length = bigOne ? 8 : 4;
                var pos = new Vector2(curX, timeBar.yMax - (length + 1));
                Widgets.DrawLineVertical(pos.x, pos.y, length);
                if (bigOne)
                {
                    var label = $"{i / 60}s";
                    pos -= new Vector2(0, 4);
                    TWidgets.DoTinyLabel(pos.RectOnPos(Text.CalcSize(label)), label);
                }
                curX += PixelPerTickAdjusted;
            }
        }

        private void DrawTimeSelectorCustom(Rect rect, int id, ref IntRange range, ref int value, float verticalHeight, int min = 0, int max = 100, int minWidth = 0)
        {
            //Custom Line
            /*
            GUI.color = Widgets.RangeControlTextColor;
            Rect barRect = new Rect(rect.x, rect.y + 8f, rect.width, 2f);
            GUI.DrawTexture(barRect, BaseContent.WhiteTex);
            GUI.color = Color.white;
            */

            //Selector Positions
            float leftX = rect.x + (range.min * PixelPerTickAdjusted); //marginRect.width * (float)(range.min - min) / (float)(max - min);
            float rightX = rect.x + (range.max * PixelPerTickAdjusted); //marginRect.width * (float)(range.max - min) / (float)(max - min);
            float valX = rect.x + (value * PixelPerTickAdjusted); //marginRect.width * (float) (value - min) / (float) (max - min);
            Rect rangeLPos = new Rect(leftX, rect.y, 16f, 16f);
            Rect rangeRPos = new Rect(rightX - 16f, rect.y, 16f, 16f);
            Rect rangeLBar = new Rect(rangeLPos.position, new Vector2(rangeLPos.width, rect.height));
            Rect rangeRBar = new Rect(rangeRPos.position, new Vector2(rangeRPos.width, rect.height));
            Rect selectorRect = new Rect(valX-12, rect.y, 25, 25);
            GUI.DrawTexture(rangeLPos, TeleContent.TimeSelRangeL);
            GUI.DrawTexture(rangeRPos, TeleContent.TimeSelRangeR);

            Widgets.DrawLineVertical(valX, selectorRect.yMax - 5, verticalHeight);

            if(Widgets.curDragEnd == Widgets.RangeEnd.None && (Widgets.draggingId == id || Mouse.IsOver(selectorRect)))
                GUI.color = Color.cyan;

            GUI.DrawTexture(selectorRect, TeleContent.TimeSelMarker);
            GUI.color = Color.white;

            //valPos = new Rect(valPos.x + 8, valPos.y, valPos.width, valPos.height);
            //Widgets.Label(valPos, $"{CurrentFrame}");

            if(Widgets.curDragEnd == Widgets.RangeEnd.Min || Mouse.IsOver(rangeLBar))
                Widgets.DrawHighlight(rangeLBar);
            if (Widgets.curDragEnd == Widgets.RangeEnd.Max || Mouse.IsOver(rangeRBar))
                Widgets.DrawHighlight(rangeRBar);

            if (!UIEventHandler.IsFocused(this)) return;

            var mouseUp = Event.current.type == EventType.MouseUp;
            if ( (mouseUp || Event.current.rawType == EventType.MouseDown))
            {
                if (mouseUp)
                    Widgets.curDragEnd = Widgets.RangeEnd.None;
                Widgets.draggingId = 0;
                SoundDefOf.DragSlider.PlayOneShotOnCamera(null);
            }
            bool flag = false;
            if (Mouse.IsOver(rect) || Widgets.draggingId == id)
            {
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && id != Widgets.draggingId)
                {
                    Widgets.draggingId = id;
                    float x = Event.current.mousePosition.x;
                    if (x >= rangeLPos.xMin && x <= rangeLPos.xMax)
                    {
                        Widgets.curDragEnd = Widgets.RangeEnd.Min;
                    }
                    else if (x >= rangeRPos.xMin && x <= rangeRPos.xMax)
                    {
                        Widgets.curDragEnd = Widgets.RangeEnd.Max;
                    }
                    else
                    {
                        Widgets.curDragEnd = Widgets.RangeEnd.None;
                        /*
                        float num3 = Mathf.Abs(x - rangeLPos.xMax);
                        float num4 = Mathf.Abs(x - (rangeRPos.x - 16f));
                        Widgets.curDragEnd = ((num3 < num4) ? Widgets.RangeEnd.Min : Widgets.RangeEnd.Max);
                        */
                    }
                    flag = true;
                    Event.current.Use();
                    SoundDefOf.DragSlider.PlayOneShotOnCamera(null);
                }
                if (flag || (Event.current.type == EventType.MouseDrag))
                {
                    int num5 = Mathf.RoundToInt(Mathf.Clamp((Event.current.mousePosition.x - rect.x) / rect.width * (float)(max - min) + (float)min, (float)min, (float)max));

                    //Value Selection
                    if (Widgets.curDragEnd == Widgets.RangeEnd.None)
                    {
                        int newSliderVal = Mathf.RoundToInt(Mathf.Clamp((Event.current.mousePosition.x - rect.x) / rect.width * (float)(max - min) + (float)min, (float)min, (float)max));
                        value = Mathf.Clamp(newSliderVal, min, max);
                    }
                    //Range selection
                    if (Widgets.curDragEnd == Widgets.RangeEnd.Min)
                    {
                        if (num5 != range.min)
                        {
                            range.min = num5;
                            if (range.min > max - minWidth)
                            {
                                range.min = max - minWidth;
                            }
                            int num6 = Mathf.Max(min, range.min + minWidth);
                            if (range.max < num6)
                            {
                                range.max = num6;
                            }
                        }
                    }
                    else if (Widgets.curDragEnd == Widgets.RangeEnd.Max && num5 != range.max)
                    {
                        range.max = num5;
                        if (range.max < min + minWidth)
                        {
                            range.max = min + minWidth;
                        }
                        int num7 = Mathf.Min(max, range.max - minWidth);
                        if (range.min > num7)
                        {
                            range.min = num7;
                        }
                    }
                    Widgets.CheckPlayDragSliderSound();
                    Event.current.Use();
                }
            }
        }

        private void ElementListing(Rect leftRect, IKeyFramedElement element)
        {
            TWidgets.DrawBox(leftRect, SelectedElement  == element ? Color.cyan : TColor.White05, 1);
            Widgets.Label(leftRect, $"{element.Owner.Label}");
            if (Widgets.ButtonImage(leftRect.RightPartPixels(20), TeleContent.DeleteX))
            {
                AnimationPartFrames[element].Clear();
            }
        }

        private void TryStartOrDragKeyframe(KeyFrame keyframe, Rect keyFrameRect)
        {

        }

        private void ElementTimeLine(Rect rightRect, IKeyFramedElement element)
        {
            Widgets.DrawHighlightIfMouseover(rightRect);
            GUI.color = SelectedElement == element ? Color.white : TColor.White025;
            var yPos = rightRect.y + rightRect.height / 2f;
            Widgets.DrawLineHorizontal(rightRect.x, yPos, rightRect.width);
            GUI.color = Color.white;

            GetKeyFrames(element, out var frame1, out var frame2, out _);
            var elements = AnimationPartFrames[element].Values;
            var elementData = GetDataFor(element);
            var atKeyFrame = GetKeyFrame(element);
            foreach (var keyFrame in elements)
            {
                Rect rect = new Vector2(rightRect.x + keyFrame.Frame * PixelPerTickAdjusted, yPos).RectOnPos(new Vector2(20, 20));
                TooltipHandler.TipRegion(rect, $"Frame:\n{element.CurrentData}");

                //Dragger
                TryStartOrDragKeyframe(keyFrame, rect);

                if (keyFrame.Equals(frame1))
                    GUI.color = Color.magenta;
                if (keyFrame.Equals(frame2))
                    GUI.color = Color.cyan;

                //if(IsAtKeyFrame(element, out var data) && data == keyFrame && element == SelectedElement)

                if(atKeyFrame == keyFrame)
                    GUI.color = Color.green;

                Widgets.DrawTextureFitted(rect, TeleContent.KeyFrame, 1f);

                GUI.color = Color.white;
            }
        }

        private void TimeControlButtons(Rect topPart)
        {
            Widgets.BeginGroup(topPart);
            WidgetRow row = new WidgetRow();
            if (row.ButtonIcon(TeleContent.PlayPause))
            {
                isPaused = !isPaused;
            }

            //Add at current frame
            if (row.ButtonIcon(TeleContent.AddKeyFrame))
            {
                foreach (var element in AnimationPartFrames.Keys)
                {
                    SetKeyFrameFor(element);
                }
            }

            //Remove at current frame
            if (row.ButtonIcon(TeleContent.AddKeyFrame,backgroundColor:Color.red))
            {
                foreach (var element in AnimationPartFrames.Keys)
                {
                    RemoveKeyFrameFor(element);
                }
            }

            row.Gap(8);
            row.Slider(125, ref zoomFactor, zoomRange.min, zoomRange.max);

            if (Canvas.ActiveTexture != null)
            {
                //Canvas.ActiveTexture.SetTRSP_Direct(rot: Canvas.ActiveTexture.TRotation);
            }
            row.Gap(8);
            row.Label($"[{CurrentFrame}][{Math.Round(CurrentFrame.TicksToSeconds(),2)}s]");
            row.Init(0, Rect.height - 16, UIDirection.LeftThenUp);
            Widgets.EndGroup();
        }
    }
}
