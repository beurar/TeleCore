using System.Collections.Generic;
using TeleCore.Network.Data;

namespace TeleCore.FlowCore;

public class FlowValueFilter<TValue>
where TValue : FlowValueDef
{
    public List<TValue> allowedValues;
}