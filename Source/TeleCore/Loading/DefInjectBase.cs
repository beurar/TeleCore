using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TeleCore
{
    public abstract class DefInjectBase : IDisposable
    {
        public virtual void OnThingDefInject(ThingDef thingDef){}
        public virtual void OnPawnInject(ThingDef pawnDef) { }
        public virtual void OnBuildableDefInject(BuildableDef def) { }

        public void Dispose()
        {
        }
    }
}
