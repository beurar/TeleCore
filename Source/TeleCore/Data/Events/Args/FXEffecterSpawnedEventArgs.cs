using Verse;

namespace TeleCore.Data.Events;

public struct FXEffecterSpawnedEventArgs
{
    public string effecterTag;
    public FleckDef fleckDef;
    public Mote mote;
}