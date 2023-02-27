using System.Collections.Generic;

namespace TeleCore;


public class NetworkContainer : BaseContainer<NetworkValueDef>
{
    #region Constructors
    

    #endregion

    public void Notify_ParentDestroyed(DestroyMode mode, Map previousMap)
    {
        if (Parent == null || TotalStored <= 0 || mode == DestroyMode.Vanish) return;

        if (mode is DestroyMode.Deconstruct or DestroyMode.Refund && Props.leaveContainer &&
            Parent.NetworkPart.NetworkDef.portableContainerDef != null)
        {
            var container = (PortableContainerThing) ThingMaker.MakeThing(Parent.NetworkPart.NetworkDef.portableContainerDef);
            //var containerCopy = Copy<NetworkContainerPortable, IContainerHolderNetworkPortable>(Parent);
            container.SetupProperties(Parent.NetworkPart.NetworkDef, this, Props);
            GenSpawn.Spawn(container, ParentThing.Position, previousMap);
        }

        if (mode is DestroyMode.KillFinalize)
        {
            if (Props.explosionProps != null)
                if (TotalStored > 0)
                    //float radius = Props.explosionProps.explosionRadius * StoredPercent;
                    //int damage = (int)(10 * StoredPercent);
                    //var mainTypeDef = MainValueType.dropThing;
                    Props.explosionProps.DoExplosion(ParentThing.Position, previousMap, ParentThing);
            //GenExplosion.DoExplosion(Parent.Thing.Position, previousMap, radius, DamageDefOf.Bomb, Parent.Thing, damage, 5, null, null, null, null, mainTypeDef, 0.18f);
            if (Props.dropContents)
            {
                var i = 0;
                var drops = Get_ThingDrops().ToList();
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

        Data_Clear();
    }

    //Virtual Functions
    public override IEnumerable<Thing> Get_ThingDrops()
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
        Parent?.ContainerSet?.Notify_AddedValue(valueType, value, Parent.NetworkPart);
        base.Notify_AddedValue(valueType, value);

        //
        Parent.NetworkPart.Notify_ReceivedValue();
    }

    public override void Notify_RemovedValue(NetworkValueDef valueType, float value)
    {
        Parent?.ContainerSet?.Notify_RemovedValue(valueType, value, Parent.NetworkPart);
        base.Notify_RemovedValue(valueType, value);
    }
}

public class NetworkContainerThing : NetworkContainerBase<IContainerHolderNetworkThing, NetworkContainerThing>
{
    public NetworkContainerThing(IContainerHolderNetworkThing parent) : base(parent)
    {
    }

    public NetworkContainerThing(IContainerHolderNetworkThing parent, DefValueStack<NetworkValueDef> valueStack) : base(parent, valueStack)
    {
    }

    public NetworkContainerThing(IContainerHolderNetworkThing parent, List<NetworkValueDef> acceptedTypes) : base(parent, acceptedTypes)
    {
    }
}

