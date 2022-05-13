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
    public enum UIElementMode
    {
        Dynamic,
        Static
    }

    public enum UIElementState
    {
        Open,
        Collapsed, 
        Closed
    }

    public abstract class UIElement : IDraggable, IFocusable
    {
        //Global Const
        private const int _BorderMargin = 25;

        //
        protected UIElement parent;
        protected List<UIElement> children = new List<UIElement>();

        //Local Data
        protected Color bgColor = TColor.MenuSectionBGFillColor;
        protected Color borderColor = TColor.MenuSectionBGBorderColor;

        protected string label, title;
        protected bool hasTopBar = true;

        private Rect? overrideRect;
        private Vector2 position = Vector2.zero;
        private Vector2 size;

        //Internal Dragging
        private Vector2? startDragPos, endDragPos, oldPos;

        public Vector2 StartDragPos => startDragPos ?? Vector2.zero;
        public Vector2 EndDragPos => endDragPos ?? Vector2.zero;
        public Vector2 CurrentDragDiff => endDragPos.HasValue ? endDragPos.Value - startDragPos.Value : Vector2.zero;
        protected Vector2 CurrentDragResult => oldPos.HasValue ? new Vector2(oldPos.Value.x + CurrentDragDiff.x, oldPos.Value.y + CurrentDragDiff.y) : position;

        //Properties
        public UIElementMode UIMode { get; private set; } = UIElementMode.Dynamic;
        public UIElementState UIState { get; protected set; } = UIElementState.Open;

        public virtual int Priority => 999;
        public int RenderLayer { get; set; } = 0;
        public bool CanAcceptAnything => IsActive && UIState == UIElementState.Open;

        protected bool IsFocused => UIEventHandler.IsFocused(this);
        protected bool IsLocked { get; private set; }
        protected bool ClickedIntoTop { get; private set; }

        public virtual bool CanBeFocused => true;
        public bool IsActive { get; set; } = true;

        public object DragAndDropData { get; protected set; }

        //Relations
        public UIElement Parent
        {
            get => parent;
            set => parent = value;
        }

        public virtual List<UIElement> Children => children;

        public virtual UIContainerMode ContainerMode => UIContainerMode.InOrder;

        //Main 
        public Vector2 Position
        {
            get => position;
            set
            {
                position = value;
                overrideRect = new Rect(position.x, position.y, size.x, size.y);
                Notify_StateChanged();
            }
        }
        public Vector2 Size
        {
            get => size;
            set
            {
                size = value;
                overrideRect = new Rect(position.x, position.y, size.x, size.y);
                Notify_StateChanged();
            }
        }

        public Rect Rect
        {
            get => (overrideRect ?? new Rect(position, size)).Rounded();
            private set
            {
                overrideRect = value;
                position = new Vector2(value.x, value.y);
                size = new Vector2(value.width, value.height);
                Notify_StateChanged();
            }
        }

        public Rect InRect => new Rect(Rect.x, hasTopBar ? TopRect.yMax : Rect.y, Rect.width, Rect.height - (hasTopBar ? TopRect.height : 0));

        public Rect? DragContext => parent?.Rect ?? null;

        public string Title
        {
            get => title;
            protected set => title = value;
        }

        public virtual string Label => "New Element";

        //Input Rect Data
        protected Rect TopRect => new Rect(position.x, position.y, size.x, _BorderMargin);
        protected virtual Rect DragAreaRect => TopRect;
        public virtual Rect FocusRect => Rect;

        protected bool CanMove => UIMode == UIElementMode.Dynamic && ClickedIntoTop && !IsLocked && IsInDragArea();

        //Constructors
        protected UIElement(UIElementMode mode)
        {
            this.UIMode = mode;
        }

        protected UIElement(Rect rect, UIElementMode mode)
        {
            this.Rect = rect;
            this.UIMode = mode;
        }

        protected UIElement(Vector2 pos, Vector2 size, UIElementMode mode)
        {
            this.size = size;
            Position = pos;
            this.UIMode = mode;
        }

        private void SetParent(UIElement parent) => this.parent = parent;
        private void SetPosition(Vector2 pos) => this.Position = pos;
        private void SetSize(Vector2 size) => this.Size = size;

        public void SetProperties(UIElement parent = null, Vector2? pos = null, Vector2? size = null)
        {
            if (parent != null)
                SetParent(parent);
            if (pos.HasValue)
                SetPosition(pos.Value);
            if (size.HasValue)
                SetSize(size.Value);
        }

        //Visibility
        public void ToggleOpen()
        {
            //if (parent is not ToolBar) return;
            UIState = UIState == UIElementState.Open ? UIElementState.Closed : UIElementState.Open;
        }

        public void SetVisibility(UIElementState state)
        {
            UIState = state;
        }

        //Relation Functions
        public T GetChildElement<T>() where T : UIElement
        {
            return (T)Children.First(t => t is T);
        }

        //Relation Changes
        public void AddElement(UIElement newElement, Vector2? pos = null)
        {
            switch (ContainerMode)
            {
                case UIContainerMode.InOrder:
                    Children.Add(newElement);
                    break;
                case UIContainerMode.Reverse:
                    Children.Insert(0, newElement);
                    break;
            }
            newElement.SetParent(this);
            if(pos.HasValue)
                newElement.SetPosition(pos.Value);

            Notify_AddedElement(newElement);
            newElement.Notify_AddedToParent(this);
        }

        public void DiscardElement(UIElement element)
        {
            Children.Remove(element);
            element.SetParent(null);
            Notify_RemovedElement(element);

            element.Notify_RemovedFromParent(this);
        }

        protected virtual void Notify_AddedElement(UIElement newElement) { }
        protected virtual void Notify_RemovedElement(UIElement newElement) { }

        protected virtual void Notify_AddedToParent(UIElement parent) { }
        protected virtual void Notify_RemovedFromParent(UIElement parent){}

        //Relation State Change
        private void Notify_StateChanged()
        {
            parent?.Notify_ChildElementChanged(this);
        }

        protected virtual void Notify_ChildElementChanged(UIElement element)
        {
        }

        public virtual void Notify_ElementSelected(UIElement element, int index)
        {
        }

        //
        protected virtual bool IsInDragArea()
        {
            return Mouse.IsOver(DragAreaRect);
        }

        //Drawing
        public void DrawElement(Rect? overrideRect = null)
        {
            if (UIState == UIElementState.Closed) return;

            UIEventHandler.RegisterLayer(this);
            if (overrideRect.HasValue)
                Rect = overrideRect.Value;

            //
            HandleEvent();

            //
            TWidgets.DrawColoredBox(Rect, bgColor, borderColor, 1);
            if(hasTopBar) DrawTopBar();

            //
            if (UIState == UIElementState.Collapsed) return;

            //
            var drawnInRect = InRect.ContractedBy(1);

            //Custom Drawing
            DragAndDropData = null;
            DrawContentsBeforeRelations(drawnInRect);
            DrawChildren(drawnInRect);
            DrawContentsAfterRelations(drawnInRect);
        }

        //Draw Relations
        protected void DrawChildren(Rect inRect)
        {
            Widgets.BeginGroup(inRect);
            switch (ContainerMode)
            {
                case UIContainerMode.InOrder:
                {
                    for (var i = 0; i < Children.Count; i++)
                    {
                        var element = Children[i];
                        element.DrawElement();
                    }

                    break;
                }
                case UIContainerMode.Reverse:
                {
                    for (int i = Children.Count - 1; i >= 0; i--)
                    {
                        var element = Children[i];
                        element.DrawElement();
                    }
                    break;
                }
            }
            Widgets.EndGroup();
        }

        private void DrawTopBar()
        {
            TWidgets.DrawBoxHighlightIfMouseOver(DragAreaRect);

            Widgets.BeginGroup(TopRect);
            {
                WidgetRow topBarRow = new WidgetRow();
                DrawSettings(topBarRow);
                if (title != null)
                {
                    topBarRow.Label(Title);
                }

                DoTopBarExtra(topBarRow);
                topBarRow.Init(TopRect.width - (WidgetRow.IconSize * 2 + 1), TopRect.height - (WidgetRow.IconSize + 1), UIDirection.RightThenDown);
            }
            Widgets.EndGroup();
        }

        private void DrawSettings(WidgetRow row)
        {
            if (UIMode == UIElementMode.Dynamic)
            {
                if (row.ButtonIcon(IsLocked ? TeleContent.LockClosed : TeleContent.LockOpen))
                {
                    IsLocked = !IsLocked;
                }
            }
            if (parent is ToolBar)
            {
                if (row.ButtonIcon(TeleContent.DeleteX))
                {
                    ToggleOpen();
                }
            }
        }

        protected virtual void DoTopBarExtra(WidgetRow row) { }

        //Event Handling
        private void HandleEvent()
        {
            Event curEvent = Event.current;
            Vector2 mousePos = curEvent.mousePosition;

            bool isInContext = Rect.Contains(mousePos);
            //if (!isInContext) return;

            if (curEvent.type == EventType.MouseDown && isInContext)
            {
                startDragPos ??= mousePos;
                oldPos ??= position;
                UIEventHandler.StartFocus(this);

                if (Mouse.IsOver(TopRect))
                {
                    ClickedIntoTop = true;
                }

                //FloatMenu
                if (curEvent.button == 1 && Mouse.IsOver(FocusRect))
                {
                    var options = RightClickOptions()?.ToList();
                    if (options != null && options.Any())
                    {
                        FloatMenu menu = new FloatMenu(options);
                        menu.vanishIfMouseDistant = true;
                        Find.WindowStack.Add(menu);
                    }
                }
            }

            if (curEvent.type == EventType.MouseDrag && startDragPos != null)
            {
                endDragPos = mousePos;
            }

            //Custom Handling
            HandleEvent_Custom(curEvent, isInContext);

            //Handle Pos Manipulation
            if (IsFocused)
            {
                if (UIDragger.IsBeingDragged(this) || (curEvent.type == EventType.MouseDrag && CanMove))
                    UIDragger.Notify_ActiveDrag(this, curEvent);

                if (UIDragNDropper.IsSource(this) || DragAndDropData != null && curEvent.type == EventType.MouseDrag)
                    UIDragNDropper.Notify_DraggingData(this, DragAndDropData, curEvent);
            }

            //Reset
            if (curEvent.type == EventType.MouseUp)
            {
                startDragPos = oldPos = endDragPos = null;
                ClickedIntoTop = false;
                UIEventHandler.StopFocus(this);
            }
        }

        //Extensions
        protected virtual IEnumerable<FloatMenuOption> RightClickOptions()
        {
            return null;
        }

        //Extensions
        protected virtual void HandleEvent_Custom(Event ev, bool inContext = false)
        {
        }

        protected virtual void DrawContentsBeforeRelations(Rect inRect)
        {
        }

        protected virtual void DrawContentsAfterRelations(Rect inRect)
        {
        }

        protected virtual void DrawTopBarExtras(Rect topRect)
        {

        }
    }
}
