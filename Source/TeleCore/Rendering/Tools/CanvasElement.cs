using UnityEngine;

namespace TeleCore;

public class CanvasElement : UIElement
{
    private BasicCanvas _parentCanvas;
    
    public CanvasElement(UIElementMode mode) : base(mode)
    {
    }

    public CanvasElement(Rect rect, UIElementMode mode) : base(rect, mode)
    {
    }

    public CanvasElement(Vector2 pos, Vector2 size, UIElementMode mode) : base(pos, size, mode)
    {
    }

    protected override void Notify_AddedToParent(UIElement parent)
    {
        base.Notify_AddedToParent(parent);
        if (parent is BasicCanvas canvas)
        {
            _parentCanvas = canvas;
        }
    }
}