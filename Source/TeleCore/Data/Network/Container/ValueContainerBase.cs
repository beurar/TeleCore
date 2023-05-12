using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using TeleCore.FlowCore;
using UnityEngine;
using Verse;

namespace TeleCore;

//Base Container Template for Values
public abstract class ValueContainerBase<TValue> : IExposable where TValue : FlowValueDef
{
    //
    private readonly ContainerConfig<TValue> config;
    
    //Dynamic settings
    private float capacity;
    
    //Dynamic Data
    private Color colorInt;
    private float totalStoredCache;
    private Dictionary<TValue, float> storedValues = new();
    private ContainerValueFilter<TValue> filter;

    //
    public virtual string Label => config.containerLabel;
    public Color Color => colorInt;
    
    //Capacity Values
    public float Capacity => capacity;
    public float TotalStored => totalStoredCache;
    public float StoredPercent => TotalStored / Capacity;

    public ContainerConfig<TValue> Config => config;
    
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

    public bool Full => FillState == ContainerFillState.Full;
    public bool Empty => FillState == ContainerFillState.Empty;
    
    public bool ContainsForbiddenType => StoredDefs.Any(t => !CanHoldValue(t));

    //Value Stuff
    public Dictionary<TValue, float> StoredValuesByType => storedValues;
    public ICollection<TValue> StoredDefs => storedValues.Keys;

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

    public ValueContainerBase(ContainerConfig<TValue> config)
    {
        this.config = config;
        capacity = config.baseCapacity;

        if (config.valueDefs == null)
        {
            TLog.Warning($"[{GetType()}.ctor]Could not correctly instantiate- Missing ValueDefs. State:\n{this}");
            return;
        }

        //Cache Types
        AcceptedTypes = new List<TValue>(config.AllowedValues);
        
        //Setup Filter
        filter = new ContainerValueFilter<TValue>(AcceptedTypes, config.defaultFilterSettings);

        ValueStack = new DefValueStack<TValue>(AcceptedTypes, capacity);
    }

    #endregion

    public void ExposeData()
    {
        if (Scribe_Container.InvalidState && Scribe.mode is LoadSaveMode.Saving or LoadSaveMode.LoadingVars)
        {
            TLog.Error($"{this} should be scribed with {typeof(Scribe_Container)}!\nScribe mode used: {Scribe.mode}");
        }
        
        Scribe_Deep.Look(ref filter, "filter");
        Scribe_Collections.Look(ref storedValues, "storedValues");
        
        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            OnContainerStateChanged(true);
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
        return def.valueColor;
    }

    
    /// <summary>
    /// Provides an extra condition to check against when trying to add a value.
    /// </summary>
    protected virtual bool CanAddValue(DefFloat<TValue> value)
    {
        return true;
    }
    
