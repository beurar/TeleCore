using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace TeleCore;

internal enum ContainerMode
{
    ByType,
    ByCapacity
}

public struct ContainerFilterSettings
{
    public bool canReceive = true;
    public bool canStore = true;

    public ContainerFilterSettings()
    {
    }
}

public class BaseContainer<T> : IExposable where T : FlowValueDef
{
    //Local Set Data
    private IContainerHolder<T> _parentHolder = null!;

    //
    private float _capacity;
    private List<T> _acceptedTypes;

    //
    protected Color _colorInt;
    protected float _totalStoredCache;
    protected HashSet<T> _storedTypeCache = new();
    protected Dictionary<T, ContainerFilterSettings> filterSettings = new();
    protected Dictionary<T, float> _storedValues = new();

    public DefValueStack<T> ValueStack { get; protected set; }

    //
    public IContainerHolder<T> Parent => _parentHolder;
    public IContainerHolderThing<T> ThingParent => _parentHolder as IContainerHolderThing<T> ?? null!;
    public IContainerHolderRoom<T> RoomParent => _parentHolder as IContainerHolderRoom<T> ?? null!;
    
    public Thing ParentThing => ThingParent.Thing ?? null!;
    public ContainerProperties? Props => Parent?.ContainerProps;
    public string Title => Parent.ContainerTitle;
    public Color Color => _colorInt;

    //Capacity Values
    public float Capacity => _capacity;
    public float TotalStored => _totalStoredCache;
    public float StoredPercent => TotalStored / Capacity;

    //Capacity States
    public bool NotEmpty => TotalStored > 0;
    public bool Empty => TotalStored <= 0;
    public bool Full => TotalStored >= Capacity;

    //
    public bool ContainsForbiddenType => AllStoredTypes.Any(t => !CanHoldValue(t));

    //
    public bool HasThingParent => _parentHolder is IContainerHolderThing<T>;
    public bool HasNetworkParent => _parentHolder is IContainerHolderNetworkPart<T>;
    public bool HasRoomParent => _parentHolder is IContainerHolderRoom<T>;

    public T MainValueDef => _storedValues.MaxBy(x => x.Value).Key;
    public Dictionary<T, float> StoredValuesByType => _storedValues;
    public Dictionary<T, ContainerFilterSettings> FilterSettings => filterSettings;

    public HashSet<T> AllStoredTypes
    {
        get { return _storedTypeCache ??= new HashSet<T>(); }
    }

    public List<T> AcceptedTypes
    {
        get => _acceptedTypes;
        set => _acceptedTypes = value;
    }

