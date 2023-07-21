using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Verse;

namespace TeleCore.Primitive;

public unsafe struct StaticStack<TDef, TValue> where TValue : struct where TDef : Def
{
    private DefValue<TDef, TValue>* stackPtr;
    private NativeArray<DefValue<TDef, TValue>> stackData;
    private Numeric<TValue> totalValue;
    
    public StaticStack()
    {
        var allDefs = DefDatabase<TDef>.AllDefsListForReading;
        stackData = new NativeArray<DefValue<TDef, TValue>>(allDefs.Count, Allocator.Persistent);
        stackPtr = (DefValue<TDef, TValue>*) stackData.GetUnsafePtr();
        totalValue = Numeric<TValue>.Zero;
        
        for (var i = 0; i < allDefs.Count; i++)
        {
            stackPtr[i] = new DefValue<TDef, TValue>(allDefs[i], Numeric<TValue>.Zero);
        }
    }
}

public unsafe class StaticStackGrid<TDef, TValue> where TValue : struct where TDef : Def
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