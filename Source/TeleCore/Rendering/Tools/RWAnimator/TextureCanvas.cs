using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public class TextureCanvas : UIElement, IDragAndDropReceiver
    {
        //
        internal const int BufferSize = 2 + 2 + 2 + 4 + 1;
        internal const int _TileSize = 100;
        internal const float CanvasSize = 5;
        internal static FloatRange SizeRange = new FloatRange(0.01f, CanvasSize);
        internal static Vector2 TileVector = new Vector2(_TileSize, _TileSize);

        //
        private Dictionary<KeyFrame, string[]> StringBuffers = new();

        //Layer Scroller
        private TextureLayerView layerView;

        //Internal Data
        private AnimationMetaData animationMetaData;

        public AnimationMetaData AnimationData
        {
            get => animationMetaData;
        }

        public bool Initialized => animationMetaData.Initialized;
        public bool ReadyToAnimate => animationMetaData.SelectedAnimationPart != null;

        public bool CanWork => Initialized && ReadyToAnimate;

        public override List<UIElement> Children => animationMetaData.CurrentElementList;

        public AnimationPartValue CurrentAnimationPart => animationMetaData.SelectedAnimationPart;
        public TextureElement ActiveTexture => layerView.ActiveElement;
        public TimeLineControl TimeLine { get; set; }

        public override string Label => "Canvas";
        public override UIContainerMode ContainerMode => UIContainerMode.Reverse;

        private Rect TextureLayerViewRect => new Rect((Position.x + Size.x) - 1, Position.y, 150, Size.y);
        private Rect DataReadoutRect => new Rect(InRect.xMax - 250, InRect.y + 1, 250, 750);
        public Rect MetaDataViewRect => new Rect((CanWork ? TextureLayerViewRect.xMax : TextureLayerViewRect.x) - 1, Position.y, 500 + 1, 250);


        //Render Data
        private Vector2? oldDragPos;
        private float zoomScale = 1;
        private FloatRange scaleRange = new FloatRange(0.5f, 20);

        //Scroller
        private Vector2 partListingScrollPos = Vector2.zero;

        public Vector2 Origin => InRect.AtZero().center + DragPos;
        public Vector2 TrueOrigin => InRect.center + DragPos;
        public Vector2 DragPos { get; private set; } = Vector2.zero;
        private Vector2 LimitSize => (Size * 1f) * CanvasZoomScale;

        public float CanvasZoomScale => zoomScale;

        //Settings
        public bool DrawMetaDataSetting { get; private set; } = true;
        public bool DrawElementPropertiesSetting { get; private set; }

        //States
        private bool CanDrawEelementProperties => ActiveTexture != null && DrawElementPropertiesSetting;

        //Event stuff
        public Vector2 MousePos => (Event.current.mousePosition - TrueOrigin) / CanvasZoomScale;

        public TextureCanvas(UIElementMode mode) : base(mode)
        {
            animationMetaData = new AnimationMetaData(this);
            layerView = new TextureLayerView(this);
            UIDragNDropper.RegisterAcceptor(this);
        }

        //
        public string[] BufferFor(KeyFrame frame)
        {
            if (StringBuffers.ContainsKey(frame))
            {
                return StringBuffers[frame];
            }
            var buffer = new string[BufferSize];
            frame.Data.UpdateBuffer(buffer);
            StringBuffers.Add(frame, buffer);
            return StringBuffers[frame];
        }

        public void Reset()
        {
            animationMetaData = new AnimationMetaData(this);
            layerView = new TextureLayerView(this);
        }

        public void Notify_LoadedElement(UIElement loadedElement)
        {
            loadedElement.SetProperties(parent: this);
            base.Notify_AddedElement(loadedElement);
        }

        protected override void Notify_AddedElement(UIElement newElement)
        {
            base.Notify_AddedElement(newElement);
            layerView.Notify_NewLayer(newElement as TextureElement);
            if (AnimationData.Loading) return;
            AnimationData.CurrentAnimations.ForEach(a => a.InternalFrames.Add(newElement as TextureElement, new Dictionary<int, KeyFrame>()));
        }

        protected override void Notify_RemovedElement(UIElement newElement)
        {
            base.Notify_RemovedElement(newElement);
            layerView.Notify_RemovedLayer(newElement);

            //
            if (newElement is not IKeyFramedElement framedElement) return;
            foreach (var animation in AnimationData.CurrentAnimations)
            {
                if (animation.InternalFrames.ContainsKey(framedElement))
                {
                    animation.InternalFrames.Remove(framedElement);
                }
            }
        }

        public override void Notify_ElementSelected(UIElement element, int index)
        {
            animationMetaData.SetElementIndex(index);
        }

        protected override void Notify_ChildElementChanged(UIElement element)
        {
            base.Notify_ChildElementChanged(element);
        }

        private void Notify_SideChanged()
        {
            layerView.Notify_SelectIndex(AnimationData.ElementIndex);
        }

        protected override void HandleEvent_Custom(Event ev, bool inContext = false)
        {
            if (!CanWork) return;

            if (Mouse.IsOver(InRect))
            {
                if (CanDrawEelementProperties && Mouse.IsOver(DataReadoutRect))
                {
                    if(ev.type == EventType.MouseDown)
                        UIEventHandler.StartFocus(this, DataReadoutRect);
                }
                else
                {
                    if (IsFocused && ev.button == 0)
                    {
                        if (ev.type == EventType.MouseDown)
                        {
                            oldDragPos = DragPos;
                        }

                        //Pan
                        if (ev.type == EventType.MouseDrag && oldDragPos.HasValue)
                        {
                            var dragDiff = (CurrentDragDiff);
                            var oldDrag = oldDragPos.Value;
                            DragPos = new Vector2(oldDrag.x + dragDiff.x, oldDrag.y + dragDiff.y);
                        }
                    }

                    //Zoom
                    if (ev.type == EventType.ScrollWheel)
                    {
                        var zoomDelta = (ev.delta.y / _TileSize) * zoomScale;
                        zoomScale = Mathf.Clamp(zoomScale - zoomDelta, scaleRange.min, scaleRange.max);
                        if (zoomScale < scaleRange.max && zoomScale > scaleRange.min)
                            DragPos += MousePos * zoomDelta;
                    }
                }

                //Clear data always
                if (ev.type == EventType.MouseUp)
                {
                    oldDragPos = null;
                }
            }
        }

        private void DoTopBarControls()
        {
            //TopBar
            Rect rightTop = TopRect.RightPartPixels(60);
            Rect viewMetaData = rightTop.RightHalf();
            Rect viewElementProperties = rightTop.LeftHalf();
            if (Widgets.ButtonImage(viewMetaData, TeleContent.HightlightInMenu))
            {
                DrawMetaDataSetting = !DrawMetaDataSetting;
            }
            if (Widgets.ButtonImage(viewElementProperties, TeleContent.HightlightInMenu))
            {
                DrawElementPropertiesSetting = !DrawElementPropertiesSetting;
            }
        }

        protected override void DrawContentsBeforeRelations(Rect inRect)
        {
            if (!Initialized) return;
            DrawGrid(inRect);
        }

        protected override void DrawContentsAfterRelations(Rect inRect)
        {
            DoTopBarControls();
            if (Initialized)
            {
                if (DrawMetaDataSetting)
                {
                    DrawAnimationMetaData();
                }

                if (!CanWork)
                {
                    var workingRect = Origin.RectOnPos(new Vector2(260, 130)).Rounded();
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(workingRect, "Missing Animation Parts");
                    Text.Anchor = default;
                    return;
                }
            }
            else
            {
                var workingRect = Origin.RectOnPos(new Vector2(260, 130)).Rounded();
                TWidgets.DrawColoredBox(workingRect, TColor.BGDarker, TColor.MenuSectionBGBorderColor, 1);
                workingRect = workingRect.ContractedBy(5);
                Listing_Standard listing = new Listing_Standard();
                listing.Begin(workingRect);

                listing.Label("New Animation Set");
                listing.GapLine();
                listing.TextFieldLabeled("DefName:", ref animationMetaData.defName, anchor: TextAnchor.MiddleLeft);

                if (listing.ButtonText("Init"))
                {
                    AnimationData.Notify_Init();
                }

                listing.End();
                return;
            }

            //LayerView
            layerView.DrawElement(TextureLayerViewRect);

            //
            var element = UIEventHandler.FocusedElement as UIElement;
            var texElement = element as TextureElement;
            TWidgets.DoTinyLabel(inRect, $"Focused: {element}[{(element)?.RenderLayer}]\n{Event.current.mousePosition}\n{MousePos}\n{(element)?.CurrentDragDiff}" + $"\n{((element?.StartDragPos - (texElement?.TextureRect.center)) ?? Vector2.zero).normalized}");

            CanvasCursor.Notify_TriggeredMode(ActiveTexture?.LockedInMode);

            if (CanDrawEelementProperties)
            {
                DrawReadout(DataReadoutRect, ActiveTexture);
            }
        }

        private void DrawGrid(Rect inRect)
        {
            Widgets.BeginGroup(inRect);
            DrawCanvasGuidelines();
            Widgets.EndGroup();
        }

        private void DrawAnimationMetaData()
        {
            Rect rect = MetaDataViewRect;
            Widgets.DrawMenuSection(rect);
            var leftRect = rect.LeftPartPixels(300).ContractedBy(5).Rounded();
            var rightRect = rect.RightPartPixels(200).Rounded();
            //
            var animationOutRect = rightRect.LeftPartPixels(100).ContractedBy(5).Rounded();
            var availableSetsOutRect = rightRect.RightPartPixels(100).ContractedBy(5).Rounded();

            //
            var animationLabelRect = animationOutRect.TopPart(0.1f).Rounded();
            var animationButtonRect = animationOutRect.BottomPart(0.1f).Rounded();
            var animationListingRect = new Rect(animationOutRect.x, animationLabelRect.yMax, animationOutRect.width, animationOutRect.height - (animationLabelRect.height + animationButtonRect.height));

            //Available Sets By Rotation
            var availableSetsLabelRect = availableSetsOutRect.TopPart(0.1f).Rounded();
            var availableSetsListingRect = availableSetsOutRect.BottomPart(0.9f).Rounded();

            var animationPartEditRect = leftRect.BottomPart(0.5f).Rounded();

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(leftRect.ContractedBy(5));

            listing.Label($"Animation Set Collection");
            listing.GapLine();

            Text.Anchor = TextAnchor.MiddleLeft;
            listing.TextFieldLabeled("DefName: ", ref animationMetaData.defName, TextAnchor.MiddleLeft);
            Text.Anchor = default;

            listing.End();

            TWidgets.DrawColoredBox(animationPartEditRect, TColor.BGDarker, TColor.MenuSectionBGBorderColor, 1);
            var curPart = AnimationData.SelectedAnimationPart;
            if (curPart != null)
            {
                Listing_Standard partListing = new Listing_Standard();
                partListing.Begin(animationPartEditRect.ContractedBy(5));

                partListing.Label($"Edit '{curPart.tag}'");
                partListing.GapLine(8);
                partListing.TextFieldLabeled("Tag:", ref curPart.tag, anchor: TextAnchor.MiddleLeft);
                partListing.TextFieldNumericLabeled("Animation Length:", ref curPart._internalSeconds, ref curPart._internalSecondsBuffer, anchor: TextAnchor.MiddleLeft);

                if (curPart.InternalDifference)
                {
                    if (partListing.ButtonText("Set New Length"))
                    {
                        curPart.SetFrames();
                    }
                }

                partListing.End();
            }

            Widgets.Label(animationLabelRect, "Animations");
            var partsViewRect = new Rect(animationListingRect.x, animationListingRect.y, animationListingRect.width,animationMetaData.CurrentAnimations.Count * 20);
            TWidgets.DrawColoredBox(animationListingRect, TColor.BlueHueBG, Color.gray, 1);
            Widgets.BeginScrollView(animationListingRect, ref partListingScrollPos, partsViewRect);
            {
                float curY = animationListingRect.y;
                foreach (var animationPart in animationMetaData.CurrentAnimations)
                {
                    Rect partListing = new Rect(animationListingRect.x, curY, animationListingRect.width, 20);

                    var color = Mouse.IsOver(partListing) || (AnimationData.SelectedAnimationPart == animationPart) ? TColor.White025 : TColor.White005;
                    Widgets.DrawBoxSolid(partListing.ContractedBy(2).Rounded(), color);
                    Widgets.Label(partListing, animationPart.tag);
                    if (Widgets.ButtonInvisible(partListing))
                    {
                        animationMetaData.SetAnimationPart(animationPart);
                    }
                    curY += 20;
                }
            }
            Widgets.EndScrollView();
            if (Widgets.ButtonText(animationButtonRect, "Add Part"))
            {
                var animation = AnimationData.Notify_CreateNewAnimationPart("Undefined", 1);
                foreach (var element in Children)
                {
                    animation.InternalFrames.Add((IKeyFramedElement)element, new Dictionary<int, KeyFrame>());
                }
            }

            Widgets.Label(availableSetsLabelRect, "Available Sets");
            TWidgets.DrawColoredBox(availableSetsListingRect, TColor.BlueHueBG, Color.gray, 1);
            float curYNew = availableSetsListingRect.y;
            for (int i = 0; i < 4; i++)
            {
                if (AnimationData.AnimationPartsFor(i))
                {
                    Rect setSelectionListing = new Rect(availableSetsListingRect.x, curYNew, availableSetsListingRect.width, 20);

                    var color = Mouse.IsOver(setSelectionListing) || (AnimationData.CurRot.AsInt == i) ? TColor.White025 : TColor.White005;
                    Widgets.DrawBoxSolid(setSelectionListing.ContractedBy(2).Rounded(), color);
                    Widgets.Label(setSelectionListing, new Rot4(i).ToStringHuman());
                    if (Widgets.ButtonInvisible(setSelectionListing))
                    {
                        AnimationData.SetRotation(new Rot4(i));
                        Notify_SideChanged();
                    } 
                    curYNew += 20;
                }
            }
        }

        private void DrawReadout(Rect rect, TextureElement tex)
        {
            //Transform
            Widgets.DrawMenuSection(rect);
            var ev = Event.current;
            //var mousePos = ev.mousePosition;

            //
            var xSize = tex.TSize.x;
            var ySize = tex.TSize.y;
            //
            var xPos = tex.TPosition.x;
            var yPos = tex.TPosition.y;
            //
            var rot = tex.TRotation;
            //
            var xPivot = tex.PivotPoint.x;
            var yPivot = tex.PivotPoint.y;
            //TexCoords
            var xCoords = tex.TexCoords.x;
            var yCoords = tex.TexCoords.y;
            var widthCoords = tex.TexCoords.width;
            var heightCoords = tex.TexCoords.height;

            //Extra
            var attachScript = tex.AttachScript;
            var layerTag = tex.LayerTag;
            var layerIndex = tex.LayerIndex;

            //Buffer
            var buffer = tex.ValueBuffer;

            //
            bool flag = ev.type == EventType.KeyDown;

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(rect.ContractedBy(4));
            listing.Label($"Transform".Bold());
            listing.GapLine();

            listing.Label("Size:");
            listing.DoBGForNext(TColor.White025);
            listing.TextFieldNumericLabeled("X", ref xSize, ref buffer[0], SizeRange.TrueMin, SizeRange.TrueMax, anchor: TextAnchor.MiddleLeft);
            listing.DoBGForNext(TColor.White025);
            listing.TextFieldNumericLabeled("Y", ref ySize, ref buffer[1], SizeRange.TrueMin, SizeRange.TrueMax, anchor: TextAnchor.MiddleLeft);

            listing.Label("Position:");
            listing.DoBGForNext(TColor.White025);
            listing.TextFieldNumericLabeled("X", ref xPos, ref buffer[2], float.MinValue, anchor: TextAnchor.MiddleLeft);
            listing.DoBGForNext(TColor.White025);
            listing.TextFieldNumericLabeled("Y", ref yPos, ref buffer[3], float.MinValue, anchor: TextAnchor.MiddleLeft);

            listing.Label("Rotation:");
            listing.DoBGForNext(TColor.White025);
            listing.TextFieldNumericLabeled("Rot", ref rot, ref buffer[4], int.MinValue, int.MaxValue, TextAnchor.MiddleLeft);

            listing.GapLine();

            listing.Label($"PivotPoint:");
            listing.DoBGForNext(TColor.White025);
            listing.TextFieldNumericLabeled("X", ref xPivot, ref buffer[5], float.MinValue, anchor: TextAnchor.MiddleLeft);
            listing.DoBGForNext(TColor.White025);
            listing.TextFieldNumericLabeled("Y", ref yPivot, ref buffer[6], float.MinValue, anchor: TextAnchor.MiddleLeft);
            listing.Label($"TexCoords:");
            listing.DoBGForNext(TColor.White025);
            listing.TextFieldNumericLabeled("X", ref xCoords, ref buffer[7], float.MinValue, anchor: TextAnchor.MiddleLeft);
            listing.DoBGForNext(TColor.White025);
            listing.TextFieldNumericLabeled("Y", ref yCoords, ref buffer[8], float.MinValue, anchor: TextAnchor.MiddleLeft);
            listing.DoBGForNext(TColor.White025);
            listing.TextFieldNumericLabeled("Width", ref widthCoords, ref buffer[9], float.MinValue, anchor: TextAnchor.MiddleLeft);
            listing.DoBGForNext(TColor.White025);
            listing.TextFieldNumericLabeled("Height", ref heightCoords, ref buffer[10], float.MinValue, anchor: TextAnchor.MiddleLeft);

            listing.GapLine();
            listing.Label($"Extra".Bold());

            listing.TextFieldLabeled("Tag Label: ", ref layerTag, TextAnchor.MiddleLeft);

            var layerIndexBuffer = tex.Data.StringBuffer;
            listing.TextFieldNumericLabeled("Layer: ", ref layerIndex, ref layerIndexBuffer, 0, int.MaxValue, TextAnchor.MiddleLeft);
            tex.StringBuffer = layerIndexBuffer;
            
            listing.CheckboxLabeled("Attach Script: ", ref attachScript);

            if (listing.ButtonTextLabeled("TextAnchor: ", $"{tex.TexCoordAnchor}"))
            {
                var floatOptions = new List<FloatMenuOption>();
                foreach (var value in Enum.GetValues(typeof(TexCoordAnchor)))
                {
                    floatOptions.Add(new FloatMenuOption($"{value}", delegate
                    {
                        tex.TexCoordAnchor = (TexCoordAnchor)value;
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(floatOptions));
            }

            listing.GapLine();
            listing.Label($"View".Bold());
            var viewTexCoords = tex.ShowTexCoordGhost;
            listing.CheckboxLabeled("View TexCoords: ", ref viewTexCoords);


            if (attachScript != tex.AttachScript)
            {
                tex.AttachScript = attachScript;
            }

            if (viewTexCoords != tex.ShowTexCoordGhost)
            {
                tex.ShowTexCoordGhost = viewTexCoords;
            }

            if (flag)
            {
                var newSize = new Vector2(xSize, ySize);
                var newPos = new Vector2(xPos, yPos);
                var newPivot = new Vector2(xPivot, yPivot);
                var newTexCoords = new Rect(xCoords, yCoords, widthCoords, heightCoords);
                //
                if (newSize != tex.TSize)
                    tex.TSize = newSize;
                if (newPos != tex.TPosition)
                    tex.TPosition = newPos;
                if (rot != tex.TRotation)
                    tex.TRotation = rot;
                if (newPivot != tex.PivotPoint)
                    tex.PivotPoint = newPivot;
                if (newTexCoords != tex.TexCoords)
                    tex.TexCoords = newTexCoords;
                //
                if (layerTag != tex.LayerTag)
                    tex.LayerTag = layerTag;

                if (layerIndex != tex.LayerIndex)
                    tex.LayerIndex = layerIndex;
            }

            listing.End();
        }

        private void DrawChildProperties(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(rect.ContractedBy(4));
            listing.Label("Sub Parts");
            listing.GapLine();

            foreach (var part in ActiveTexture.SubParts)
            {
                listing.TextureElement(part);
            }

            listing.End();
        }

        private void DrawCanvasGuidelines()
        {
            //Limit rect
            var dimension = 5;
            var tileSize = _TileSize * CanvasZoomScale;
            var limitSize = (new Vector2(tileSize, tileSize) * dimension);
            var canvasRect = Origin.RectOnPos(limitSize).Rounded();
            TWidgets.DrawColoredBox(canvasRect, TColor.BGDarker, TColor.White05, 1);

            GUI.color = TColor.White025;
            var curX = canvasRect.x;
            var curY = canvasRect.y;
            for (int x = 0; x < dimension; x++)
            {
                Widgets.DrawLineVertical(curX, canvasRect.y, canvasRect.height);
                Widgets.DrawLineHorizontal(canvasRect.x, curY, canvasRect.width);
                curY += tileSize;
                curX += tileSize;
            }

            GUI.color = TColor.White05;
            Widgets.DrawLineHorizontal(Origin.x - limitSize.x / 2, Origin.y, limitSize.x);
            Widgets.DrawLineVertical(Origin.x, Origin.y - limitSize.y / 2, limitSize.y);
            GUI.color = Color.white;
        }

        //Dragging
        public void DrawHoveredData(object draggedData, Vector2 pos)
        {
            if (!CanWork) return;

            GUI.color = TColor.White05;
            if (draggedData is WrappedTexture tex)
            {
                var texture = tex.Texture;
                Rect drawRect = pos.RectOnPos(TileVector * CanvasZoomScale);
                Widgets.DrawTextureFitted(drawRect, texture, 1);
                TWidgets.DoTinyLabel(drawRect, $"{pos}");
                TWidgets.DrawBox(drawRect, Color.black, 1);
            }

            if (draggedData is SpriteTile tile)
            {
                Rect drawRect = pos.RectOnPos(((tile.normalRect.size) * TileVector) * CanvasZoomScale);
                tile.DrawTile(drawRect);
                TWidgets.DoTinyLabel(drawRect, $"{pos}");
                TWidgets.DrawBox(drawRect, Color.black, 1);
            }
            GUI.color = Color.white;
        }

        public bool TryAccept(object draggedObject, Vector2 pos)
        {
            if (!CanWork) return false;

            TextureElement element = null;
            if (draggedObject is WrappedTexture texture)
            {
                element = new TextureElement(new Rect(Vector2.zero, Size), texture);
                AddElement(element);
                element.SetTRSP_FromScreenSpace();
            }

            if (draggedObject is SpriteTile tile)
            {
                element = new TextureElement(new Rect(Vector2.zero, Size), tile.spriteMat, tile.normalRect);
                AddElement(element);
                element.SetTRSP_FromScreenSpace(pivot:tile.pivot);
            }

            return element != null;
        }

        public bool Accepts(object draggedObject)
        {
            if (!CanWork) return false;

            if (draggedObject is WrappedTexture or SpriteTile) return true;
            return false;
        }

        //
        protected override IEnumerable<FloatMenuOption> RightClickOptions()
        {
            yield return new FloatMenuOption("Recenter...", delegate
            {
                DragPos = Vector2.zero;
            });

            foreach (var option in RoationOptions())
            {
                yield return option;
            }
        }

        private IEnumerable<FloatMenuOption> RoationOptions()
        {
            for (int i = 0; i < 4; i++)
            {
                Rot4 rot = new Rot4(i);
                yield return new FloatMenuOption($"Rotate {rot.ToStringHuman()}", delegate
                {
                    animationMetaData.SetRotation(rot);
                    Notify_SideChanged();
                });
            }
        }
    }
}
