using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TeleCore.Static;
using Verse;

namespace TeleCore.FlowCore;

public class ContainerConfig<TValue> : IExposable
where TValue: FlowValueDef
{
    [Unsaved]
    private List<TValue> allowedValuesInt = null!;
    
    public Type containerClass = typeof(ValueContainerBase<TValue>);
        
    public int baseCapacity = 0;
    public string containerLabel;
        
    //TODO: 
    public bool storeEvenly = false;
    public bool dropContents = false;
    
    //Assumed for Networks only
    public bool leaveContainer = false;
    public ThingDef droppedContainerDef;
    public ExplosionProperties explosionProps;
    
    //
    public ContainerValueFilter valueDefs;

    public List<TValue> AllowedValues
    {
        get
        {
            if (allowedValuesInt == null)
            {
                var list = new List<TValue>();
                if (valueDefs != null)
                {
                    if (valueDefs.fromCollection != null)
                    {
                        list.AddRange(valueDefs.fromCollection.ValueDefs.Cast<TValue>());
                    }
                    if (!valueDefs.values.NullOrEmpty())
                    {
                        list.AddRange(valueDefs.values);
                    }
                }
                
                allowedValuesInt = list.Distinct().ToList();
            }
            return allowedValuesInt;
        }
    }
    
    public sealed class ContainerValueFilter : IExposable
    {
        public FlowValueCollectionDef fromCollection;
        public List<TValue> values;


        public static implicit operator ContainerValueFilter(List<TValue> values) => new ContainerValueFilter()
        {
            values = values,
        };
        
        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            if (xmlRoot.FirstChild.Name == "li")
            {
                values = DirectXmlToObject.ObjectFromXml<List<TValue>>(xmlRoot, true);
                return;
            }
                
            var fromDefNode = xmlRoot.SelectSingleNode(nameof(fromCollection));
            if (fromDefNode != null)
            {
                DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, $"{nameof(fromCollection)}", fromDefNode.InnerText);
            }

            //
            var valuesNode = xmlRoot.SelectSingleNode(nameof(values));
            if (valuesNode != null)
            {
                values = DirectXmlToObject.ObjectFromXml<List<TValue>>(valuesNode, true);
            }
        }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref fromCollection, nameof(fromCollection));
            Scribe_Collections.Look(ref values, nameof(values), LookMode.Def);
        }
    }
    
    public void ExposeData()
    {
        Scribe_Values.Look(ref baseCapacity, nameof(baseCapacity));
        Scribe_Values.Look(ref containerLabel, nameof(containerLabel));
        Scribe_Values.Look(ref storeEvenly, nameof(storeEvenly));
        Scribe_Values.Look(ref dropContents, nameof(dropContents));
        Scribe_Values.Look(ref leaveContainer, nameof(leaveContainer));
        Scribe_Deep.Look(ref valueDefs, nameof(valueDefs));
        Scribe_Deep.Look(ref explosionProps, nameof(explosionProps));
    }
    
    public ContainerConfig<TValue> Copy()
    {
        return new ContainerConfig<TValue>
        {
            containerClass = this.containerClass,
            baseCapacity = baseCapacity,
            storeEvenly = storeEvenly,
            dropContents = dropContents,
            leaveContainer = leaveContainer,
            explosionProps = explosionProps,
        };
    }
}