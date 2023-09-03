using TeleCore.Data.AI.Pathing;
using Verse;

namespace TeleCore.Data.Vehicles;

//A simple thing which can move along a pather
public class MovingThing : ThingWithComps
{
    private Generic_PathFollower _pather;
}