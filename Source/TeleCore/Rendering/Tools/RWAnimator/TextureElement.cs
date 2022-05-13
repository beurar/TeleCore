using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public enum ManipulationMode
    {
        None,
        Move,
        Resize,
        Rotate,
        PivotDrag
    }

    public enum TexCoordAnchor
    {
        Center,
        Top,
        Bottom,
        Left,
        Right,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
    }

    public class TextureElement : UIElement, IKeyFramedElement, IReorderableElement
    {
        //
        private string[] tempBuffer = new string[TextureCanvas.BufferSize];

        //
        protected TextureData texture;
        protected TextureElement parentElement;
        protected List<TextureElement> subParts = new ();

        //Local texture manipulation
        private Vector2? oldPivot;
        private KeyFrameData? oldKF;
        private ManipulationMode? lockedInMode = null;

        //UIElement
        public UIElement Element => this;
        public UIElement Owner => this;
        public TextureCanvas ParentCanvas => (TextureCanvas)parent;
        public override bool CanBeFocused => base.CanBeFocused && IsSelected;
        protected override Rect DragAreaRect => Rect;
        public ManipulationMode ManiMode { get; private set; } = ManipulationMode.None;

        //Texture Data
        //public KeyFrameData EditData => internalFrameData;
        public KeyFrameData CurrentData => ParentCanvas.TimeLine.GetDataFor(this);
        public KeyFrame CurrentFrame => ParentCanvas.TimeLine.GetKeyFrame(this);

        private Vector2 RenderPivot => TruePos + (PivotPoint * ParentCanvas.CanvasZoomScale);

        public List<TextureElement> SubParts => subParts;

        public ManipulationMode LockedInMode => lockedInMode ?? ManiMode;
        private bool ManiModeFlag => lockedInMode != null && lockedInMode != ManipulationMode.None;

        public Color BorderColor
        {
            get
            {
                switch (lockedInMode ?? ManiMode)
                {
                    case ManipulationMode.Move:
                        return Color.blue;
                    case ManipulationMode.Resize:
                        return Color.green;
                    case ManipulationMode.Rotate:
                        return Color.magenta;
                    default: return IsSelected ? Color.cyan : TColor.White005;
                }
            }
        }

        // IMPORTANT: Add UpdateKeyframeFor to KeyFrameData changes!
        public Vector2 PivotPoint
        {
            get => CurrentData.PivotPoint;
            set
            {
                var tempFrame = CurrentData;
                tempFrame.PivotPoint = value;

                //Apply edited Data onto actual frame
                ParentCanvas.TimeLine.UpdateKeyframeFor(this, CurrentData);
            }
        }

        public Rect TexCoords
        {
            get => CurrentData.TexCoords;
            set
            {
                var tempFrame = CurrentData;
                tempFrame.TexCoords = value;

                //
                ParentCanvas.TimeLine.UpdateKeyframeFor(this, tempFrame);
            }
        }

        public TexCoordAnchor TexCoordAnchor
        {
            get => texture.TexCoordAnchor;
            set => texture.TexCoordAnchor = value;
        }

        public bool AttachScript
        {
            get => texture.AttachScript; 
            set => texture.AttachScript = value;
        }

        public string LayerTag
        {
            get => texture.LayerTag;
            set => texture.LayerTag = value;
        }

        public string StringBuffer
        {
            get => texture.StringBuffer;
            set => texture.StringBuffer = value;
        }
        public int LayerIndex
        {
            get => texture.LayerIndex;
            set => texture.LayerIndex = value;
        }

        public Vector2 RelatedPosition => TPosition + (parentElement?.TPosition ?? Vector2.zero);

        public float RelatedRotation
        {
            get
            {
                return parentElement?.TRotation ?? 0;
            }
        }

        public Vector2 TPosition
        {
            get => CurrentData.TPosition;
            set
            {
                var tempFrame = CurrentData;
                var oldPos = tempFrame.TPosition;
                tempFrame.TPosition = value;

                //
                foreach (var t in SubParts)
                {
                    t.SetTRSP_Direct(t.TPosition + (value - oldPos));
                }

                //
                ParentCanvas.TimeLine.UpdateKeyframeFor(this, tempFrame);
            }
        }

        public float TRotation
        {
            get => CurrentData.TRotation;
            set
            {
                var tempFrame = CurrentData;
                var oldRot = tempFrame.TRotation;
                tempFrame.TRotation = value;

                var rotDiff = value - oldRot;
                var pivot = tempFrame.TPosition;
                foreach (var t in SubParts)
                {
                    var point = t.TPosition;
                    var result = ((Quaternion.Euler(0,0, rotDiff) * (point - pivot)) + (Vector3)pivot);
                    t.SetTRSP_Direct(result, rot: t.TRotation + rotDiff);
                }

                //
                ParentCanvas.TimeLine.UpdateKeyframeFor(this, tempFrame);
                
            }
        }

        public Vector2 TSize
        {
            get => CurrentData.TSize;
            set
            {
                var tempFrame = CurrentData;
                var oldSize = tempFrame.TSize;
                var newVal = value.Clamp(new Vector2(TextureCanvas.SizeRange.min, TextureCanvas.SizeRange.min), new Vector2(TextureCanvas.SizeRange.max, TextureCanvas.SizeRange.max));
                tempFrame.TSize = newVal;
                var sizeDiff = (newVal - oldSize);
                foreach (var t in SubParts)
                {
                    var tPosOff = t.TPosition - TPosition;
                    var offSet = new Vector2((tPosOff.x / oldSize.x), (tPosOff.y / oldSize.y)) * sizeDiff; 
                    var newPos = t.TPosition + (offSet);
                    t.SetTRSP_Direct(newPos, size:t.TSize + sizeDiff);
                }

                //
                ParentCanvas.TimeLine.UpdateKeyframeFor(this, tempFrame);
            }
        }

        public string[] ValueBuffer
        {
            get
            {
                if (IsAtKeyFrame)
                {
                    return ParentCanvas.BufferFor(CurrentFrame);
                }
                CurrentData.UpdateBuffer(tempBuffer);
                return tempBuffer;
            }
        }

        public void UpdateBuffer()
        {
            if (IsAtKeyFrame)
            {
                CurrentData.UpdateBuffer(ValueBuffer);
            }
        }

        //
        public Vector2 TSizeFactor => TextureCanvas.TileVector * texture.TexCoordReference.size;
        public Vector2 DrawSize => CurrentData.TSize * TSizeFactor;

        private bool IsSelected => ParentCanvas.ActiveTexture == this;
        private bool IsAtKeyFrame => ParentCanvas.TimeLine.IsAtKeyFrame(this, out _);

        //
        public bool ShowTexCoordGhost { get; set; }
        public bool Visibility { get; set; } = true;

        //
        private Vector2 ZoomedSize => DrawSize * ParentCanvas.CanvasZoomScale;
        private Vector2 ZoomedPos => TPosition * ParentCanvas.CanvasZoomScale;

        private Vector2 TruePos => ParentCanvas.Origin + (ZoomedPos);
        public Vector2 RectPosition => TruePos - ZoomedSize / 2f;

        public Rect TextureRect => new Rect(RectPosition, ZoomedSize);
        public override Rect FocusRect => TextureRect.ExpandedBy(15);

        public Material Material => texture.Material;
        public Texture Texture => Material.mainTexture;
        public TextureData Data => texture;

        public TextureElement(Rect rect, TextureData textureData) : base(rect, UIElementMode.Dynamic)
        {
            bgColor = Color.clear;
            this.texture = textureData;
            this.texture.LayerTag = Material.mainTexture.name;

            hasTopBar = false;
        }

        public TextureElement(Rect rect, WrappedTexture texture) : base(rect, UIElementMode.Dynamic)
        {
            bgColor = Color.clear;
            this.texture = new TextureData(texture);
            this.texture.LayerTag = Material.mainTexture.name;

            hasTopBar = false;
        }

        public TextureElement(Rect rect, Material mat, Rect texCoords) : base(rect, UIElementMode.Dynamic)
        {
            bgColor = Color.clear;
            texture = new TextureData(mat);
            texture.SetTexCoordReference(texCoords);
            texture.LayerTag = Material.mainTexture.name;
            hasTopBar = false;
        }

        public void SetTRS(KeyFrameData? data)
        {
            if (data.HasValue)
            {
                var frame = data.Value;
                SetTRSP_Direct(frame.TPosition, frame.TRotation, frame.TSize);
            }
        }

        public void SetTRSP_Direct(Vector2? pos = null, float? rot = null, Vector2? size = null, Vector2? pivot = null)
        {
            TPosition = pos ?? CurrentData.TPosition;
            TRotation = rot ?? CurrentData.TRotation;
            TSize = size ?? CurrentData.TSize;
            PivotPoint = pivot ?? PivotPoint;
            ParentCanvas.TimeLine.UpdateKeyframeFor(this, CurrentData);
            TexCoords = texture.TexCoordReference;
            UpdateBuffer();
        }

        public void SetTRSP_FromScreenSpace(Vector2? pos = null, float? rot = null, Vector2? size = null, Vector2? pivot = null)
        {
            SetTRSP_Direct(ParentCanvas.MousePos);
        }

        public void Reset()
        {
            TSize = Vector2.one;
            TRotation = 0;
            PivotPoint = Vector2.zero;
            TexCoords = texture.TexCoordReference;
            ParentCanvas.TimeLine.UpdateKeyframeFor(this, CurrentData);
            UpdateBuffer();
        }

        public void Recenter()
        {
            TPosition = Vector2.zero;
            ParentCanvas.TimeLine.UpdateKeyframeFor(this, CurrentData);
            UpdateBuffer();
        }

        public void LinkToParent(TextureElement newParent)
        {
            newParent.SubParts.Add(this);
            this.parentElement = newParent;
        }

        public void UnlinkFromParent()
        {
            if (parentElement != null)
            {
                parentElement.SubParts.Remove(this);
                parentElement = null;
            }
        }

        protected override void Notify_RemovedFromParent(UIElement parent)
        {
            UnlinkFromParent();
            if (!subParts.NullOrEmpty())
            {
                for (int i = subParts.Count - 1; i >= 0; i--)
                {
                    TextureElement part = subParts[i];
                    part.Notify_RemovedFromParent(parent);
                }
            }
        }

        protected override void HandleEvent_Custom(Event ev, bool inContext)
        {
            if (!(IsSelected)) return;

            var mv = ev.mousePosition;
            var dist = mv.DistanceToRect(TextureRect);
            var pivotDist = Vector2.Distance(mv, RenderPivot);

            ManiMode = ManipulationMode.None;
            if (pivotDist <= 8)
            {
                ManiMode = ManipulationMode.PivotDrag;
            }
            else if (TextureRect.Contains(mv))
            {
                ManiMode = ManipulationMode.Move;
            }

            if (dist is > 0 and < 5)
            {
                ManiMode = ManipulationMode.Resize;
            }

            if (dist is > 5 and < 15)
            {
                ManiMode = ManipulationMode.Rotate;
            }

            if (ev.type == EventType.MouseDown)
            {
                if (ev.button == 0)
                {
                    lockedInMode ??= ManiMode;
                    oldKF ??= CurrentData;
                    oldPivot ??= PivotPoint;

                    if (ManiModeFlag) //Update Local
                    {
                        //LocalData = oldKF.Value;
                        UIEventHandler.StartFocusForced(this);
                    }
                }
            }

            if (IsFocused && ev.type == EventType.MouseDrag)
            {
                var dragDiff = CurrentDragDiff;
                switch (lockedInMode)
                {
                    case ManipulationMode.PivotDrag:
                        var oldPivPos = oldPivot.Value;
                        dragDiff /= ParentCanvas.CanvasZoomScale;
                        PivotPoint = new Vector2(oldPivPos.x + dragDiff.x, oldPivPos.y + dragDiff.y);
                        break;
                    case ManipulationMode.Move:
                        //if (!TextureRect.Contains(mv) || !IsFocused) return;
                        var oldPos = oldKF.Value.TPosition;
                        dragDiff /= ParentCanvas.CanvasZoomScale;
                        TPosition = oldPos + dragDiff;
                        break;
                    case ManipulationMode.Resize:
                        dragDiff *= 2;
                        dragDiff /= TSizeFactor;
                        dragDiff /= ParentCanvas.CanvasZoomScale;

                        var norm = (StartDragPos - TextureRect.center).normalized;
                        if (norm.x < 0)
                            dragDiff = new Vector2(-dragDiff.x, dragDiff.y);
                        if (norm.y < 0)
                            dragDiff = new Vector2(dragDiff.x, -dragDiff.y);

                        TSize = oldKF.Value.TSize + dragDiff;
                        break;
                    case ManipulationMode.Rotate:
                        var vec1 = StartDragPos - RenderPivot;
                        var vec2 = ev.mousePosition - RenderPivot;
                        var newRot = oldKF.Value.TRotation + Vector2.SignedAngle(vec1, vec2);
                        TRotation = newRot;
                        break;
                }

                if (ManiModeFlag) //Update KeyFrame
                {
                    UpdateBuffer();
                }
            }

            if (ev.type == EventType.MouseUp)
            {
                oldKF = null;
                oldPivot = null;
                lockedInMode = null;
            }
        }

        protected override IEnumerable<FloatMenuOption> RightClickOptions()
        {
            if (!IsSelected) yield break;
            yield return new FloatMenuOption("Reset", this.Reset);
            yield return new FloatMenuOption("Delete", delegate { ParentCanvas.DiscardElement(this); });
            yield return new FloatMenuOption("Center", Recenter);
        }

        protected override void DrawContentsBeforeRelations(Rect inRect)
        {
            if (!Visibility) return;

            Material.SetTextureOffset("_MainTex", TexCoords.position);
            Material.SetTextureScale("_MainTex", TexCoords.size);

            TWidgets.DrawRotatedMaterial(TWidgets.RectFitted(TextureRect, TexCoords.size, TexCoordAnchor), RenderPivot, TRotation, Material, TexCoords);

            if (TRotation != 0 && IsSelected)
            {
                var matrix = GUI.matrix;
                UI.RotateAroundPivot(TRotation, TextureRect.center);
                TWidgets.DrawBoxHighlight(TextureRect);
                GUI.matrix = matrix;
            }

            TWidgets.DrawBox(TextureRect, BorderColor, 1);

            if (ShowTexCoordGhost)
            {
                //Widgets.BeginGroup(TextureRect);
                GUI.color = TColor.White05;
                TWidgets.DrawRotatedMaterial(TextureRect, Vector2.zero, 0, Material, new Rect(0, 0, 1, 1));
                GUI.color = Color.white;

                Widgets.BeginGroup(TextureRect);
                TWidgets.DrawBox(TWidgets.TexCoordsToRect(TextureRect, TexCoords), TColor.NiceBlue, 1);
                Widgets.EndGroup();
            }

            //Draw Pivot
            GUI.color = ManiMode == ManipulationMode.PivotDrag ? Color.red : Color.white;
            Widgets.DrawTextureFitted(RenderPivot.RectOnPos(new Vector2(24, 24)), TeleContent.PivotPoint, 1, Vector2.one, new Rect(0, 0, 1, 1), TRotation);
            GUI.color = Color.white;
        }

        public void DrawElementInScroller(Rect inRect)
        {
            var mat = Material;
            TWidgets.DrawRotatedMaterial(TWidgets.RectFitted(inRect, TexCoords.size, TexCoordAnchor), Vector2.zero, 0, mat, TexCoords);

            Text.Anchor = TextAnchor.LowerLeft;
            TWidgets.DoTinyLabel(inRect, LayerTag);
            Text.Anchor = default;

            if (parentElement != null)
                GUI.color = Color.blue;

            if(!subParts.NullOrEmpty())
                GUI.color = Color.red;

            Rect linkButton = new Rect(inRect.xMax-32, inRect.y, 32, 32);
            Rect visibleButton = new Rect(inRect.xMax - 32, inRect.y+32, 32, 32);
            if (Widgets.ButtonImage(linkButton, TeleContent.LinkIcon, GUI.color))
            {
                var floatOptions = new List<FloatMenuOption>();
                foreach (TextureElement tex in ParentCanvas.Children)
                {
                    if (tex != this)
                    {
                        floatOptions.Add(new FloatMenuOption(tex.LayerTag, delegate
                        {
                            LinkToParent(tex);
                        }));
                    }
                }
                Find.WindowStack.Add(new FloatMenu(floatOptions));
            }
            GUI.color = Color.white;

            var visibility = Visibility ? TeleContent.VisibilityOn : TeleContent.VisibilityOff;
            if (Widgets.ButtonImage(visibleButton, visibility, Color.white))
            {
                Visibility = !Visibility;
            }

            if(parentElement != null)
                TooltipHandler.TipRegion(linkButton, $"Linked To: {parentElement.LayerTag}");

            GUI.color = Color.white;
            //TWidgets.DoTinyLabel(inRect, $"{mat.mainTexture.name}\n{mat.shader.name}\n{RectSimple(texCoords ?? default)}\n{pivotPoint}\n{mat.mainTextureOffset}\n{mat.mainTextureScale}");
        }

        private string RectSimple(Rect rect)
        {
            return $"({rect.x},{rect.y});({rect.width},{rect.height})";
        }

        public TextureData GetData()
        {
            return texture;
        }
    }
}
