using System;

namespace TeleCore.Rendering.UI.DynaUI.Events;

public class ElementSelectedEventArgs : EventArgs
{
    private UIElement _element;
    private int _index;
    

    public UIElement Element => _element;

    public int Index => _index;
    
    public ElementSelectedEventArgs(UIElement element, int index)
    {
        _element = element;
        _index = index;
    }
}