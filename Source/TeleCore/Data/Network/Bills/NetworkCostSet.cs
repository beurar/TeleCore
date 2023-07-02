using System.Collections.Generic;
using System.Linq;

namespace TeleCore.Network.Bills;

public class NetworkCostSet
{
    private List<NetworkValueDef> acceptedTypes;

    //
    public float mainCost;
    public List<NetworkCostValue> specificCosts;

    private List<NetworkCostValue> specificCostsWithValues;

    //Cache
    private double? totalCost;
    private double? totalSpecificCost;

    public bool HasSpecifics => SpecificCosts.Any();

    public double TotalSpecificCost
    {
        get
        {
            totalSpecificCost ??= HasSpecifics ? SpecificCosts.Sum(t => t.value) : 0;
            return totalSpecificCost.Value;
        }
    }

    public List<NetworkValueDef> AcceptedValueTypes
    {
        get
        {
            acceptedTypes ??= specificCosts.Select(t => t.valueDef).ToList();
            return acceptedTypes;
        }
    }

    public List<NetworkCostValue> SpecificCosts
    {
        get
        {
            specificCostsWithValues ??= specificCosts.Where(t => t.HasValue).ToList();
            return specificCostsWithValues;
        }
    }

    public double TotalCost
    {
        get
        {
            totalCost ??= mainCost + TotalSpecificCost;
            return totalCost.Value;
        }
    }

    public override string ToString()
    {
        return $"[Total: {TotalCost}|AT: {AcceptedValueTypes.Count}|SC: {SpecificCosts.Count}]";
    }
}