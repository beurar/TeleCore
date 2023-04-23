using System;
using System.Collections.Generic;
using Verse;

namespace TeleCore
{
    public abstract class DefInjectBase : IDisposable
    {
        public virtual void OnThingDefInject(ThingDef thingDef){}
        public virtual void OnPawnInject(ThingDef pawnDef) { }
        public virtual void OnBuildableDefInject(BuildableDef def) { }

        public virtual bool AcceptsSpecial(Def def) => true;
        public virtual void OnDefSpecialInjected(Def def) { }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual void Dispose(bool disposing)
        {
        }
    }
}
