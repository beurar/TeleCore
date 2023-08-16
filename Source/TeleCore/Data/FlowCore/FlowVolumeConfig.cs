using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TeleCore.FlowCore;

public class FlowVolumeConfig<T> where T : FlowValueDef
{
    private readonly List<T> _values = new();
    private bool isReady;
    
    private Values values;

    public int capacity;
    public int area = 1;
    public int elevation = 0;
    public int height = 1;

    public bool storeEvenly;
    public bool leaveContainer;
    public bool dropContents;
    
    private class Values
    {
        public List<T> allowedValues;
        public FlowValueCollectionDef fromCollection;
    }
    
    public double Volume => capacity;
    
    public List<T> AllowedValues
    {
        get
        {
            if (isReady) return _values;
            if (values == null) return _values;
            
            //Prepare
            if(!values.allowedValues.NullOrEmpty())
                _values.AddRange(values.allowedValues);
            if (values.fromCollection == null) return _values;
            foreach (var var in values.fromCollection.ValueDefs.Cast<T?>())
            {
                _values.Add(var);
            }
            isReady = true;
            return _values;
        }
        set => _values.AddRange(value);
    }

    public void PostLoad()
    {
    }

    //Note:We dont need this approach for now
    //public double Volume => area * height * AREA_VALUE;
}
