using System;
using System.Collections.Generic;
using System.Linq;

namespace TeleCore;

public class ContainerConfig
{
    public Type containerClass = typeof(BaseContainer<FlowValueDef>);
        
    public int baseCapacity = 0;
    public string containerLabel;
        
    public bool storeEvenly = false;
    public bool dropContents = false;
    public bool leaveContainer = false;

    public List<FlowValueDef> valueDefs;
        
    public ExplosionProperties explosionProps;

    public ContainerConfig Copy()
    {
        return new ContainerConfig
        {
            containerClass = this.containerClass,
            baseCapacity = baseCapacity,
            storeEvenly = storeEvenly,
            dropContents = dropContents,
            leaveContainer = leaveContainer,
            explosionProps = explosionProps,
        };
    }
    
    /*public List<NetworkValueDef> ContainerValues
    {
        get
        {
            if (allowedValuesInt == null)
            {
                var list = new List<NetworkValueDef>();
                if (defFilter.fromDef != null)
                {
                    list.AddRange(defFilter.fromDef.NetworkValueDefs);
                }
                if (!defFilter.values.NullOrEmpty())
                {
                    list.AddRange(defFilter.values);
                }
                allowedValuesInt = list.Distinct().ToList();
            }
            return allowedValuesInt;
        }
    }*/
}