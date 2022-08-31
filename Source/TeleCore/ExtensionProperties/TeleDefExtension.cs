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
        public SubMenuDesignation subMenuDesignation;

        //
        public List<ThingGroupDef> thingGroups = new List<ThingGroupDef>(){ThingGroupDefOf.All};

        public List<GraphicData> extraGraphics;

        public FXDefExtension graphics;
        public TurretDefExtension turret;
        public ProjectileDefExtension projectile;


    }

    public class SubMenuDesignation
    {
        public SubThingGroupDef groupDef;
        public SubThingCategory category;
        public bool hidden;

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            var innerValue = xmlRoot.InnerText;
            string s = Regex.Replace(innerValue, @"\s+", "");
            string[] array = s.Split(',');
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, $"{nameof(groupDef)}", array[0]);
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, $"{nameof(category)}", array[1]);
        }
    }
}
