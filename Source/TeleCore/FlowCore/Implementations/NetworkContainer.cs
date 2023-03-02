using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TeleCore;

public interface IContainerHolderNetworkThing : IContainerHolderThing<NetworkValueDef>
{
    public NetworkDef NetworkDef { get; }
}

public interface IContainerHolderNetwork : IContainerHolderNetworkThing
{
    INetworkSubPart NetworkPart { get; }
    NetworkContainerSet ContainerSet { get; }
}

public class NetworkContainerThing<THolder> : ValueContainerThing<NetworkValueDef, THolder>
    where THolder : IContainerHolderNetworkThing
{
    public NetworkContainerThing(ContainerConfig config, THolder holder) : base(config, holder)
    {
    }
}

public class NetworkContainer : NetworkContainerThing<IContainerHolderNetwork>
{
    public NetworkContainer(ContainerConfig config, IContainerHolderNetwork holder) : base(config, holder)
    {
    }
    
    public void Notify_ParentDestroyed(DestroyMode mode, Map previousMap)
    {
        if (Holder == null || TotalStored <= 0 || mode == DestroyMode.Vanish) return;

        if (mode is DestroyMode.Deconstruct or DestroyMode.Refund && Config.leaveContainer &&
            Holder.NetworkPart.NetworkDef.portableContainerDef != null)
        {
            GenSpawn.Spawn(PortableNetworkContainer.Create(this), ParentThing.Position, previousMap);
        }

        if (mode is DestroyMode.KillFinalize)
        {
            if (Config.explosionProps != null)
                if (TotalStored > 0)
                    //float radius = Props.explosionProps.explosionRadius * StoredPercent;
                    //int damage = (int)(10 * StoredPercent);
                    //var mainTypeDef = MainValueType.dropThing;
                    Config.explosionProps.DoExplosion(ParentThing.Position, previousMap, ParentThing);
            //GenExplosion.DoExplosion(Parent.Thing.Position, previousMap, radius, DamageDefOf.Bomb, Parent.Thing, damage, 5, null, null, null, null, mainTypeDef, 0.18f);
            if (Config.dropContents)
            {
                var i = 0;
                var drops = GetThingDrops().ToList();
                Predicate<IntVec3> pred = c => c.InBounds(previousMap) && c.GetEdifice(previousMap) == null;
                var action = delegate(IntVec3 c)
                {
                    if (i < drops.Count)
                    {
                        var drop = drops[i];
                        if (drop != null)
                        {
                            GenSpawn.Spawn(drop, c, previousMap);
                            drops.Remove(drop);
                        }

                        i++;
                    }
                };
                _ = TeleFlooder.Flood(previousMap, ParentThing.OccupiedRect(), action, pred, drops.Count);
            }
        }

        Clear();
    }

    //Virtual Functions
    public override IEnumerable<Thing> GetThingDrops()
    {
        foreach (var storedValue in StoredValuesByType)
        {
            if (storedValue.Key.thingDroppedFromContainer == null) continue;
            var count = Mathf.RoundToInt(storedValue.Value / storedValue.Key.valueToThingRatio);
            if (count <= 0) continue;
            yield return ThingMaker.MakeThing(storedValue.Key.thingDroppedFromContainer);
        }
    }

    public override void Notify_AddedValue(NetworkValueDef valueType, float value)
    {
        Holder?.ContainerSet?.Notify_AddedValue(valueType, value, Holder.NetworkPart);
        base.Notify_AddedValue(valueType, value);

        //
        Holder.NetworkPart.Notify_ReceivedValue();
    }

    public override void Notify_RemovedValue(NetworkValueDef valueType, float value)
    {
        Holder?.ContainerSet?.Notify_RemovedValue(valueType, value, Holder.NetworkPart);
        base.Notify_RemovedValue(valueType, value);
    }
}

