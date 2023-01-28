using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TeleCore;

public class EffectCanvas : BasicCanvas
{
    //Settings
    private bool ShouldShowMetaData { get; set; } = true;
    private bool ShouldShowElementProperties { get; set; }

    public Rect MetaDataViewRect => new Rect(Rect.xMax, Position.y, 500, 250);

    public EffectCanvas(Vector2 pos, Vector2 size, UIElementMode mode) : base(pos, size, mode)
    {
        UIDragNDropper.RegisterAcceptor(this);
    }

    public EffectCanvas(UIElementMode mode) : base(mode)
    {
        UIDragNDropper.RegisterAcceptor(this);
    }

    protected override IEnumerable<FloatMenuOption> RightClickOptions()
    {
        foreach (var rightClickOption in base.RightClickOptions())
        {
            yield return rightClickOption;
        }

        //
        yield return new FloatMenuOption("Add Node", () => { });
    }


    //
    private void DrawEffectMetaData()
    {
        Rect rect = MetaDataViewRect;
        UILayerRenderer.DrawImmediateLayer(0, rect, delegate { });
    }

    //

    protected override void DrawTopBarExtras(Rect topRect)
    {
        return;
        WidgetRow buttonRow = new WidgetRow();
        buttonRow.Init(topRect.xMax, topRect.y, UIDirection.LeftThenDown);
        if (buttonRow.ButtonIcon(TeleContent.SettingsWheel))
        {
            ShouldShowMetaData = !ShouldShowMetaData;
        }

        if (buttonRow.ButtonIcon(TeleContent.BurgerMenu))
        {
            ShouldShowElementProperties = !ShouldShowElementProperties;
        }
    }

    protected override void DrawOnCanvas(Rect inRect)
    {
        if (ShouldShowMetaData)
        {
            DrawEffectMetaData();
        }
    }

    //
    public override bool TryAcceptDrop(object draggedObject, Vector2 pos)
    {
        if (draggedObject is Def def)
        {
            var element = new EffectElement(new Rect(pos, new Vector2(20,20)), def);
            AddElement(element);
            return true;
        }

        return false;
    }

    public override bool CanAcceptDrop(object draggedObject)
    {
        return draggedObject is Def;
    }

    public override void DrawHoveredData(object draggedObject, Vector2 pos)
    {
        if (draggedObject is Def def)
        {
            var texture = TWidgets.TextureForFleckMote(def);
            var labelSize = Text.CalcSize(def.defName);
            Rect drawRect = pos.RectOnPos(new Vector2(20, 20) * CanvasZoomScale);
            Rect labelRect = new Rect(drawRect.x, drawRect.y - 20, labelSize.x, labelSize.y);
            TWidgets.DoTinyLabel(labelRect, $"[{def.defName}]");
            Widgets.DrawTextureFitted(drawRect, TeleContent.UIDataNode, 1f);
        }
    }
}

