using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using UnityEngine.UIElements;
using Verse;
using Verse.Sound;

namespace TeleCore
{
    public static class TWidgets
    {
        //World To ScreenSpace
        public static void DrawBoxOnThing(Thing thing)
        {
            var v = ToScreenPos(thing.TrueCenter());
            
            var driver = Find.CameraDriver;
            var size = 1 * driver.CellSizePixels;
            var sizeHalf = size * 0.5f;
            
            var rect = new Rect(v.x - sizeHalf, v.y - sizeHalf, size, size);
            DrawColoredBox(rect, new Color(1, 1, 1, 0.125f), Color.white, 1);
        }
        
        public static Vector2 ToScreenPos(this Vector3 vec)
        {
            Vector2 vector = Find.Camera.WorldToScreenPoint(vec) / Prefs.UIScale;
            vector.y = (float)UI.screenHeight - vector.y;
            return vector;
        }

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

        public static float VerticalSlider(Rect rect, float value, float min, float max, float roundTo = -1f, bool enabled = true)
        {
            var tempEnabled = GUI.enabled;
            GUI.enabled = enabled;
            float num = GUI.VerticalSlider(rect, value, max, min);
            if (roundTo > 0f)
            {
                num = (float)Mathf.RoundToInt(num / roundTo) * roundTo;
            }
            if (value != num)
            {
                SoundDefOf.DragSlider.PlayOneShotOnCamera(null);
            }
            GUI.enabled = tempEnabled;
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
        public static Rect RectFitted(Rect outerRect, Vector2 uvScale, Vector2 textureSize, TexCoordAnchor texCoordAnchor, TexStretchMode stretchMode, float scale = 1)
        {
            var size = uvScale;
            if (stretchMode == TexStretchMode.Normal)
                size = textureSize;
            
            Rect rect = RectFitted(outerRect, scale, size);
            return GenTransform.OffSetByCoordAnchor(outerRect, rect, texCoordAnchor);
        }

        private static Rect RectFitted(Rect outerRect, float scale, Vector2 texProportions)
        {
            Rect rect = new Rect(0f, 0f, texProportions.x, texProportions.y);
            float num;
            if (rect.width / rect.height < outerRect.width / outerRect.height)
            {
                num = outerRect.height / rect.height;
            }
            else
            {
                num = outerRect.width / rect.width;
            }
            num *= scale;
            rect.width *= num;
            rect.height *= num;
            rect.x = outerRect.x + outerRect.width / 2f - rect.width / 2f;
            rect.y = outerRect.y + outerRect.height / 2f - rect.height / 2f;
            return rect;
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
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }
            
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
        public static void Checkbox(this WidgetRow row, ref bool checkOn, bool active, float width = 24)
        {
            row.IncrementYIfWillExceedMaxWidth(width);
            Rect rect = new Rect(row.LeftX(width), row.curY, width, 24f);
            
            if (active && Widgets.ButtonInvisible(rect, true))
            {
                checkOn = !checkOn;
                if (checkOn)
                    SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera(null);
                else
                    SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera(null);
            }
            Widgets.CheckboxDraw(rect.x, rect.y, checkOn, !active, 24f, null, null);
            
            row.IncrementPosition(width);
        }
        
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

        public static void Highlight(this WidgetRow row, float width = 24)
        {
           Widgets.DrawHighlightIfMouseover(new Rect(row.curX, row.curY, width, 24));
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

        public static void MultiCheckboxLabeled(this Listing_Standard listing, string label, ref bool[] checkOn, string tooltip = null, float height = 0f, float labelPct = 1f)
        {
            float height2 = (height != 0f) ? height : Text.CalcHeight(label, listing.ColumnWidth * labelPct);
            Rect rect = listing.GetRect(height2, labelPct);
            rect.width = Math.Min(rect.width + 24f, listing.ColumnWidth);
            if (listing.BoundingRectCached == null || rect.Overlaps(listing.BoundingRectCached.Value))
            {
                if (!tooltip.NullOrEmpty())
                {
                    if (Mouse.IsOver(rect))
                    {
                        Widgets.DrawHighlight(rect);
                    }
                    TooltipHandler.TipRegion(rect, tooltip);
                }

                MultiCheckboxLabeled(rect, label, ref checkOn);
            }
            listing.Gap(listing.verticalSpacing);
        }
        
        
        public static void MultiCheckboxLabeled(Rect rect, string label, ref bool[] checkOn, bool disabled = false, Texture2D texChecked = null, Texture2D texUnchecked = null, bool placeCheckboxNearText = false)
        {
            TextAnchor anchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            if (placeCheckboxNearText)
            {
                rect.width = Mathf.Min(rect.width, Text.CalcSize(label).x + 24f + 10f);
            }
            Rect rect2 = rect;
            rect2.xMax -= 24f;
            Widgets.Label(rect2, label);
            for (var i = 0; i < checkOn.Length; i++)
            {
                //var refBool = checkOn[i];
                rect.Set(rect.xMax - ((i+1) * 24), rect.y, 24, 24);
                if (!disabled && Widgets.ButtonInvisible(rect, true))
                {
                    checkOn[i] = !checkOn[i];
                    if (checkOn[i])
                    {
                        SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera(null);
                    }
                    else
                    {
                        SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera(null);
                    }
                }
                //y: + (rect.height - 24f) / 2f
                Widgets.CheckboxDraw(rect.x, rect.y, checkOn[i], disabled, 24f, null, null);
            }
            Text.Anchor = anchor;
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
                Vector2 typeSize = Text.CalcSize($"{type.labelShort}: {container.TotalStoredOf(type)} ({container.StoredPercentOf(type).ToStringPercent()})");
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

        public static Vector2 FittedSizeFor(Texture2D texture, float width)
        {
            Vector2 dimensions = new Vector2(texture.width, texture.height);
            float mainPct = dimensions.x / width;
            dimensions /= mainPct;
            return dimensions;
        }

        public static void DrawNetworkValueReadout(Rect rect, NetworkContainer container)
        {
            float height = 5;
            Widgets.DrawMenuSection(rect);
            Widgets.BeginGroup(rect);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperLeft;
            foreach (var type in container.AllStoredTypes)
            {
                string label = $"{type.labelShort}: {container.TotalStoredOf(type)} ({container.StoredPercentOf(type).ToStringPercent()})";
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

        //Internals
        internal static string GetPathOf(GraphicData data)
        {
            if (data != null && data.graphicClass == typeof(Graphic_Single))
            {
                return data.texPath;
            }
            return null;
        }

        internal static Texture2D TextureForFleck(FleckDef fleck)
        {
            Texture2D texture = null;
            if (fleck.graphicData is {Graphic: { }})
                texture = (Texture2D)fleck.graphicData.Graphic.MatSingle.mainTexture;
            if (texture == null && fleck.randomGraphics != null)
            {
                var randomGraphic = fleck.randomGraphics.FirstOrFallback(c => GetPathOf(c) != null);
                if(randomGraphic != null)
                    texture = (Texture2D)randomGraphic.Graphic.MatSingle.mainTexture;
            }
            return texture ?? BaseContent.BadTex;
        }

        internal static Texture2D TextureForThingDef(ThingDef def)
        {
            Texture2D texture = null;
            if (def.graphicData is {Graphic: { }})
                texture = (Texture2D)def.graphicData.Graphic.MatSingle.mainTexture;
            if (texture == null && def.graphicData.Graphic is Graphic_Random random)
            {
                var randomGraphic = random.subGraphics.FirstOrDefault();
                if(randomGraphic != null)
                    texture = (Texture2D)randomGraphic.MatSingle.mainTexture;
            }
            return texture ?? BaseContent.BadTex;
        }
        
        internal static Texture2D TextureForFleckMote(Def def)
        {
            Texture2D texture = null;
            if (def is ThingDef mote)
            {
                texture = mote.uiIcon;
                return texture;
            }
            if (def is FleckDef fleck)
            {
                if (fleck.graphicData is {Graphic: { }})
                    texture = (Texture2D)fleck.graphicData.Graphic.MatSingle.mainTexture;
                if (texture == null && fleck.randomGraphics != null)
                {
                    var randomGraphic = fleck.randomGraphics.FirstOrFallback(c => GetPathOf(c) != null);
                    if(randomGraphic != null)
                        texture = (Texture2D)randomGraphic.Graphic.MatSingle.mainTexture;
                }
            }
            return texture;
        }

        //
        /*
        internal static void DrawFlagAtlas(Rect rect)
        {
            rect = rect.Rounded();
            var texture2D = TeleContent.TimeFlag;

            Rect drawRect;
            Rect uvRect;

            //Draw Left
            Widgets.DrawTexturePart(drawRect, uvRect, texture2D);
        }
        */
        public static void DrawTextureInCorner(Rect rect, Texture2D texture, float textureWidth, TextAnchor anchor, Vector2 offset = default, Action clickAction = null)
        {
            Rect newRect = new Rect();
            Vector2 size = FittedSizeFor(texture, textureWidth);
            switch (anchor)
            {
                case TextAnchor.UpperLeft:
                    newRect = new Rect(rect.x, rect.y, size.x, size.y);
                    break;
                case TextAnchor.LowerLeft:
                    newRect = new Rect(rect.x, rect.yMax - size.y, size.x, size.y);
                    break;
                case TextAnchor.UpperRight:
                    newRect = new Rect(rect.xMax - size.x, rect.y, size.x, size.y);
                    break;
                case TextAnchor.LowerRight:
                    newRect = new Rect(rect.xMax - size.x, rect.yMax - size.y, size.x, size.y);
                    break;
            }
            newRect.Set(newRect.x + offset.x, newRect.y + offset.y, newRect.width, newRect.height);
            
            //
            if (clickAction != null)
            {
                if (Widgets.ButtonImage(newRect, texture, false))
                {
                    clickAction.Invoke();
                }
                return;
            }
            
            //
            Widgets.DrawTextureFitted(newRect, texture, 1f);
        }
        
        //
        public static void DrawHalfArrow(Vector2 start, Vector2 end, Color color, float width)
        {
            var diffVec = end - start;
            var magnitude = diffVec.magnitude;
            var angle = diffVec.ToAngle();

            var atlas = TeleContent.EdgeArrow;
            Rect rect = new Rect(start, new Vector2(magnitude, width));

            var previousColor = GUI.color;
            GUI.color = color;
            var matrix = GUI.matrix;
            UI.RotateAroundPivot(-angle, rect.position);
            {
                Rect drawRect;
                Rect uvRect;
                Widgets.BeginGroup(rect);
                {
                    //Draw ArrowLine
                    drawRect = new Rect(0f, 0f, magnitude - width, width);
                    uvRect = new Rect(0f, 0.5f, 0.5f, 0.5f);
                    Widgets.DrawTexturePart(drawRect, uvRect, atlas);

                    //Draw ArrowTip
                    drawRect = new Rect(drawRect.width, 0f, width, width);
                    uvRect = new Rect(0.5f, 0.5f, 0.5f, 0.5f);
                    Widgets.DrawTexturePart(drawRect, uvRect, atlas);
                }
                Widgets.EndGroup();

            }
            GUI.matrix = matrix;
            GUI.color = previousColor;
        }
    }
}
