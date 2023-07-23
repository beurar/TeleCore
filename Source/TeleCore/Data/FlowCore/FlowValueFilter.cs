using System.Collections.Generic;
using TeleCore.Network.Data;

namespace TeleCore.FlowCore;

public class FlowValueFilter<TValue>
    where TValue : IFlowValueDef
{
    public List<TValue> allowedValues;
}