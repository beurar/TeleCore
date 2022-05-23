using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TeleCore
{
    public static class TWidgets
    {
        public static float GapLine(float x, float y, float width, float gapSize = 5, float sideContraction = 4, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            //Adds 
            GUI.color = TColor.GapLineColor;
            {
                var yPos = y;
                switch (anchor)
                {
                    case TextAnchor.MiddleCenter:
                        yPos = y + (gapSize / 2f);
                        break;
                    case TextAnchor.UpperCenter:
                        yPos = y;
                        break;
                    case TextAnchor.LowerCenter:
                        yPos = y + gapSize;
                        break;
                }

                Widgets.DrawLineHorizontal(x + sideContraction, yPos, width - (2 * sideContraction));
                y += gapSize;
            }
            GUI.color = Color.white;
            return y;
        }

        //
        public static void DrawBarMarkerAt(Rect barRect, float pct)
        {
            float num = barRect.height;
            Vector2 vector = new Vector2(barRect.x + barRect.width * pct, barRect.y);
            Rect rect = new Rect(vector.x - num / 2f, vector.y, num, num);
            var matrix = GUI.matrix;
            UI.RotateAroundPivot(180f, rect.center);
            GUI.DrawTexture(rect, Need.BarInstantMarkerTex);
            GUI.matrix = matrix;
        }

        public static float VerticalSlider(Rect rect, float value, float min, float max, float roundTo = -1f)
        {
            float num = GUI.VerticalSlider(rect, value, max, min);
            if (roundTo > 0f)
            {
                num = (float)Mathf.RoundToInt(num / roundTo) * roundTo;
            }
            if (value != num)
            {
                SoundDefOf.DragSlider.PlayOneShotOnCamera(null);
            }
            return num;
        }

        //TODO: Test if functional
        [Obsolete]
        public static void SliderCustom(Rect rect, int id, ref int value, int min = 0, int max = 100, Texture sliderTexture = null, Color sliderColor = default)
        {
            Rect rect2 = rect;
            rect2.xMin += 8f;
            rect2.xMax -= 8f;

            Rect position = new Rect(rect2.x, rect2.yMax - 8f - 1f, rect2.width, 2f);
            GUI.DrawTexture(position, BaseContent.WhiteTex);
            GUI.color = Color.white;
            float num = rect2.x + rect2.width * (float)(value - min) / (float)(max - min);
            Rect position2 = new Rect(num - 16f, position.center.y - 8f, 16f, 16f);
            GUI.DrawTexture(position2, sliderTexture ?? TeleContent.TimeSelMarker);

            if (Event.current.type == EventType.MouseUp || Event.current.rawType == EventType.MouseDown)
            {
                Widgets.draggingId = 0;
                SoundDefOf.DragSlider.PlayOneShotOnCamera(null);
            }

            bool flag = false;
            if (Mouse.IsOver(rect) || Widgets.draggingId == id)
            {
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && id != Widgets.draggingId)
                {
                    Widgets.draggingId = id;
                    /*
                    float x = Event.current.mousePosition.x;
                    if (x < position2.xMax)
                    {
                        Widgets.curDragEnd = Widgets.RangeEnd.Min;
                    }
                    else if (x > position3.xMin)
                    {
                        Widgets.curDragEnd = Widgets.RangeEnd.Max;
                    }
                    else
                    {
                        float num3 = Mathf.Abs(x - position2.xMax);
                        float num4 = Mathf.Abs(x - (position3.x - 16f));
                        Widgets.curDragEnd = ((num3 < num4) ? Widgets.RangeEnd.Min : Widgets.RangeEnd.Max);
                    }
                    */
                    flag = true;
                    Event.current.Use();
                    SoundDefOf.DragSlider.PlayOneShotOnCamera(null);
                }
                if (flag || Event.current.type == EventType.MouseDrag)
                {
                    int newSliderVal = Mathf.RoundToInt(Mathf.Clamp((Event.current.mousePosition.x - rect2.x) / rect2.width * (float)(max - min) + (float)min, (float)min, (float)max));
                    value = Mathf.Clamp(newSliderVal, min, max);
                    Widgets.CheckPlayDragSliderSound();
                    Event.current.Use();
                }
            }

        }


        //Labels
        public static void DoTinyLabel(Rect rect, string label)
        {
            var prevFont = Text.Font;
            var prevAnch = Text.Anchor;
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(rect, label);

            Text.Anchor = prevAnch;
            Text.Font = prevFont;
        }

        //Utility
        public static Rect RectFitted(Rect inRect, Vector2 texProportions, TexCoordAnchor texCoordAnchor, float scale = 1)
        {
            Rect rect = new Rect(0f, 0f, texProportions.x, texProportions.y);
            float num;
            if (rect.width / rect.height < inRect.width / inRect.height)
            {
                num = inRect.height / rect.height;
            }
            else
            {
                num = inRect.width / rect.width;
            }

            num *= scale;
            rect.width *= num;
            rect.height *= num;
            return GenTransform.OffSetByCoordAnchor(inRect, rect, texCoordAnchor);
        }

        public static Vector2 Size(this Texture texture)
        {
            return new Vector2(texture.width, texture.height);
        }

        /// <summary>
        /// Creates a rect on a given position with a size.
        /// </summary>
        /// <param name="pos">Center of the new Rect.</param>
        /// <param name="size">Size of the new Rect.</param>
        public static Rect RectOnPos(this Vector2 pos, Vector2 size)
        {
            return new Rect(pos.x - size.x / 2f, pos.y - size.y / 2f, size.x, size.y);
        }

        public static Rect RectToTexCoords(Rect parentRect, Rect partRect)
        {
            return new Rect(
                (partRect.x / parentRect.width),
                1f - ((partRect.y + partRect.height) / parentRect.height),
                partRect.width / parentRect.width,
                partRect.height / parentRect.height);
        }

        public static Rect TexCoordsToRect(Rect forParentRect, Rect texCoords)
        {
            return new Rect(
                texCoords.x * forParentRect.width,
                ((1 - texCoords.y) * forParentRect.height),
                texCoords.width * forParentRect.width,
                -texCoords.height * forParentRect.height);
        }

        //Events
        public static bool MouseClickIn(Rect rect, int mouseButton)
        {
            Event curEvent = Event.current;
            return Mouse.IsOver(rect) && curEvent.type == EventType.MouseDown && curEvent.button == mouseButton;
        }

        /// <summary>
        /// If clicked inside of this, the event is consumed and wont be used later in the current frame.
        /// </summary>
        public static void AbsorbInput(Rect rect)
        {
            var curEv = Event.current;
            if (Mouse.IsOver(rect) && curEv.type == EventType.MouseDown)
                curEv.Use();
        }

        //
        public static void DrawHighlightColor(Rect rect, Color color)
        {
            var oldColor = GUI.color;
            GUI.color = color;
            Widgets.DrawHighlight(rect);
            GUI.color = oldColor;
        }


        //MouseOver
        public static void DrawHighlightIfMouseOverColor(Rect rect, Color color)
        {
            var oldColor = GUI.color;
            GUI.color = color;
            Widgets.DrawHighlightIfMouseover(rect);
            GUI.color = oldColor;
        }

        public static void DrawBoxHighlightIfMouseOver(Rect rect)
        {
            if (Mouse.IsOver(rect))
                DrawBoxHighlight(rect);
        }

        //Boxes
        public static void DrawSelectionHighlight(Rect rect)
        {
            if (Mouse.IsOver(rect))
            {
                DrawColoredBox(rect, TColor.White01, TColor.White05, 1);
            }
        }

        public static void DrawBoxHighlight(Rect rect)
        {
            DrawBox(rect, TColor.White025, 1);
        }

        public static void DrawBox(Rect rect, float opacity, int thickness)
        {
            DrawBox(rect, new Color(1, 1, 1, opacity), thickness);
        }

        public static void DrawBox(Rect rect, Color color, int thickness)
        {
            Color oldColor = GUI.color;
            GUI.color = color;
            Widgets.DrawBox(rect, thickness);
            GUI.color = oldColor;
        }

        public static void DrawColoredBox(Rect rect, Color fillColor, Color borderColor, int thickness)
        {
            Color oldColor = GUI.color;
            Widgets.DrawBoxSolid(rect, fillColor);
            GUI.color = borderColor;
            Widgets.DrawBox(rect, thickness);
            GUI.color = oldColor;
        }

        //Texturea/Materials
        public static void DrawRotatedMaterial(Rect rect, Vector2 pivot, float angle, Material material, Rect texCoords = default(Rect))
        {
            Matrix4x4 matrix = Matrix4x4.identity;
            if (angle != 0f)
            {
                matrix = GUI.matrix;
                UI.RotateAroundPivot(angle, pivot);
            }

            GenUI.DrawTextureWithMaterial(rect, material.mainTexture, null, texCoords);

            if (angle != 0f)
            {
                GUI.matrix = matrix;
            }
        }

        public static void DrawTextureWithMaterial(Rect rect, Texture texture, Material material, Rect texCoords = default(Rect))
        {
            if (texCoords == default(Rect))
            {
                if (material == null)
                {
                    GUI.DrawTexture(rect, texture);
                    return;
                }
                if (Event.current.type == EventType.Repaint)
                {
                    Graphics.DrawTexture(rect, texture, new Rect(0f, 0f, 1f, 1f), 0, 0, 0, 0, new Color(GUI.color.r * 0.5f, GUI.color.g * 0.5f, GUI.color.b * 0.5f, GUI.color.a * 0.5f), material);
                }
            }
            else
            {
                if (material == null)
                {
                    GUI.DrawTextureWithTexCoords(rect, texture, texCoords);
                    return;
                }
                if (Event.current.type == EventType.Repaint)
                {
                    Graphics.DrawTexture(rect, texture, texCoords, 0, 0, 0, 0, new Color(GUI.color.r * 0.5f, GUI.color.g * 0.5f, GUI.color.b * 0.5f, GUI.color.a * 0.5f), material);
                }
            }
        }

        //WidgetRow Extensions
        public static void Slider(this WidgetRow row, float width, ref float value, float min = 0, float max = 1)
        {
            row.IncrementYIfWillExceedMaxWidth(width);
            Rect rect = new Rect(row.LeftX(width), row.curY, width, 24f);
            value = Widgets.HorizontalSlider(rect, value, min, max, true);
            row.IncrementPosition(width);
        }

        public static Rect TextFieldNumericFix<T>(this WidgetRow row, ref T val, ref string buffer, float width = -1f) where T : struct
        {
            if (width < 0f)
            {
                width = Text.CalcSize(val.ToString()).x;
            }
            row.IncrementYIfWillExceedMaxWidth(width + 2f);
            row.IncrementPosition(2f);
            Rect rect = new Rect(row.LeftX(width), row.curY, width, 24f);
            Widgets.TextFieldNumeric<T>(rect, ref val, ref buffer, float.MinValue, float.MaxValue);
            row.IncrementPosition(2f);
            row.IncrementPosition(rect.width);
            return rect;
        }

        public static bool ButtonIcon(this WidgetRow row, Texture2D tex, string tooltip = null, float iconSize = 24, Color? iconColor = null)
        {
            float num = iconSize;
            float num2 = (24f - num) / 2f;
            row.IncrementYIfWillExceedMaxWidth(num);
            Rect rect = new Rect(row.LeftX(num) + num2, row.curY + num2, num, num);
            MouseoverSounds.DoRegion(rect);

            bool result = Widgets.ButtonImage(rect, tex, iconColor ?? Color.white, GenUI.MouseoverColor);
            GUI.color = Color.white;
            row.IncrementPosition(num);
            if (!tooltip.NullOrEmpty())
            {
                TooltipHandler.TipRegion(rect, tooltip);
            }
            return result;
        }

        //
        public static bool CloseButtonCustom(Rect rectToClose, float buttonSize = 18)
        {
            return Widgets.ButtonImage(new Rect(rectToClose.x + rectToClose.width - buttonSize, rectToClose.y, buttonSize, buttonSize), TexButton.CloseXSmall, true);
        }

        public static bool ButtonBox(this WidgetRow row, string label, Color fill, Color border, float? fixedWidth = null)
        {
            Rect rect = row.ButtonRect(label, fixedWidth);
            DrawColoredBox(rect, fill, border, 1);
            Widgets.DrawHighlightIfMouseover(rect);

            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, label);
            Text.Anchor = default;
            return Widgets.ButtonInvisible(rect);
        }

        //Listing_Standard Extensions
        public static Rect GetNextRect(this Listing_Standard listing)
        {
            return new Rect(listing.curX, listing.curY, listing.ColumnWidth, Text.LineHeight);
        }

        public static void DoBGForNext(this Listing_Standard listing, Color color)
        {
            Rect rect = listing.GetNextRect();
            Widgets.DrawBoxSolid(rect, color);
        }

        internal static void TextureElement(this Listing_Standard listing, TextureElement tex)
        {
            Rect rect = listing.GetRect(Text.LineHeight);
            if (listing.BoundingRectCached == null || rect.Overlaps(listing.BoundingRectCached.Value))
            {
                DrawTextureWithMaterial(rect, tex.Texture, tex.Material, tex.TexCoords);
            }
        }

        public static void ClearFocusedControl(Rect rect, string name)
        {
            if (GUI.GetNameOfFocusedControl() != name) return;
            if (OriginalEventUtility.EventType == EventType.MouseDown && !rect.Contains(UIEventHandler.MouseOnScreen))
            {
                GUI.FocusControl(null);
            }
        }

        public static void TextFieldLabeled(this Listing_Standard listing, string label, ref string textVal, TextAnchor anchor = TextAnchor.MiddleRight)
        {
            Rect rect = listing.GetRect(Text.LineHeight);
            if (listing.BoundingRectCached == null || rect.Overlaps(listing.BoundingRectCached.Value))
            {
                Rect rect2 = rect.LeftHalf().Rounded();
                Rect rect3 = rect.RightHalf().Rounded();
                TextAnchor oldAnchor = Text.Anchor;
                Text.Anchor = anchor;
                Widgets.Label(rect2, label);
                Text.Anchor = oldAnchor;

                string text = $"TextFieldLabeled{rect3.y:F0}{rect3.x:F0}";
                GUI.SetNextControlName(text);
                textVal = Widgets.TextField(rect3, textVal);
                ClearFocusedControl(listing.listingRect, text);
            }
            listing.Gap(listing.verticalSpacing);
        }

        public static void TextFieldNumericLabeled<T>(this Listing_Standard listing, string label, ref T val, ref string buffer, float min = 0f, float max = 1E+09f, TextAnchor anchor = TextAnchor.MiddleRight) where T : struct
        {
            Rect rect = listing.GetRect(Text.LineHeight);
            if (listing.BoundingRectCached == null || rect.Overlaps(listing.BoundingRectCached.Value))
            {
                Rect rect2 = rect.LeftHalf().Rounded();
                Rect rect3 = rect.RightHalf().Rounded();
                TextAnchor oldAnchor = Text.Anchor;
                Text.Anchor = anchor;
                Widgets.Label(rect2, label);
                Text.Anchor = oldAnchor;

                string text = $"TextField{rect3.y:F0}{rect3.x:F0}";
                Widgets.TextFieldNumeric(rect3, ref val, ref buffer, min, max);
                ClearFocusedControl(listing.listingRect, text);
            }
            listing.Gap(listing.verticalSpacing);
        }

        //
        public static void DrawListedPart<T>(Rect rect, ref Vector2 scrollPos, List<T> elements, Action<Rect, UIPartSizes, T> drawProccessor, Func<T, UIPartSizes> heightFunc)
        {
            var height = elements.Sum(s => heightFunc(s).totalSize);
            var viewRect = new Rect(rect.x, rect.y, rect.width, height);
            Widgets.BeginScrollView(rect, ref scrollPos, viewRect, false);
            float curY = rect.y;
            for (var i = 0; i < elements.Count; i++)
            {
                var element = elements[i];
                var listingHeight = heightFunc(element);
                var listingRect = new Rect(rect.x, curY, rect.width, listingHeight.totalSize);
                if (i % 2 == 0)
                {
                    Widgets.DrawHighlight(listingRect);
                }
                drawProccessor(listingRect, listingHeight, element);
                curY += listingHeight.totalSize;
            }
            Widgets.EndScrollView();
        }

        //
        public static Vector2 GetNetworkValueReadoutSize(NetworkContainer container)
        {
            Vector2 size = new Vector2(10, 10);
            foreach (var type in container.AllStoredTypes)
            {
                Vector2 typeSize = Text.CalcSize($"{type.labelShort}: {container.ValueForType(type)}");
                size.y += 10 + 2;
                var sizeX = typeSize.x + 20;
                if (size.x <= sizeX)
                    size.x += sizeX;
            }
            return size;
        }

        public static float DrawNetworkValueTypeReadout(Rect rect, GameFont font, float textYOffset, NetworkContainerSet containerSet)
        {
            float height = 5;

            Widgets.BeginGroup(rect);
            Text.Font = font;
            Text.Anchor = TextAnchor.UpperLeft;
            foreach (var type in containerSet.AllTypes)
            {
                // float value = GetNetwork(Find.CurrentMap).NetworkValueFor(type);
                //if(value <= 0) continue;
                string label = $"{type}: {containerSet.GetValueByType(type)}";
                Rect typeRect = new Rect(5, height, 10, 10);
                Vector2 typeSize = Text.CalcSize(label);
                Rect typeLabelRect = new Rect(20, height + textYOffset, typeSize.x, typeSize.y);
                Widgets.DrawBoxSolid(typeRect, type.valueColor);
                Widgets.Label(typeLabelRect, label);
                height += 10 + 2;
            }
            Text.Font = default;
            Text.Anchor = default;
            Widgets.EndGroup();

            return height;
        }

        public static void DrawNetworkValueReadout(Rect rect, NetworkContainer container)
        {
            float height = 5;
            Widgets.BeginGroup(rect);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperLeft;
            foreach (var type in container.AllStoredTypes)
            {
                string label = $"{type.labelShort}: {container.ValueForType(type)}";
                Rect typeRect = new Rect(5, height, 10, 10);
                Vector2 typeSize = Text.CalcSize(label);
                Rect typeLabelRect = new Rect(20, height - 2, typeSize.x, typeSize.y);
                Widgets.DrawBoxSolid(typeRect, type.valueColor);
                Widgets.Label(typeLabelRect, label);
                height += 10 + 2;
            }
            Text.Font = default;
            Text.Anchor = default;
            Widgets.EndGroup();
        }
    }
}
