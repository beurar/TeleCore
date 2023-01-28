using UnityEngine;

namespace TeleCore;

public struct UIElementStyle
{
    public UIElementStyle()
    {
    }

    public Color BgColor { get; set; } = TColor.MenuSectionBGFillColor;
    public Color BorderColor { get; set; } = TColor.MenuSectionBGBorderColor;
    public bool HasTopBar { get; set; } = false;
}