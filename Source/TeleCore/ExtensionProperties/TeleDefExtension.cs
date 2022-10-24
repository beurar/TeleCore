using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using TeleCore.Static;
using Verse;

namespace TeleCore
{
    public class TeleDefExtension : DefModExtension
    {
        //
        public ThingGroupCollection thingGroups = new ThingGroupCollection();
        public List<GraphicData> extraGraphics;
        public ProjectileDefExtension projectile;
    }

    //
    public class ThingGroupCollection
    {
        public List<ThingGroupDef> groups = new List<ThingGroupDef>(){ThingGroupDefOf.All};

        /*TODO: Add listed items to list
        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            if (xmlRoot.FirstChild?.Name == "<li>")
            {
                return;
            }
            var innerValue = xmlRoot.InnerText;
            string s = Regex.Replace(innerValue, @"\s+", "");
            string[] array = s.Split('|');
            for (var i = 0; i < array.Length; i++)
            {
                var groupDefname = array[i];
            }
        }
        */
    }
}
