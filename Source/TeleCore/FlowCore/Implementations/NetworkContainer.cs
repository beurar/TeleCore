using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace TeleCore.FlowCore;

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
    public NetworkContainerThing(ContainerConfig<NetworkValueDef> config, THolder holder) : base(config, holder)
    {
    }
}

public class NetworkContainer : NetworkContainerThing<IContainerHolderNetwork>
{
    public NetworkContainer(ContainerConfig<NetworkValueDef> config, IContainerHolderNetwork holder) : base(config, holder)
    {
    }

    public NetworkContainer(ContainerConfig<NetworkValueDef> config, List<NetworkValueDef> extraValueDefs, IContainerHolderNetwork holder) : base(config, holder)
    {
        AcceptedTypes.AddRange(extraValueDefs);
    }

    //Virtual Functions
    public override IEnumerable<Thing> GetThingDrops()
    {
        foreach (var storedValue in StoredValuesByType)
        {
            if (storedValue.Key.ThingDroppedFromContainer == null) continue;
            var count = Mathf.RoundToInt(storedValue.Value / storedValue.Key.ValueToThingRatio);
            if (count <= 0) continue;
            yield return ThingMaker.MakeThing(storedValue.Key.ThingDroppedFromContainer);
        }
    }

    public override void OnParentDestroyed(DestroyMode mode, Map previousMap)
    {
        base.OnParentDestroyed(mode, previousMap);
        if (mode is DestroyMode.Deconstruct or DestroyMode.Refund && Config.leaveContainer)
        {
            GenSpawn.Spawn(PortableNetworkContainer.Create(this), ParentThing.Position, previousMap);
        }
    }

    public override void Notify_AddedValue(NetworkValueDef valueType, float value)
    {
        Holder?.ContainerSet?.Notify_AddedValue(valueType, value, Holder.NetworkPart);
        base.Notify_AddedValue(valueType, value);

        //
        Holder.NetworkPart?.Notify_ReceivedValue();
    }

    public override void Notify_RemovedValue(NetworkValueDef valueType, float value)
    {
        Holder?.ContainerSet?.Notify_RemovedValue(valueType, value, Holder.NetworkPart);
        base.Notify_RemovedValue(valueType, value);
    }
}

