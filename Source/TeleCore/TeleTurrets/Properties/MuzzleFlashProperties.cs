using Verse;

namespace TeleCore;

public class MuzzleFlashProperties
{
    public float fadeInTime = 0f;
    public float fadeOutTime = 0f;

    public GraphicData flashGraphicData;
    private Graphic graphicInt;

    public float scale = 1;
    public float solidTime = 0.25f;

    public Graphic Graphic => graphicInt ??= flashGraphicData.Graphic;
}