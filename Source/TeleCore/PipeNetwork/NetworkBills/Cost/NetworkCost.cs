using System.Linq;
using TeleCore.FlowCore;
using Verse;

namespace TeleCore
{
    public class NetworkCost
    {
        public NetworkCostSet costSet;
        public bool useDirectStorage = false;

        public NetworkCostSet Cost => costSet;

        //Validation
        public bool CanPayWith(Comp_NetworkStructure networkComp)
        {
            return networkComp.NetworkParts.Any(CanPayWith);
        }

        public bool CanPayWith(INetworkSubPart subPart)
        {
            return useDirectStorage ? CanPayWith(subPart.Container) : CanPayWith(subPart.Network);
        }

        private bool CanPayWith(NetworkContainer directContainer)
        {
            if (directContainer.TotalStored < Cost.TotalCost) return false;
            float totalNeeded = Cost.TotalCost;

            //
            if (Cost.HasSpecifics)
            {
                foreach (var specificCost in Cost.SpecificCosts)
                {
                    if (directContainer.StoredValueOf(specificCost.valueDef) >= specificCost.value)
                        totalNeeded -= specificCost.value;
                }
            }

            //
            if (Cost.mainCost > 0)
            {
                foreach (var type in Cost.AcceptedValueTypes)
                {
                    totalNeeded -= directContainer.StoredValueOf(type);
                }
            }

            return totalNeeded == 0;
        }

        private bool CanPayWith(PipeNetwork wholeNetwork)
        {
            var totalNetworkValue = wholeNetwork.TotalNetworkValue;
            float totalNeeded = Cost.TotalCost;
            if (totalNetworkValue < totalNeeded) return false;
            //Check For Specifics
            if (Cost.HasSpecifics)
            {
                foreach (var typeCost in Cost.SpecificCosts)
                {
                    var specCost = typeCost.value;
                    if (wholeNetwork.TotalValueFor(typeCost.valueDef) >= specCost)
                        totalNeeded -= specCost;
                }
            }

            //Check For Generic Cost Value
            if (Cost.mainCost > 0)
            {
                if (wholeNetwork.TotalStorageNetworkValue >= Cost.mainCost)
                {
                    totalNeeded -= Cost.mainCost;
                }
            }

            return totalNeeded == 0;
        }

        //Process
        public void DoPayWith(Comp_NetworkStructure networkStructure)
        {
            if (useDirectStorage)
                DoPayWithContainer(networkStructure);
            else
                DoPayWithNetwork(networkStructure);
        }

        private void DoPayWithContainer(Comp_NetworkStructure structure)
        {
            var totalCost = Cost.TotalCost;
            if (totalCost <= 0) return;

            foreach (var typeCost in Cost.SpecificCosts)
            {
                var container = structure[typeCost.valueDef.networkDef].Container;
                if (container.TryConsume(typeCost.valueDef, typeCost.value))
                    totalCost -= typeCost.value;
            }

            foreach (var type in Cost.AcceptedValueTypes)
            {
                var container = structure[type.networkDef].Container;
                
                var result = container.TryRemoveValue(type, totalCost);
                if (result)
                {
                    totalCost -= result.ActualAmount;
                }
            }

            if (totalCost > 0)
                TLog.Warning($"Paying {this} with {structure.Thing} had leftOver {totalCost}");
            if (totalCost < 0)
                TLog.Warning($"Paying {this} with {structure.Thing} was too much: {totalCost}");
        }

        private void DoPayWithNetwork(Comp_NetworkStructure structure)
        {
            var totalCost = Cost.TotalCost;
            if (totalCost <= 0) return;

            foreach (var storage in structure.NetworkParts.Select(s => s.Network).SelectMany(n => n.PartSet[NetworkRole.Storage]).TakeWhile(storage => !(totalCost <= 0)))
            {
                foreach (var typeCost in Cost.SpecificCosts)
                {
                    if (storage.Container.TryConsume(typeCost.valueDef, typeCost.value))
                        totalCost -= typeCost.value;
                }

                foreach (var type in Cost.AcceptedValueTypes)
                {
                    var result = storage.Container.TryRemoveValue(type, totalCost);
                    if (result)
                    {
                        totalCost -= result.ActualAmount;
                    }
                }
            }

            if (totalCost > 0)
                TLog.Warning($"Paying {this} with {structure.Thing} had leftOver: {totalCost}");
            if (totalCost < 0)
                TLog.Warning($"Paying {this} with {structure.Thing} was too much: {totalCost}");
        }

        public override string ToString()
        {
            return $"{Cost}|Direct: {useDirectStorage}";
        }
    }
}
