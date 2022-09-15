using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TeleCore
{
    /// <summary>
    /// If set on a projectile's def, on Impact, all of these effects will be created - if available.
    /// </summary>
    public class ProjectileDefExtension
    {
        public EffecterDef impactEffecter;
        public ExplosionProperties impactExplosion;
        public FilthSpawnerProperties impactFilth;
    }
}
