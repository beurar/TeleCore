using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TeleCore;

public class FactionDefExtension : DefModExtension
{
    public List<DefValueLoadable<FactionDef, int>> enemyTo;
}