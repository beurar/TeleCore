using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TeleCore;

public class WidgetStackPanel
{
	private const float DistFromMouse = 26f;
	private const float LabelColumnWidth = 130f;
	private const float InfoColumnWidth = 170f;
	private const float WindowPadding = 12f;
	private const float Padding = 12f;
	private const float LineHeight = 24f;
	private const float ThingIconSize = 22f;

	private bool active;
	private int numLines;
	private Vector2 startPos;
	private float width;
	private float widthHalf;
	
	//Dynamic
	private Vector2 curXY;
	
	public Rect Rect => new Rect(curXY.x, curXY.y, width, LineHeight * numLines);
	
	public void Begin(Rect rect)
	{
		numLines = 0;
		active = true;
		startPos = curXY = rect.position;
		width = rect.width;
		widthHalf = width / 2;
	}

	public void End()
	{
		numLines = 0;
		active = false;
	}

	private void Increment(float y_Val = LineHeight)
	{
		numLines++;
		curXY += new Vector2(0, y_Val);
	}
	
	//
	public void DrawHeader(string text)
	{
		//var curY = curXY.y + (numLines * LineHeight) + 12f - 8f;
		var curY = curXY.y + Padding;
		Text.Anchor = TextAnchor.UpperCenter;
		Text.Font = GameFont.Small;
		var rectHeight = Text.CalcHeight(text, width);
		Rect rect = new Rect(curXY.x, curY, width, rectHeight);
		Widgets.Label(rect, text);
		Text.Font = default;
		Text.Anchor = default;
		
		//
		Increment(rectHeight);
	}

	public void DrawWidgetRow(WidgetRow row)
	{
		float num = numLines * 24f;
		var curY = curXY.y + num + 12f;
		Rect rect = new Rect(curXY.x,  curY, width, 24f);
		row.Init(curXY.x, curY, UIDirection.RightThenDown, width);
	}

	public void DrawRow(string label, string info)
	{
		float num = numLines * LineHeight;
		var curY = curXY.y + num + Padding;
		Rect rect = new Rect(curXY.x, curY, width, LineHeight);

		if (numLines % 2 == 1)
			Widgets.DrawLightHighlight(rect);

		//Label Part
		GUI.color = Color.gray;
		rect = new Rect(curXY.x + LineHeight, num + Padding, LabelColumnWidth, LineHeight);
		Widgets.Label(rect, label);
		
		
		//Info Part
		GUI.color = Color.white;
		rect = new Rect(rect.xMax + Padding, rect.y, width - (rect.xMax + Padding), LineHeight);
		Widgets.Label(rect, info);
		TooltipHandler.TipRegion(rect, info);

		//
		Increment();
	}

	public void DrawThingRow(Thing thing)
	{
		float num = numLines * 24f;
		List<object> selectedObjects = Find.Selector.SelectedObjects;
		Rect rect = new Rect(12f, num + 12f, width, 24f);
		if (selectedObjects.Contains(thing))
		{
			Widgets.DrawHighlight(rect);
		}
		else if (numLines % 2 == 1)
		{
			Widgets.DrawLightHighlight(rect);
		}

		rect = new Rect(24f, num + 12f + 1f, 22f, 22f);
		if (thing is Blueprint || thing is Frame)
		{
			Widgets.DefIcon(rect, thing.def, null, 1f, null, false, null, null, null);
		}
		else if (thing is Pawn || thing is Corpse)
		{
			Widgets.ThingIcon(rect.ExpandedBy(5f), thing, 1f, null, false);
		}
		else
		{
			Widgets.ThingIcon(rect, thing, 1f, null, false);
		}

		rect = new Rect(58f, num + 12f, 370f, 24f);
		Widgets.Label(rect, thing.LabelMouseover);
		
		//
		Increment();
	}

	public void DrawDivider()
	{
		GUI.color = Color.gray;
		Widgets.DrawLineHorizontal(curXY.x, curXY.y + Padding, width);
		//TWidgets.GapLine(curXY.x, curXY.y, width, 24f);
		GUI.color = Color.white;
		
		//
		Increment();
	}
}