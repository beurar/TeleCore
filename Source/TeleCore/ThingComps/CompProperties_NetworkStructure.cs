using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Color = System.Drawing.Color;

namespace TeleCore
{
    /// <summary>
    /// 
    /// </summary>
    public class CompProperties_NetworkStructure : CompProperties
    {
        public List<NetworkSubPartProperties> networks;
        public string generalIOPattern;

        public CompProperties_NetworkStructure()
        {
            this.compClass = typeof(Comp_NetworkStructure);
        }
    }
}
