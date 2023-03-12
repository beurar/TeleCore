using System.Collections.Generic;
using Verse;

namespace TeleCore
{
    public class TurretTopProperties
    {
        public GraphicData topGraphic;
        public List<TurretBarrelProperties> barrels;

        public float aimSpeed = 20f;

        //
        public float resetSpeed = 5;
        public float recoilSpeed = 150;
        
        public IntRange idleDuration = new IntRange(50, 200);
        public IntRange idleInterval = new IntRange(150, 350);
    }
}
