using System.Collections.Generic;

namespace TeleCore.FlowCore;

public class FlowVolumeConfig<T> where T : FlowValueDef
{
    //private const int AREA_VALUE = 128;
    public FlowValueFilter<T> _filter;
    public List<T> allowedValues;

    public int capacity;
    public int area = 1;
    public int elevation = 0;
    public int height = 1;

    //We dont need this approach for now
    public double Volume => capacity;
    //public double Volume => area * height * AREA_VALUE;

    public void Attach(FlowValueFilter<T> valueFilter)
    {
        _filter = valueFilter;
    }
}
