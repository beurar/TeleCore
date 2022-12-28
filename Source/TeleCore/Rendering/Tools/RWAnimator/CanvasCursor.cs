using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeleCore.Static;
using UnityEngine;
using Verse;

namespace TeleCore
{
    internal static class CanvasCursor
    {
        //
        private static Vector2 cursorHotspot;
        private static ManipulationMode? lastMode;
        private static bool usingDefault, usingCustom;

        public static void Notify_TriggeredMode(ManipulationMode? maniMode)
        {
            if (lastMode == maniMode) return;
            var shouldReset = maniMode == null || maniMode == ManipulationMode.None;
            var newMode = !shouldReset && (maniMode != ManipulationMode.None || maniMode != lastMode);
            if (newMode)
            {
                switch (maniMode)
                {
                    case ManipulationMode.Move:
                        cursorHotspot = new Vector2(TRContentDatabase.CustomCursor_Drag.width / 2f, TRContentDatabase.CustomCursor_Drag.height / 2f);
                        Cursor.SetCursor(TRContentDatabase.CustomCursor_Drag, cursorHotspot, CursorMode.Auto);
                        break;
                    case ManipulationMode.Resize:
                        break;
                    case ManipulationMode.Rotate:
                        cursorHotspot = new Vector2(TRContentDatabase.CustomCursor_Rotate.width / 2f, TRContentDatabase.CustomCursor_Rotate.height / 2f);
                        Cursor.SetCursor(TRContentDatabase.CustomCursor_Rotate, cursorHotspot, CursorMode.Auto);
                        break;
                }
                lastMode ??= maniMode;
                usingCustom = true;
                usingDefault = false;
            }
            else if (usingCustom && !usingDefault && shouldReset)
            {
                CustomCursor.Deactivate();
                CustomCursor.Activate();
                lastMode = null;
                usingCustom = false;
                usingDefault = true;
            }
        }
    }

}