    //Type-Based States
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual float CapacityOf(T def)
    {
        return _capacity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float TotalStoredOf(T def)
    {
        if (def == null) return 0;
        return _storedValues.GetValueOrDefault(def, 0f);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float TotalStoredOfMany(IEnumerable<T> defs)
    {
        return defs.Sum(TotalStoredOf);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float StoredPercentOf(T def)
    {
        return TotalStoredOf(def) / Mathf.Ceil(CapacityOf(def));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsFull(T def)
    {
        if (def.sharesCapacity) return Full;
        return TotalStoredOf(def) >= _capacity;
    }

    #region Constructors
    
    public BaseContainer()
    {
    }

    public BaseContainer(IContainerHolder<T> parent)
    {
        _parentHolder = parent;
        _capacity = Props?.maxStorage ?? 0;
    }

    public BaseContainer(IContainerHolder<T> parent, DefValueStack<T> valueStack)
    {
        _parentHolder = parent;
        _capacity = Props.maxStorage;
        AcceptedTypes = valueStack.AllTypes.ToList();
        foreach (var type in AcceptedTypes)
        {
            FilterSettings.Add(type, new ContainerFilterSettings());
        }

        //
        Data_LoadFromStack(valueStack);
    }

    public BaseContainer(IContainerHolder<T> parent, List<T> acceptedTypes)
    {
        _parentHolder = parent;
        _capacity = Props.maxStorage;
        if (!acceptedTypes.NullOrEmpty())
        {
            AcceptedTypes = acceptedTypes;
            foreach (var type in AcceptedTypes)
            {
                FilterSettings.Add(type, new ContainerFilterSettings());
            }
        }
        else
        {
            TLog.Warning($"Created NetworkContainer for {ParentThing} without any allowed types!");
        }
        //TLog.Message($"Creating new container for {Parent?.Thing} with capacity {Capacity} | acceptedTypes: {this.AcceptedTypes.ToStringSafeEnumerable()}");
    }
    
    #endregion
    
    //
    public virtual void Notify_AddedValue(T valueType, float value)
    {
        _totalStoredCache += value;
        AllStoredTypes.Add(valueType);

        //Update stack state
        UpdateContainerState();
    }

    public virtual void Notify_RemovedValue(T valueType, float value)
    {
        _totalStoredCache -= value;
        if (AllStoredTypes.Contains(valueType) && TotalStoredOf(valueType) <= 0)
            AllStoredTypes.RemoveWhere(v => v == valueType);

        //Update stack state
        UpdateContainerState();
    }
    
    //
    public void Data_Clear()
    {
        for (int i = _storedValues.Count - 1; i >= 0; i--)
        {
            var keyValuePair = _storedValues.ElementAt(i);
            TryRemoveValue(keyValuePair.Key, keyValuePair.Value, out _);
        }

        //
        UpdateContainerState();
    }

    public void Data_Fill(float toCapacity)
    {
        float val = toCapacity / AcceptedTypes.Count;
        foreach (T def in AcceptedTypes)
        {
            TryAddValue(def, val, out float e);
        }
    }

    public void Data_ChangeCapacity(int newCapacity)
    {
        _capacity = newCapacity;
    }

    public void Data_LoadFromStack(DefValueStack<T> stack)
    {
        Data_Clear();
        foreach (var def in stack.values)
        {
            TryAddValue(def.Def, def.Value, out _);
        }
    }

    public void Notify_Full()
    {
        Parent?.Notify_ContainerFull();
    }
    
    public void Notify_FilterChanged(T def, ContainerFilterSettings settings)
    {
        FilterSettings[def] = settings;
    }

    //
    public BaseContainer<T> Copy(IContainerHolder<T> newHolder = null!)
    {
        BaseContainer<T> newContainer = new BaseContainer<T>();
        newContainer._parentHolder = newHolder ?? _parentHolder;
        newContainer._capacity = _capacity;
        newContainer._totalStoredCache = TotalStored;
        newContainer._colorInt = _colorInt;

        //Copy Lists
        _acceptedTypes ??= new List<T>();
        _storedTypeCache ??= new HashSet<T>();
        filterSettings ??= new Dictionary<T, ContainerFilterSettings>();
        _storedValues ??= new Dictionary<T, float>();
        newContainer._acceptedTypes.AddRange(_acceptedTypes);
        newContainer._storedTypeCache.AddRange(_storedTypeCache);
        newContainer.filterSettings.AddRange(filterSettings);
        newContainer._storedValues.AddRange(_storedValues);

        newContainer.ValueStack = ValueStack;

        newContainer.UpdateContainerState(true);
        return newContainer;
    }

    //Value Funcs
    public virtual bool AcceptsValue(T valueType)
    {
        return FilterSettings.TryGetValue(valueType, out var settings) && settings.canReceive;
    }
    
    public virtual bool CanHoldValue(T valueType)
    {
        return FilterSettings.TryGetValue(valueType, out var settings) && settings.canStore;
    }

    public bool CanFullyTransferTo(BaseContainer<T> other, float value)
    {
        return other.TotalStored + value <= other.Capacity;
    }
    public bool CanFullyTransferTo(BaseContainer<T> other, T valueDef, float value)
    {
        //Check Tag Rules
        if (_storedValues.TryGetValue(valueDef) < value) return false;
        return other.TotalStoredOf(valueDef) + value <= other.CapacityOf(valueDef);
    }
    
    public virtual bool CanAccept(T valueDef)
    {
        if (IsFull(valueDef)) return false;
          
        /* TODO: Consider adding displacement for all containers?
        var totalPct = StoredPercentOf(valueDef);
        foreach (var value in _storedValues)
        {
            var valDef = value.Key;
            if (valDef.displaceTags != null && valDef.displaceTags.Contains(valueDef.atmosphericTag))
            {
                var valPct = value.Value / Capacity;
                return (1 - valPct) > totalPct;
            }
        }
        */
        return true;
    }
    
    public bool PotentialCapacityFull(T valueType, float potentialVal, out bool overfilled)
    {
        float val = potentialVal;
        foreach (var type2 in AllStoredTypes)
        {
            if (!type2.Equals(valueType))
            {
                val += _storedValues[type2];
            }
        }
        overfilled = val > Capacity;
        return val >= Capacity;
    }
    
    //
    /* -- Seems redundant
    public float GetMaxTransferRateTo(BaseContainer<T> other, T valueDef, float desiredValue)
    {
        //var maxCap = other.CapacityOf(valueType) - other.TotalStoredOf(valueType);
        return Mathf.Clamp(desiredValue, 0, other.CapacityOf(valueDef) - other.TotalStoredOf(valueDef));
    }
    */

    public float GetMaxTransferRate(T valueDef, float desiredValue)
    {
        return Mathf.Clamp(desiredValue, 0, CapacityOf(valueDef) - TotalStoredOf(valueDef));
    }
    
    // Value Functions
    public bool TryAddValue(T valueType, float wantedValue, out float actualValue)
    {
        if (!ShouldAddValue(valueType, wantedValue))
        {
            actualValue = 0;
            return false;
        }
        
        //If we add more than we can contain, we have an excess weight
        var excessValue = Mathf.Clamp((TotalStored + wantedValue) - Capacity, 0, float.MaxValue);
        //The actual added weight is the wanted weight minus the excess
        actualValue = wantedValue - excessValue;

        //If the container is full, or doesnt accept the type, we dont add anything
        if (IsFull(valueType))
        {
            Notify_Full();
            return false;
        }

        if (!AcceptsValue(valueType)) return false;

        //If the weight type is already stored, add to it, if not, make a new entry
        if (_storedValues.ContainsKey(valueType))
            _storedValues[valueType] += actualValue;
        else
            _storedValues.Add(valueType, actualValue);

        Notify_AddedValue(valueType, actualValue);
        //If this adds the last drop, notify full
        if (IsFull(valueType))
            Notify_Full();

        return true;
    }

    public bool TryRemoveValue(T valueType, float wantedValue, out float actualValue)
    {
        if (!ShouldRemoveValue(valueType, wantedValue))
        {
            actualValue = 0;
            return false;
        }
        
        //Attempt to remove a certain weight from the container
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
    }
    
    //
    protected virtual bool ShouldAddValue(T valueType, float wantedValue)
    {
        return true;
    }
    
    protected virtual bool ShouldRemoveValue(T valueType, float wantedValue)
    {
        return true;
    }

    //
    public void TransferAllTo(BaseContainer<T> otherContainer)
    {
        foreach (var type in AllStoredTypes)
        {
            TryTransferTo(otherContainer, type, TotalStoredOf(type), out _);
        }
    }
    
    public void TryTransferTo(BaseContainer<T> otherContainer, float value)
    {
        for (int i = AllStoredTypes.Count - 1; i >= 0; i--)
        {
            TryTransferTo(otherContainer, AllStoredTypes.ElementAt(i), value, out _);
        }
    }

    public bool TryTransferTo(BaseContainer<T> otherContainer, T valueType, float value, out float actualTransferedValue)
    {
        //Attempt to transfer a weight to another container
        //Check if anything of that type is stored, check if transfer of weight is possible without loss, try remove the weight from this container
        actualTransferedValue = 0;
        if (!otherContainer.AcceptsValue(valueType)) return false;
        if (StoredValuesByType.TryGetValue(valueType) >= value && CanFullyTransferTo(otherContainer, value) && TryRemoveValue(valueType, value, out float actualValue))
        {
            //If passed, try to add the actual weight removed from this container, to the other.
            otherContainer.TryAddValue(valueType, actualValue, out actualTransferedValue);
            return true;
        }
        return false;
    }
    
    public bool TryConsume(float wantedValue)
    {
        if (TotalStored >= wantedValue)
        {
            float value = wantedValue;
            var allTypes = AllStoredTypes.ToArray();
            foreach (T type in allTypes)
            {
                if (value > 0f && TryRemoveValue(type, value, out float leftOver))
                {
                    value = leftOver;
                }
            }
            return true;
        }
        return false;
    }

    public bool TryConsume(T valueDef, float wantedValue)
    {
        if (TotalStoredOf(valueDef) >= wantedValue)
        {
            return TryRemoveValue(valueDef, wantedValue, out float leftOver);
        }
        return false;
    }
    
    //Visual
    public virtual Color ColorFor(T def)
    {
        return Color.white;
    }

    public virtual void UpdateContainerState(bool updateMetaData = false)
    {
        //Set Stack
        ValueStack = new DefValueStack<T>(_storedValues);

        //Update metadata
        if (updateMetaData)
        {
            _totalStoredCache = ValueStack.TotalValue;
            AllStoredTypes.AddRange(ValueStack.AllTypes);
        }

        //
        _colorInt = Color.clear;
        if (_storedValues.Count > 0)
        {
            foreach (var value in _storedValues)
            {
                _colorInt += ColorFor(value.Key) * (value.Value / Capacity);
            }
        }

        //
        Parent?.Notify_ContainerStateChanged();
    }

    //
    public virtual IEnumerable<Thing> Get_ThingDrops()
    {
        yield break;
    }

    //
    public void ExposeData()
    {
        Scribe_Collections.Look(ref filterSettings, "typeFilter");
        Scribe_Collections.Look(ref _storedValues, "storedValues");
        Scribe_Collections.Look(ref _acceptedTypes, "acceptedTypes", LookMode.Def);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            UpdateContainerState(true);
        }
    }

    public virtual IEnumerable<Gizmo> GetGizmos()
    {
        yield break;
    }
}