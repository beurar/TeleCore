using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public class HediffComp_Icon : HediffComp
    {
        private TextureAndColor icon;

        public HediffCompProperties_Icon Props => (HediffCompProperties_Icon)base.props;

        public override TextureAndColor CompStateIcon
        {
            get
            {
                if (!icon.HasValue)
                {
                    icon = new TextureAndColor(ContentFinder<Texture2D>.Get(Props.iconPath), Props.color);
                }
                return icon;
            }
        }
    }

    public class HediffCompProperties_Icon : HediffCompProperties
    {
        public string iconPath = null;
        public Color color = Color.white;

        public HediffCompProperties_Icon()
        {
            compClass = typeof(HediffComp_Icon);
        }
    }
}
