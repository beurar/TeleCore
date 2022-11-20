using Verse;

namespace TeleCore;

public class GraphicAlternateWorker
{
    public virtual bool NeedsAlt(Rot4 rot, Thing thing)
    {
        return false;
    }
}

public class GraphicAlternateWorker_WallOnSouth : GraphicAlternateWorker
{
    public override bool NeedsAlt(Rot4 rot, Thing thing)
    {
        if (rot == Rot4.East || rot == Rot4.West)
        {
            var checkPos = thing.Position + Rot4.South.FacingCell;
            if (checkPos.GetEdifice(thing.Map) != null)
            {
                return true;
            }
        }

        return false;
    }
}