using System;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public interface IFocusable
    {
        public bool CanBeFocused { get; }
        public Rect FocusRect { get; }
        public int RenderLayer { get; }
    }

    public static class UIEventHandler
    {
        private static Rect?[] layers = new Rect?[255];
        private static UIElement[] elementLayers = new UIElement[255];
        private static bool IsInitialized = false;

        private static int CurrentLayer;
        public static Vector2 MouseOnScreen { get; private set; }
        public static IFocusable FocusedElement { get; private set; }
        public static UIElement[] Layers => elementLayers;

        public static bool IsFocused(IFocusable element) => element.Equals(FocusedElement);

        public static void RegisterLayer(UIElement element)
        {
            element.RenderLayer = CurrentLayer;
            elementLayers[CurrentLayer] = element;
            CurrentLayer++;
        }

        public static int GetLayerOf(UIElement element)
        {
            throw new NotImplementedException();
        }
        
        public static bool ElementIsCovered(IFocusable element)
        {
            for (int i = element.RenderLayer; i > -1; i--)
            {
                if (CurrentLayer == i) continue;
                if (layers[i].HasValue && Mouse.IsOver(layers[i].Value))
                {
                    TLog.Warning($"Tried to focus covered element: {elementLayers[element.RenderLayer]} at [{element.RenderLayer}] covered by [{i}]");
                    return true;
                }
            }
            return false;
        }


        public static void Begin()
        {
            if (IsInitialized)
            {
                TLog.Warning($"More calls to {nameof(UIEventHandler)}.{nameof(Begin)} than {nameof(UIEventHandler)}.{nameof(End)}, make sure to close the scope correctly.");
            }
            CurrentLayer = 0;
            MouseOnScreen = Event.current.mousePosition;
            IsInitialized = true;
        }

        public static void End()
        {
            MouseOnScreen = Vector2.zero;
            IsInitialized = false;
        }

        public static void StartFocusForced(IFocusable element)
        {
            if (element.CanBeFocused && !ElementIsCovered(element))
            {
                FocusedElement = element;
            }
        }

        public static void StartFocus(IFocusable element, Rect? markedRect = null)
        {
            if (element.CanBeFocused && Mouse.IsOver(element.FocusRect) && !ElementIsCovered(element))
            {
                if (markedRect.HasValue)
                {
                    layers[element.RenderLayer] = markedRect.Value;
                }
                FocusedElement = element;
            }
        }

        public static void StopFocus(IFocusable element)
        {
            if (IsFocused(element))
            {
                FocusedElement = null;
                layers[element.RenderLayer] = null;
            }
        }
    }
}
