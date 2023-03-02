using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace TeleCore;

public enum ContainerFillState
{
    Full,
    Partial,
    Empty
}

public struct FlowValueFilterSettings
{
    public bool canReceive = true;
    public bool canStore = true;

    public FlowValueFilterSettings()
    {
    }
}

//Base Container Template for Values
public abstract class ValueContainerBase<TValue> : IExposable where TValue : FlowValueDef
{
    //
    private readonly ContainerConfig _config;
    
    //Dynamic settings
    protected float capacity;
    
    //Dynamic Data
    protected Color colorInt;
    protected float totalStoredCache;
    protected Dictionary<TValue, float> storedValues = new();
    protected Dictionary<TValue, FlowValueFilterSettings> filterSettings = new();
    
    //
    public virtual string Label => _config.containerLabel;
    public Color Color => colorInt;
    
    //Capacity Values
    public float Capacity => capacity;
    public float TotalStored => totalStoredCache;
    public float StoredPercent => TotalStored / Capacity;

    public ContainerConfig Config => _config;
    
    //Capacity State
    public ContainerFillState FillState
    {
        get
        {
            return totalStoredCache switch
            {
                0 => ContainerFillState.Empty,
                var n when n >= Capacity => ContainerFillState.Full,
                _ => ContainerFillState.Partial
            };
        }
    }
    
    public bool ContainsForbiddenType => AllStoredTypes.Any(t => !CanHoldValue(t));

    //Value Stuff
    public Dictionary<TValue, float> StoredValuesByType => storedValues;
    public IReadOnlyCollection<TValue> AllStoredTypes => storedValues.Keys;

    public TValue CurrentMainValueType => storedValues.MaxBy(x => x.Value).Key;
    
    //Stack Cache
    public DefValueStack<TValue> ValueStack { get; set; }
    public List<TValue> AcceptedTypes { get; }
    
