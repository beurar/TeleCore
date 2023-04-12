using System;
using Verse;

namespace TeleCore.Data.Events;

public class TerrainChangedEventArgs : EventArgs
{
    public IntVec3 Position { get; }
    
    public TerrainDef PreviousTerrain { get; }
    public TerrainDef NewTerrain { get; }
    
    public bool IsSubTerrain { get; }
    
    public TerrainChangedEventArgs(IntVec3 pos, bool isSubTerrain, TerrainDef previous, TerrainDef terrain)
    {
        Position = pos;
        PreviousTerrain = previous;
        NewTerrain = terrain;
        IsSubTerrain = isSubTerrain;
    }
}