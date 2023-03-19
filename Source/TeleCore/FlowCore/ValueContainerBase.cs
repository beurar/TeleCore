using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Verse;

namespace TeleCore.FlowCore;

public enum ContainerFillState
{
    Full,
    Partial,
    Empty
}

public struct FlowValueFilterSettings : IExposable
{
    public bool canReceive = true;
    public bool canStore = true;
    public bool canTransfer = true;

    public FlowValueFilterSettings()
    {
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref canReceive, nameof(canReceive));
        Scribe_Values.Look(ref canStore, nameof(canStore));
        Scribe_Values.Look(ref canTransfer, nameof(canTransfer));
    }
}

/// <summary>
/// The resulting state of a <see cref="ValueContainerBase{TValue}"/> value-change operation.
/// </summary>
public enum ValueState
{
    Incomplete,
    Completed,
    CompletedWithExcess,
    CompletedWithShortage,
    Failed
}

/// <summary>
/// The result of a <see cref="ValueContainerBase{TValue}"/> Value-Change operation.
/// </summary>
public struct ValueResult<TValue>
where TValue : FlowValueDef
{
    public ValueState State { get; private set; }
    //Initial desire value
    public float DesiredAmount { get; private set; }
    //Actual resulting value
    public float ActualAmount { get; private set; }

    public float LeftOver => DesiredAmount - ActualAmount;
    public float Diff { get; private set; }

    public DefValueStack<TValue> FullDiff { get; private set; }
    
    public static implicit operator bool(ValueResult<TValue> result)
    {
        return result.State != ValueState.Failed;
    }

    public ValueResult()
    {
    }

    public static ValueResult<TValue> InitFail(float desiredAmount)
    {
        return new ValueResult<TValue>
        {
            State = ValueState.Failed,
            DesiredAmount = desiredAmount,
            ActualAmount = 0,
        };
    }

    public static ValueResult<TValue> Init(float desiredAmount, TValue usedDef)
    {
        return new ValueResult<TValue>
        {
            State = ValueState.Incomplete,
            DesiredAmount = desiredAmount,
            ActualAmount = 0,
            FullDiff = new DefValueStack<TValue>(usedDef.ToSingleItemList())
        };
    }

    public static ValueResult<TValue> Init(float desiredAmount, ICollection<TValue> usedDefs)
    {
        return new ValueResult<TValue>
        {
            State = ValueState.Incomplete,
            DesiredAmount = desiredAmount,
            ActualAmount = 0,
            FullDiff = new DefValueStack<TValue>(usedDefs)
        };
    }
    
    public ValueResult<TValue> AddDiff(TValue def, float diffAmount)
    {
        FullDiff += (def, diffAmount);
        Diff += diffAmount;
        return this;
    }
    
    public ValueResult<TValue> SetActual(float actual)
    {
        ActualAmount = actual;
        return this;
    }
    
    public ValueResult<TValue> Fail()
    {
        State = ValueState.Failed;
        return this;
    }

    public ValueResult<TValue> Complete(float? finalActual = null)
    {
        State = ValueState.Completed;
        ActualAmount = finalActual ?? ActualAmount;
        return this;
    }

    public ValueResult<TValue> Resolve()
    {
        if (Math.Abs(ActualAmount - DesiredAmount) < Mathf.Epsilon)
            State = ValueState.Completed;
        if (ActualAmount > DesiredAmount)
            State = ValueState.CompletedWithExcess;
        if (ActualAmount < DesiredAmount)
            State = ValueState.CompletedWithShortage;
        return this;
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append($"State: {State} | ");
        sb.Append($"DesiredToActual: {DesiredAmount} -> {ActualAmount} | ");
        sb.Append($"Diff: {Diff}");
        return sb.ToString();
    }
}

//Base Container Template for Values
public abstract class ValueContainerBase<TValue> : IExposable where TValue : FlowValueDef
{
    //
    private readonly ContainerConfig<TValue> _config;
    
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

    public ContainerConfig<TValue> Config => _config;
    
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
        _config = config;
        capacity = config.baseCapacity;

        if (config.valueDefs == null)
        {
            TLog.Warning($"[{GetType()}.ctor]Could not correctly instantiate- Missing ValueDefs. State:\n{this}");
            return;
        }

        //Cache Types
        AcceptedTypes = new List<TValue>(config.AllowedValues);
        
        //Setup Filter
        foreach (var value in AcceptedTypes)
        {
            filterSettings.Add(value, new FlowValueFilterSettings());
        }

        ValueStack = new DefValueStack<TValue>(AcceptedTypes, capacity);
    }

    #endregion

    public void ExposeData()
    {
        if (Scribe_Container.InvalidState && Scribe.mode is LoadSaveMode.Saving or LoadSaveMode.LoadingVars)
        {
            TLog.Error($"{this} should be scribed with {typeof(Scribe_Container)}!\nScribe mode used: {Scribe.mode}");
        }
        
        Scribe_Collections.Look(ref filterSettings, "typeFilter", LookMode.Def, LookMode.Deep);
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
        return Color.white;
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

//Container Template implementing IContainerHolder
public abstract class ValueContainer<TValue, THolder> : ValueContainerBase<TValue>
    where TValue : FlowValueDef
    where THolder : IContainerHolderBase<TValue>
{
    public THolder Holder { get; }

    protected ValueContainer(ContainerConfig<TValue> config, THolder holder) : base(config)
    {
        Holder = holder;
    }

    public override void Notify_ContainerStateChanged(NotifyContainerChangedArgs<TValue> stateChangeArgs)
    {
        Holder?.Notify_ContainerStateChanged(stateChangeArgs);
    }
    
}

public class ValueContainerThing<TValue, THolder> : ValueContainer<TValue, THolder>
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
    
    public ValueContainerThing(ContainerConfig<TValue> config, THolder holder) : base(config, holder)
    {
    }
    
    public void Notify_ParentDestroyed(DestroyMode mode, Map previousMap)
    {
        if (Holder == null || TotalStored <= 0 || mode == DestroyMode.Vanish) return;
        OnParentDestroyed(mode, previousMap);
        Clear();
    }

    public virtual void OnParentDestroyed(DestroyMode mode, Map previousMap)
    {
        if (mode is DestroyMode.KillFinalize)
        {
            if (Config.explosionProps != null)
                if (TotalStored > 0)
                    //float radius = Props.explosionProps.explosionRadius * StoredPercent;
                    //int damage = (int)(10 * StoredPercent);
                    //var mainTypeDef = MainValueType.dropThing;
                    Config.explosionProps.DoExplosion(ParentThing.Position, previousMap, ParentThing);
            //GenExplosion.DoExplosion(Parent.Thing.Position, previousMap, radius, DamageDefOf.Bomb, Parent.Thing, damage, 5, null, null, null, null, mainTypeDef, 0.18f);
            if (Config.dropContents)
            {
                var i = 0;
                var drops = GetThingDrops().ToList();
                Predicate<IntVec3> pred = c => c.InBounds(previousMap) && c.GetEdifice(previousMap) == null;
                var action = delegate(IntVec3 c)
                {
                    if (i < drops.Count)
                    {
                        var drop = drops[i];
                        if (drop != null)
                        {
                            GenSpawn.Spawn(drop, c, previousMap);
                            drops.Remove(drop);
                        }

                        i++;
                    }
                };
                _ = TeleFlooder.Flood(previousMap, ParentThing.OccupiedRect(), action, pred, drops.Count);
            }
        }
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