    /// <summary>
    /// Provides an extra condition to check against when trying to remove a value.
    /// </summary>
    protected virtual bool CanRemoveValue(DefFloat<TValue> value)
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
        return filter.CanReceive(valueType);
    }
    
    /// <summary>
    /// Checks the <see cref="FlowValueFilterSettings.canStore"/> boolean.
    /// Determines whether or not this value needs to be purged from this container./>
    /// </summary>
    public virtual bool CanHoldValue(TValue valueType)
    {
        return filter.CanStore(valueType);
    }
    
    public bool CanTransferAmountTo(ValueContainerBase<TValue> other, float amount)
    {
        return other.TotalStored + amount <= other.Capacity;
    }
    
    public bool CanTransferAmountTo(ValueContainerBase<TValue> other, TValue valueDef, float amount)
    {
        if (storedValues.TryGetValue(valueDef) < amount) return false;
        return other.StoredValueOf(valueDef) + amount <= other.CapacityOf(valueDef);
    }

    
    private bool CanResolveTransfer(ValueContainerBase<TValue> other, TValue type, float amount, out float actualTransfer)
    {
        var remainingCapacity = type.sharesCapacity
            ? other.CapacityOf(type) - other.StoredValueOf(type)
            : other.Capacity - other.TotalStored;

        actualTransfer = Mathf.Min(amount, remainingCapacity);

        return actualTransfer > 0;
    }

    public ContainerValueFilter<TValue> Filter => filter;
    
    public FlowValueFilterSettings GetFilterFor(TValue value)
    {
        return filter.SettingsFor(value);
    }

    public void SetFilterFor(TValue value, FlowValueFilterSettings filter)
    {
        this.filter.SetSettings(value, filter);
    }

    public ContainerValueFilter<TValue> GetFilterCopy() => filter.Copy();

    #endregion

    #region State Notifiers

    public virtual void Notify_AddedValue(TValue valueType, float value)
    {
        totalStoredCache += value;

        //Update stack state
        OnContainerStateChanged();
    }

    public virtual void Notify_RemovedValue(TValue valueType, float value)
    {
        totalStoredCache -= value;

        //Update stack state
        OnContainerStateChanged();
    }
    
    public abstract void Notify_ContainerStateChanged(NotifyContainerChangedArgs<TValue> stateChangeArgs);

    /// <summary>
    /// Internal container state logic notifier.
    /// </summary>
    private void OnContainerStateChanged(bool updateMetaData = false)
    {
        //Cache previous stack
        var previous = ValueStack;
        
        ValueStack = new DefValueStack<TValue>(storedValues); //Set new stack
        var stackDelta = ValueStack - previous; //Get stack delta
        
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

        Notify_ContainerStateChanged(new NotifyContainerChangedArgs<TValue>(stackDelta, ValueStack));
    }

    #endregion
    
    #region Data Handling

    /// <summary>
    /// Clears all values inside the container.
    /// </summary>
    public void Clear()
    {
        foreach (var def in storedValues.Keys.ToArray())
        {
            _ = TryRemoveValue(def, storedValues[def]);
        }
    }

    /// <summary>
    /// Fills the container evenly until reaching a desired capacity.
    /// </summary>
    public void Fill(float toCapacity)
    {
        float totalValue = toCapacity - TotalStored;
        float valuePerType = totalValue / AcceptedTypes.Count;
    
        foreach (TValue def in AcceptedTypes)
        {
            _ = TryAddValue(def, valuePerType);
        }
    }

    /// <summary>
    /// Sets a new capacity value, overwriting the <see cref="Config"/> capacity.
    /// </summary>
    public void ChangeCapacity(int newCapacity)
    {
        capacity = newCapacity;
    }

    /// <summary>
    /// Sets the container values to be sourced by a stack input.
    /// </summary>
    /// <param name="stack">Stack to provide values for the container.</param>
    public void LoadFromStack(DefValueStack<TValue> stack)
    {
        Clear();
        foreach (var def in stack)
        {
            _ = TryAddValue(def.Def, def.Value);
        }
    }

    public virtual TCopy Copy<TCopy>(ContainerConfig<TValue> configOverride = null)
    where TCopy : ValueContainerBase<TValue>
    {
        var newContainer = (TCopy) Activator.CreateInstance(typeof(TCopy), config);
        newContainer.LoadFromStack(ValueStack);
        
        //Copy Settings
        newContainer.filter = filter;

        return newContainer;
    }

    public void CopyTo(ValueContainerBase<TValue> other)
    {
        other.LoadFromStack(ValueStack);
    }
    
    #endregion
    
    #region Processor Methods

    public bool TryAddValue(TValue valueDef, float amount, out ValueResult<TValue> result)
    {
        return result = TryAddValue((valueDef, amount));
    }
    
    public ValueResult<TValue> TryAddValue(TValue valueDef, float amount)
    {
        return TryAddValue((valueDef, amount));
    }
    
    public ValueResult<TValue> TryAddValue(DefFloat<TValue> value)
    {
        var desired = value.Value; //Local cache for desired value
        var result = ValueResult<TValue>.Init(desired, AcceptedTypes); //ValueResult Init

        //Lazy sanity checks for failure
        if (!CanAddValue(value) || IsFull(value) || !CanReceiveValue(value))
        {
            return result.Fail();
        }

        //Calculate excess and adjust our actual possible addable value
        var excessValue = Mathf.Max(TotalStored + desired - Capacity, 0);
        var actual = desired - excessValue;

        //Fail if resulting actual value is <= 0
        if (actual <= 0)
        {
            return result.Fail();
        }

        //Otherwise continue to add the value
        if (storedValues.TryGetValue(value, out var currentValue))
        {
            storedValues[value] = currentValue + actual;
            result.AddDiff(value, actual);
        }
        else
        {
            storedValues.Add(value, actual);
        }

        Notify_AddedValue(value, actual); //Notify internal logic updates
        
        //On the result, set actual added value and resolve completion status
        return result.SetActual(actual).Complete().Resolve();
    }
    
    [Obsolete]
    public ValueResult<TValue> TryAddValueDeprecated(DefFloat<TValue> value)
    {
        float desired = value.Value;
        float actual = 0;
        var result = ValueResult<TValue>.Init(desired,AcceptedTypes);

        //If we cant add the value, the operation fails
        if (!CanAddValue(value))
        {
            return result.Fail();
        }
        
        // If the container is full or doesn't accept the type, we don't add anything
        if (IsFull(value) || !CanReceiveValue(value))
        {
            return result.Fail(); //ValueResult<TValue>.Failed(desired);
        }

        // Calculate excess value if we add more than we can contain
        var excessValue = Mathf.Max(TotalStored + desired - Capacity, 0);

        // If we cannot add the full wanted value, adjust it to fit within the available capacity
        actual = value.Value - excessValue;
        if (desired - excessValue > 0 && excessValue > 0)
        {
            actual = desired - excessValue;
        }

        // If the excess is equivalent to the desired amount - we cannot add more and thus quit
        if (desired <= 0)
        {
            //TODO: This case technically should never happen due to the IsFull check
            return result.Fail();
        }

        // Add the actual value to the stored values dictionary
        if (storedValues.TryGetValue(value, out var currentValue))
        {
            storedValues[value] = currentValue + actual;
            result.AddDiff(value, actual);
        }
        else
        {
            storedValues.Add(value, actual);
        }

        // Notify that a value has been added
        Notify_AddedValue(value, actual);
        return result.AddDiff(value, actual).SetActual(actual).Complete().Resolve();
    }
    
    public bool TryRemoveValue(TValue valueDef, float amount, out ValueResult<TValue> result)
    {
        return result = TryRemoveValue((valueDef, amount));
    }

    public ValueResult<TValue> TryRemoveValue(TValue valueDef, float value)
    {
        return TryRemoveValue((valueDef, value));
    }

    public ValueResult<TValue> TryRemoveValue(DefFloat<TValue> value)
    {
        var desired = value.Value;
        var result = ValueResult<TValue>.Init(desired,AcceptedTypes);

        //Lazy sanity checks for failure
        if (!CanRemoveValue(value) || !storedValues.TryGetValue(value, out var available))
        {
            return result.Fail();
        }

        //Calculate the actual removeable value
        var actual = Mathf.Min(available, desired);
        
        //Remove the value from the dictionary or update the value if there is still some left
        if (available - actual <= 0)
        {
            storedValues.Remove(value.Def);
        }
        else
        {
            storedValues[value] -= actual;
        }

        //Notify internal cached data
        Notify_RemovedValue(value, actual);

        //On the result, set actual removed value and resolve completion status
        return result.AddDiff(value, -actual).SetActual(actual).Complete().Resolve();

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
    
    //What are settings on a container value operation?
    //Manipulation Kind - Add, Remove -- extended to --> Transfer (remove from and add to) // Consume (remove desired amount) // Clear (remove all) //
    //Type value selection - Equal/Even (same amount for each desired type); First Available (take any of the first available)

    /// <summary>
    /// Attempts to transfer the desired value and amount to another container, returns how much was transfered
    /// </summary>
    public bool TryTransferValue(ValueContainerBase<TValue> other, TValue valueDef, float amount, out ValueResult<TValue> result)
    {
        //Attempt to transfer a weight to another container
        //Check if anything of that type is stored, check if transfer of weight is possible without loss, try remove the weight from this container
        result = ValueResult<TValue>.InitFail(amount);
        
        
        if (other == null) return false;
        if (!other.CanReceiveValue(valueDef)) return false;

        if (CanResolveTransfer(other, valueDef, amount, out var possibleTransfer))
        {
            var remResult = TryRemoveValue(valueDef, possibleTransfer);
            if (remResult)
            {
                //If passed, try to add the actual weight removed from this container, to the other.
                return  result = other.TryAddValue(valueDef, remResult.ActualAmount);
            }
        }

        return result;
    }

    /// <summary>
    /// Attempts to transfer any held value to the other container, split evenly.
    /// </summary>
    public ValueResult<TValue> TryTransferTo(ValueContainerBase<TValue> other, float amount, out DefValueStack<TValue> transferedDiff)
    {
        //Even split for each value
        var evenAmount = amount / StoredDefs.Count;
        transferedDiff = new DefValueStack<TValue>(StoredDefs);
        foreach (var def in StoredDefs)
        {
            if (TryTransferValue(other, def, evenAmount, out var tempResult))
            {
                transferedDiff += (def, tempResult.ActualAmount);
            }
            else
            {
                //If we cannot transfer, we break and check our current state
                break;
            }
        }

        //Resolve result state
        return transferedDiff switch
        {
            var n when n == 0 => ValueResult<TValue>.InitFail(amount),
            var n when n > 0 => ValueResult<TValue>.Init(amount, AcceptedTypes).Complete(transferedDiff.TotalValue),
            var n when n == amount => ValueResult<TValue>.Init(amount, AcceptedTypes).Complete(amount),
        };
    }

    /// <summary>
    /// Attempts to consume each given value.
    /// </summary>
    public bool TryConsume(IEnumerable<DefFloat<TValue>> values)
    {
        foreach (var value in values)
        {
            if (TryConsume(value))
            {
                
            }
        }
        return true;
    }
    
    /// <summary>
    /// Consumes a set amount, using any value from the container.
    /// </summary>
    public ValueResult<TValue> TryConsume(float wantedValue)
    {
        if (TotalStored >= wantedValue)
        {
            var allTypes = StoredDefs;
            var equalSplit = wantedValue/allTypes.Count;
            float actualConsumed = 0;
            foreach (var type in allTypes)
            {
                var remResult = TryRemoveValue(type, equalSplit);
                if (actualConsumed < wantedValue && remResult)
                {
                    actualConsumed += equalSplit - remResult.ActualAmount;
                    wantedValue = remResult.ActualAmount;
                }
            }
            
            //Resolve result state
            return wantedValue switch
            {
                0 => ValueResult<TValue>.InitFail(wantedValue),
                > 0 => ValueResult<TValue>.Init(wantedValue,AcceptedTypes).Complete(actualConsumed),
                var n when n == wantedValue => ValueResult<TValue>.Init(wantedValue, AcceptedTypes).Complete(actualConsumed),
            };
        }
        return ValueResult<TValue>.InitFail(wantedValue);
    }

    public ValueResult<TValue> TryConsume(TValue def, float amount)
    {
        return TryConsume((def, amount));
    }

    /// <summary>
    /// Consumes a fixed given value.
    /// </summary>
    public ValueResult<TValue> TryConsume(DefFloat<TValue> value)
    {
        if (StoredValueOf(value) >= (float)value)
        {
            return TryRemoveValue(value);
        }
        return ValueResult<TValue>.InitFail((float)value);
    }

    #endregion

    #region DEBUG

            
    private List<FloatMenuOption> _debugFloatMenuOptions;

    public List<FloatMenuOption> DebugFloatMenuOptions
    {
        get
        {
            if (_debugFloatMenuOptions == null)
            {
                _debugFloatMenuOptions = new List<FloatMenuOption>();
                float part = Capacity / AcceptedTypes.Count;
                _debugFloatMenuOptions.Add(new FloatMenuOption("Add ALL", delegate { Debug_AddAll(part); }));

                _debugFloatMenuOptions.Add(new FloatMenuOption("Remove ALL", Debug_Clear));

                foreach (var type in AcceptedTypes)
                {
                    _debugFloatMenuOptions.Add(new FloatMenuOption($"Add {type}", delegate { Debug_AddType(type, part); }));
                }
            }
            return _debugFloatMenuOptions;
        }
    }
    
    //TODO: cant be in generic class [SyncMethod]
    private void Debug_AddAll(float part)
    {
        foreach (var type in AcceptedTypes)
        {
            TryAddValue(type, part);
        }
    }

    private void Debug_Clear()
    {
        Clear();
    }
    
    private void Debug_AddType(FlowValueDef type, float part)
    {
        TryAddValue((TValue) type, part);
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

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Capacity: {Capacity}");
        sb.AppendLine($"ValueStack:\n{ValueStack}");
        sb.AppendLine($"StoredDefs: {StoredDefs.ToStringSafeEnumerable()}");
        sb.AppendLine($"StoredValues: {StoredValuesByType.ToStringSafeEnumerable()}");
        sb.AppendLine($"FillSate: {FillState}");
        return sb.ToString();
    }
}