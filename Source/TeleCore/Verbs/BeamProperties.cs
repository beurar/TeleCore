using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public class BeamProperties
    {
        [Unsaved] 
        private VerbProperties_Extended parent;

        //
        public DamageDef damageDef;
        public int damageBase = 100;
        public float armorPenetration;
        public int damageTicks = 10;
        public float stoppingPower;
        public float staggerTime = 95.TicksToSeconds();

        //
        public EffecterDef impactEffecter;
        public ExplosionProperties impactExplosion;
        public FilthSpawnerProperties impactFilth;
        
        public ThingDef BeamMoteDef => parent.beamMoteDef;
        public FleckDef BeamGroundFleckDef => parent.beamGroundFleckDef;
        public float BeamWidth => parent.beamWidth;
        public float BeamMaxDeviation => parent.beamMaxDeviation;
        

        //public Material BeamMat => cachedBeamMat ??= MaterialPool.MatFrom(beamTexturePath, ShaderDatabase.MoteGlow);
        public void SetParent(VerbProperties_Extended verbprops)
        {
            this.parent = verbprops;
        }
    }
}
