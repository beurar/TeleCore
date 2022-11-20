using UnityEngine;
using Verse;

namespace TeleCore;

public class Designator_SubBuildMenu : Designator
{
    private SubBuildMenuDef subMenuDef;

    public override string Label => "Reset";
    public override string Desc => "Reset window position.";

    public Designator_SubBuildMenu(SubBuildMenuDef menuDef)
    {
        order = -1;
        subMenuDef = menuDef;
    }

    public void Toggle_Menu(bool opening)
    {
        SubBuildMenu.ToggleOpen(subMenuDef, opening);
    }

    public override void ProcessInput(Event ev)
    {
        SubBuildMenu.ResetMenuWindow(subMenuDef);
    }

    public override AcceptanceReport CanDesignateCell(IntVec3 loc)
    {
        return AcceptanceReport.WasRejected;
    }
}