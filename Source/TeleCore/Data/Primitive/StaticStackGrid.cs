using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Verse;

namespace TeleCore.Primitive;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct StaticValue<TDef, TValue> 
    where TDef : Def
    where TValue : unmanaged
{
    private ushort _defID = 0;
    private Numeric<TValue> _value;
    private Numeric<TValue> _overflow;

    public TDef Def => _defID.ToDef<TDef>();
    public TValue Value => _value.Value;
    
    public static StaticValue<TDef, TValue> Invalid => new (0, Numeric<TValue>.Zero, Numeric<TValue>.Zero);
    public static StaticValue<TDef, TValue> Empty => new (ushort.MaxValue, Numeric<TValue>.Zero, Numeric<TValue>.Zero);
    
    public StaticValue(Def def, TValue value) : this(def.ToID(), value)
    {
    }
    
    public StaticValue(ushort defID, TValue value)
    {
        this._defID = defID;
        this._value = value;
    }
    
    public StaticValue(ushort defID, TValue value, TValue overflow)
    {
        this._defID = defID;
        this._value = value;
        this._overflow = overflow;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 23 + _defID.GetHashCode();
            hash = hash * 23 + _value.GetHashCode();
            hash = hash * 23 + _overflow.GetHashCode();
            return hash;
        }
    }

    public static bool operator ==(StaticValue<TDef, TValue> left, StaticValue<TDef, TValue> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(StaticValue<TDef, TValue> left, StaticValue<TDef, TValue> right)
    {
        return !(left == right);
    }
    
    public override bool Equals(object obj) 
    {
        if(obj is not StaticValue<TDef, TValue> other) return false;
        return _defID == other._defID && _value == other._value && _overflow == other._overflow;
    }
    
    public override string ToString()
    {
        return $"[{_defID.ToDef<TDef>()}]: ({_value}, {_overflow})";
    }
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct StaticStack<TDef, TValue> where TValue : unmanaged where TDef : Def
{
    private StaticValue<TDef, TValue>* stackPtr;
    private NativeArray<StaticValue<TDef, TValue>> stackData;
    private Numeric<TValue> totalValue;
    
    public bool HasAnyValue => totalValue > Numeric<TValue>.Zero;
    public int Length => stackData.Length;
    
    public StaticValue<TDef, TValue> this[int idx]
    {
        get
        {
            if(idx > 0 || idx < stackData.Length)
                return stackPtr[idx];
            TLog.Warning($"Index out of bounds: {idx} [0...{Length}]");
            return StaticValue<TDef, TValue>.Empty;
        }
        //Main Setting Operation
        set
        {
            if (idx <= 0 && idx >= stackData.Length) return;
            AdjustTotalValue(stackPtr[idx].Value, value.Value);
            stackPtr[idx] = value;
        }
    }
    
    public StaticStack()
    {
        var allDefs = DefDatabase<TDef>.AllDefsListForReading;
        stackData = new NativeArray<StaticValue<TDef, TValue>>(allDefs.Count, Allocator.Persistent);
        stackPtr = (StaticValue<TDef, TValue>*) stackData.GetUnsafePtr();
        totalValue = Numeric<TValue>.Zero;
        
        for (var i = 0; i < allDefs.Count; i++)
        {
            stackPtr[i] = new StaticValue<TDef, TValue>(allDefs[i], Numeric<TValue>.Zero);
        }
    }
    
    private void AdjustTotalValue(Numeric<TValue> previousValue, Numeric<TValue> newValue)
    {
        totalValue = (TValue)(totalValue + (newValue - previousValue));
    }
}

public unsafe class StaticStackGrid<TDef, TValue> 
    where TValue : unmanaged 
    where TDef : Def
{
    private int _gridSize;
    private TDef[] _defArray;
    private int _defCount;
    
    private readonly NativeArray<StaticStack<TDef, TValue>> _grid;
    private readonly StaticStack<TDef, TValue>* _gridPtr;
    
    public StaticStackGrid(Map map)
    {
        _gridSize = map.cellIndices.NumGridCells;
        _grid = new NativeArray<StaticStack<TDef, TValue>>(_gridSize, Allocator.Persistent); // new GasCellStack[gridSize];
        _gridPtr = (StaticStack<TDef, TValue>*)_grid.GetUnsafePtr();
        
        if (_defArray == null)
        {
            _defArray = DefDatabase<TDef>.AllDefsListForReading.ToArray();
            _defCount = _defArray.Length;
        }
        
        for (var c = 0; c < _gridSize; c++)
        {
            _gridPtr[c] = new StaticStack<TDef, TValue>();
        }
    }
}