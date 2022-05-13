using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public static class Scribe_Rect
    {
        public static void Look(ref Rect value, string label, Rect defaultValue = default)
        {
            defaultValue = Rect.zero;
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                Scribe.saver.WriteElement(label, value.ToStringSimple());
            }

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {

                value = ScribeExtractor.ValueFromNode<Rect>(Scribe.loader.curXmlParent[label], defaultValue);
            }
        }

        public static string ToStringSimple(this Rect rect)
        {
            return $"({rect.x},{rect.y},{rect.width},{rect.height})";
        }
    }
}