    //
    #region Value Getters

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual float CapacityOf(TValue def)
    {
        return Capacity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float StoredValueOf(TValue def)
    {
        if (def == null) return 0;
        return storedValues.GetValueOrDefault(def, 0f);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float TotalStoredOfMany(IEnumerable<TValue> defs)
    {
        return defs.Sum(StoredValueOf);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float StoredPercentOf(TValue def)
    {
        return StoredValueOf(def) / Mathf.Ceil(CapacityOf(def));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsFull(TValue def)
    {
        if (def.sharesCapacity) return StoredValueOf(def) >= CapacityOf(def);
        return FillState == ContainerFillState.Full;
    }

    #endregion
    
    //
    #region Constructors

    public ValueContainerBase(ContainerConfig config)
    {
        _config = config;
        capacity = config.baseCapacity;
        
        //Cache Types
        AcceptedTypes = new List<TValue>((IEnumerable<TValue>) config.valueDefs);
    }

    #endregion
    
    public void ExposeData()
    {
        Scribe_Collections.Look(ref filterSettings, "typeFilter");
        Scribe_Collections.Look(ref storedValues, "storedValues");
        
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            Notify_ContainerStateChanged(true);
        }
    }

    #region Helper Methods

    //
    public float GetMaxTransferRate(TValue valueDef, float desiredValue)
    {
        return Mathf.Clamp(desiredValue, 0, CapacityOf(valueDef) - StoredValueOf(valueDef));
    }
    
    //
    public virtual Color ColorFor(TValue def)
    {
        return Color.white;
    }

    //
    protected virtual bool CanAddValue(TValue valueType, float wantedValue)
    {
        return true;
    }
    
    protected virtual bool CanRemoveValue(TValue valueType, float wantedValue)
    {
        return true;
    }
    
    //Filter Settings
    /// <summary>
    /// Checks the <see cref="FlowValueFilterSettings.canReceive"/> boolean.
    /// Determines whether or not this value can be received during a transaction./>
    /// </summary>
    public virtual bool CanReceiveValue(TValue valueType)
    {
        return filterSettings.TryGetValue(valueType, out var settings) && settings.canReceive;
    }
    
    /// <summary>
    /// Checks the <see cref="FlowValueFilterSettings.canStore"/> boolean.
    /// Determines whether or not this value needs to be purged from this container./>
    /// </summary>
    public virtual bool CanHoldValue(TValue valueType)
    {
        return filterSettings.TryGetValue(valueType, out var settings) && settings.canStore;
    }

    public bool CanResolveTransfer(ValueContainerBase<TValue> other, TValue type, float value, out float actualTransfer)
    {
        var remainingCapacity = type.sharesCapacity
            ? other.CapacityOf(type) - other.StoredValueOf(type)
            : other.Capacity - other.TotalStored;

        actualTransfer = Mathf.Min(value, remainingCapacity);

        return actualTransfer > 0;
    }
    
    public FlowValueFilterSettings GetFilterFor(TValue value)
    {
        return filterSettings[value];
    }

    public void SetFilterFor(TValue value, FlowValueFilterSettings filter)
    {
        filterSettings[value] = filter;
    }

    public Dictionary<TValue, FlowValueFilterSettings> GetFilterCopy() => filterSettings.Copy();

    #endregion

    #region State Notifiers

    public virtual void Notify_AddedValue(TValue valueType, float value)
    {
        totalStoredCache += value;

        //Update stack state
        Notify_ContainerStateChanged();
    }

    public virtual void Notify_RemovedValue(TValue valueType, float value)
    {
        totalStoredCache -= value;
        if (storedValues[valueType] <= 0)
        {
            storedValues.Remove(valueType);
        }

        //Update stack state
        Notify_ContainerStateChanged();
    }

    public virtual void Notify_ContainerStateChanged(bool updateMetaData = false)
    {
        //Cache previous stack
        var previous = ValueStack;
        
        //Set New Values Onto Stack
        ValueStack = new DefValueStack<TValue>(storedValues);

        //Get Delta
        var stackDelta = previous - ValueStack;
        
        //Update metadata
        if (updateMetaData)
        {
            totalStoredCache = ValueStack.TotalValue;
        }

        //
        colorInt = Color.clear;
        if (storedValues.Count > 0)
        {
            foreach (var value in storedValues)
            {
                colorInt += ColorFor(value.Key) * (value.Value / Capacity);
            }
        }

        //TODO: Add In Inherited Holder
        //Parent?.Notify_ContainerStateChanged(new NotifyContainerChangedArgs<TValue>(stackDelta, ValueStack));
    }

    #endregion
    
    #region Data Handling

    /// <summary>
    /// Clears all values inside the container.
    /// </summary>
    public void Clear()
    {
        foreach (TValue def in storedValues.Keys)
        {
            TryRemoveValue(def, storedValues[def], out _);
        }
    }

    /// <summary>
    /// Fills the container equally until reaching a desired capacity.
    /// </summary>
    /// <param name="toCapacity"></param>
    public void Fill(float toCapacity)
    {
        float totalValue = toCapacity - TotalStored;
        float valuePerType = totalValue / AcceptedTypes.Count;
    
        foreach (TValue def in AcceptedTypes)
        {
            TryAddValue(def, valuePerType, out _);
        }
    }

    public void ChangeCapacity(int newCapacity)
    {
        capacity = newCapacity;
    }

    public void LoadFromStack(DefValueStack<TValue> stack)
    {
        Clear();
        foreach (var def in stack)
        {
            TryAddValue(def.Def, def.Value, out _);
        }
    }

    public virtual TCopy Copy<TCopy>(ContainerConfig configOverride = null)
    where TCopy : ValueContainerBase<TValue>
    {
        var newContainer = (TCopy) Activator.CreateInstance(typeof(TCopy), _config);
        newContainer.LoadFromStack(ValueStack);
        
        //Copy Settings
        newContainer.filterSettings = filterSettings;

        return newContainer;
    }

    public void CopyTo(ValueContainerBase<TValue> other)
    {
        other.LoadFromStack(ValueStack);
    }

    #endregion
    
    #region Processor Methods

    public bool TryAddValue(TValue valueType, float wantedValue, out float actualValue)
    {
        actualValue = 0;
        
        if (!CanAddValue(valueType, wantedValue))
        {
            return false;
        }
        
        // If the container is full or doesn't accept the type, we don't add anything
        if (IsFull(valueType) || !CanReceiveValue(valueType))
        {
            return false;
        }

        // Calculate excess value if we add more than we can contain
        var excessValue = Mathf.Max(TotalStored + wantedValue - Capacity, 0);

        // If we cannot add the full wanted value, adjust it to fit within the available capacity
        if (wantedValue - excessValue > 0 && excessValue > 0)
        {
            wantedValue -= excessValue;
        }

        // If there's no wanted value left, return false
        if (wantedValue <= 0)
        {
            return false;
        }

        // Add the actual value to the stored values dictionary
        actualValue = wantedValue;
        if (storedValues.TryGetValue(valueType, out var currentValue))
        {
            storedValues[valueType] = currentValue + actualValue;
        }
        else
        {
            storedValues.Add(valueType, actualValue);
        }

        // Notify that a value has been added
        Notify_AddedValue(valueType, actualValue);
        return true;
    }

    public bool TryRemoveValue(TValue valueType, float wantedValue, out float actualValue)
    {
        if (!CanRemoveValue(valueType, wantedValue) || 
            !storedValues.TryGetValue(valueType, out float storedValue))
        {
            actualValue = 0;
            return false;
        }

        actualValue = Mathf.Min(storedValue, wantedValue); // Actual removed value can't exceed the stored value
    
        storedValues[valueType] -= actualValue;
    
        if (storedValues[valueType] <= 0)
        {
            storedValues.Remove(valueType);
        }
    
        Notify_RemovedValue(valueType, actualValue);
        return true;
        
        /*
        actualValue = wantedValue;
        if (_storedValues.TryGetValue(valueType, out float value) && value > 0)
        {
            if (value >= wantedValue)
                //If we have stored more than we need to pay, remove the wanted weight
                _storedValues[valueType] -= wantedValue;
            else if (value > 0)
            {
                //If not enough stored to "pay" the wanted weight, remove the existing weight and set actual removed weight to removed weight 
                _storedValues[valueType] = 0;
                actualValue = value;
            }
            
            if (_storedValues[valueType] <= 0)
            {
                _storedValues.Remove(valueType);
            }
            Notify_RemovedValue(valueType, actualValue);
        }
        return actualValue > 0;
        */
    }
    
    public bool TryTransferTo(ValueContainerBase<TValue> other, TValue valueType, float value, out float actualTransferedValue)
    {
        //Attempt to transfer a weight to another container
        //Check if anything of that type is stored, check if transfer of weight is possible without loss, try remove the weight from this container
        actualTransferedValue = 0;

        if (other == null) return false;
        if (!other.CanReceiveValue(valueType)) return false;

        if (CanResolveTransfer(other, valueType, value, out var possibleTransfer))
        {
            if (TryRemoveValue(valueType, possibleTransfer, out float actualValue))
            {
                //If passed, try to add the actual weight removed from this container, to the other.
                return other.TryAddValue(valueType, actualValue, out actualTransferedValue);;
            }

        }
        return false;
    }

    public enum ValueMode
    {
        Equal,
        TakeFirst
    }

    //TODO: Implement way to dynamically process any request function with different value approaches
    private void Processor(Action<TValue, float> action,ValueMode mode)
    {
        switch (mode)
        {
            case ValueMode.Equal:
                break;
            case ValueMode.TakeFirst:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }
        
        
    }
    
    //TODO: Assume equal for now
    public bool TryConsume(float wantedValue)
    {
        if (TotalStored >= wantedValue)
        {
            var allTypes = AllStoredTypes;
            var equalSplit = wantedValue/allTypes.Count;
            foreach (var type in allTypes)
            {
                if (wantedValue > 0f && TryRemoveValue(type, equalSplit, out float leftOver))
                {
                    wantedValue = leftOver;
                }
            }
            return true;
        }
        return false;
    }

    public bool TryConsume(TValue valueDef, float wantedValue)
    {
        if (StoredValueOf(valueDef) >= wantedValue)
        {
            return TryRemoveValue(valueDef, wantedValue, out float leftOver);
        }
        return false;
    }
    
    
    #endregion
    
    public virtual IEnumerable<Thing> GetThingDrops()
    {
        yield break;
    }

    public virtual IEnumerable<Gizmo> GetGizmos()
    {
        yield break;
    }
}

//Container Template implementing IContainerHolder
public abstract class ValueContainer<TValue, THolder> : ValueContainerBase<TValue>
    where TValue : FlowValueDef
    where THolder : IContainerHolderBase<TValue>
{
    public THolder Holder { get; }

    protected ValueContainer(ContainerConfig config, THolder holder) : base(config)
    {
        Holder = holder;
    }
}

public abstract class ValueContainerThing<TValue, THolder> : ValueContainer<TValue, THolder>
    where TValue : FlowValueDef
    where THolder : IContainerHolderThing<TValue>
{
    //Cache
    private Gizmo_ContainerStorage<TValue, ValueContainerThing<TValue, THolder>> containerGizmoInt = null;


    public Gizmo_ContainerStorage<TValue, ValueContainerThing<TValue, THolder>> ContainerGizmo
    {
        get
        {
            return containerGizmoInt ??= new Gizmo_ContainerStorage<TValue, ValueContainerThing<TValue, THolder>>(this);
        }
    }
    
    public Thing ParentThing => Holder.Thing;
    
    protected ValueContainerThing(ContainerConfig config, THolder holder) : base(config, holder)
    {
    }
    
    public override IEnumerable<Gizmo> GetGizmos()
    {
        if (Capacity <= 0) yield break;


        if (Holder.ShowStorageGizmo)
        {
            if (Find.Selector.NumSelected == 1 && Find.Selector.IsSelected(ParentThing)) yield return ContainerGizmo;
        }
        
        /*
        if (DebugSettings.godMode)
        {
            yield return new Command_Action
            {
                defaultLabel = $"DEBUG: Container Options {Props.maxStorage}",
                icon = TiberiumContent.ContainMode_TripleSwitch,
                action = delegate
                {
                    List<FloatMenuOption> list = new List<FloatMenuOption>();
                    list.Add(new FloatMenuOption("Add ALL", delegate
                    {
                        foreach (var type in AcceptedTypes)
                        {
                            TryAddValue(type, 500, out _);
                        }
                    }));
                    list.Add(new FloatMenuOption("Remove ALL", delegate
                    {
                        foreach (var type in AcceptedTypes)
                        {
                            TryRemoveValue(type, 500, out _);
                        }
                    }));
                    foreach (var type in AcceptedTypes)
                    {
                        list.Add(new FloatMenuOption($"Add {type}", delegate
                        {
                            TryAddValue(type, 500, out var _);
                        }));
                    }
                    FloatMenu menu = new FloatMenu(list, $"Add NetworkValue", true);
                    menu.vanishIfMouseDistant = true;
                    Find.WindowStack.Add(menu);
                }
            };
        }
        */
    }
}
