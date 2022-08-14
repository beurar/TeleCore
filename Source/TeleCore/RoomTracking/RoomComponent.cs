using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TeleCore
{
    public abstract class RoomComponent
    {
        private RoomTracker parent;

        public RoomTracker Parent => parent;
        public Map Map => Parent.Map;
        public Room Room => Parent.Room;

        public List<Thing> ContainedPawns => Parent.ContainedPawns;

        public bool Disbanded => Parent.IsDisbanded;

        public virtual void Create(RoomTracker parent)
        {
            this.parent = parent;
        }

        public virtual void Disband(RoomTracker parent, Map map) { }
        public virtual void Notify_Reused() { }
        //public virtual void Notify_() { }
        public virtual void Notify_RoofClosed() { }
        public virtual void Notify_RoofOpened() { }
        public virtual void Notify_RoofChanged() { }
        public virtual void Notify_BorderThingAdded(Thing thing) { }
        public virtual void Notify_ThingAdded(Thing thing) { }
        public virtual void Notify_ThingRemoved(Thing thing) { }

        public virtual void Notify_PawnEnteredRoom(Pawn pawn) { }
        public virtual void Notify_PawnLeftRoom(Pawn pawn) { }

        public virtual void Reset() {}
        public virtual void PreApply() { }

        public virtual void FinalizeApply() { }

        public virtual void CompTick() { }

        public virtual void OnGUI() { }

        public virtual void Draw() { }

        public override string ToString()
        {
            return $"{nameof(this.GetType)}[{Room.ID}]";
        }
    }
}
