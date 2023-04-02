using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TeleCore;

public class GenericPathFollower : IExposable
{
    private const int MaxMoveTicks = 450;
    private const int MaxCheckAheadNodes = 20;
    private const int MinCostWalk = 50;
    
    //
    private Thing curThing;
    public PawnPath curPath;
    private LocalTargetInfo destination;
    private PathEndMode peMode;
    private TraverseMode traverseMode;
    
    
    //
    private bool moving;

    //
    public IntVec3 nextCell;
    private IntVec3 lastCell;
    public IntVec3 lastPathedTargetPosition;
    
    //
    public float nextCellCostLeft;
    public float nextCellCostTotal = 1f;
    private int lastMovedTick = -999999;
    private static float moveSpeed = 1;

    //
    public Map Map => curThing.Map;

    public LocalTargetInfo Destination => destination;

    public GenericPathFollower(Thing forThing)
    {
	    curThing = forThing;
    }

    public void ExposeData()
    {
	    Scribe_Values.Look(ref moving, "moving", true);
	    Scribe_Values.Look(ref nextCell, "nextCell");
	    Scribe_Values.Look(ref nextCellCostLeft, "nextCellCostLeft");
	    Scribe_Values.Look(ref nextCellCostTotal, "nextCellCostInitial");
	    Scribe_Values.Look(ref peMode, "peMode");
	    Scribe_Values.Look(ref lastMovedTick, "lastMovedTick", -999999);
	    Scribe_Values.Look(ref traverseMode, "traverseMode");
	    if (moving) Scribe_TargetInfo.Look(ref destination, "destination");
    }
    
    #region States

    public bool AtDestinationPosition => TouchPathEndModeUtility.IsAdjacentOrInsideAndAllowedToTouch(curThing.Position, destination, Map.pathing.normal);
    
    public bool Moving => moving;
    
    public float MovedPercent
    {
	    get
	    {
		    if (!Moving)
		    {
			    return 0f;
		    }
		    if (BuildingBlockingNextPathCell() != null)
		    {
			    return 0f;
		    }
		    if (NextCellDoorToWaitForOrManuallyOpen() != null)
		    {
			    return 0f;
		    }
		    return 1f - (nextCellCostLeft / nextCellCostTotal);
	    }
    }

    #endregion
    
    public void StartPath(LocalTargetInfo dest, PathEndMode peMode, TraverseMode traverseMode)
    {
	    //
	    StopDead();
	    
	    //
	    this.peMode = peMode;
	    this.traverseMode = traverseMode;
	    
	    if (dest.HasThing && dest.ThingDestroyed)
	    {
		    TLog.Error($"{curThing} pathing to destroyed thing {dest.Thing}");
		    PatherFailed();
		    return;
	    }

	    if (!ThingCanOccupy(curThing.Position) && !TryRecoverFromUnwalkablePosition()) return;

	    if (moving && curPath != null && destination == dest && this.peMode == peMode) return;

	    if (!curThing.Map.reachability.CanReach(curThing.Position, dest, peMode, traverseMode))
	    {
		    PatherFailed();
		    return;
	    }
	    
	    destination = dest;
	    if (!nextCell.Walkable(Map) || NextCellDoorToWaitForOrManuallyOpen() != null || nextCellCostLeft == nextCellCostTotal) 
		    ResetToCurrentPosition();

	    /*TODO: Things can have reservation?
	    PawnDestinationReservationManager.PawnDestinationReservation pawnDestinationReservation = this.curThing.Map.pawnDestinationReservationManager.MostRecentReservationFor(this.pawn);
	    if (pawnDestinationReservation != null &&
	        ((this.destination.HasThing && pawnDestinationReservation.target != this.destination.Cell) ||
	         (pawnDestinationReservation.job != this.pawn.CurJob &&
	          pawnDestinationReservation.target != this.destination.Cell)))
	    {
		    this.pawn.Map.pawnDestinationReservationManager.ObsoleteAllClaimedBy(this.pawn);
	    }
		*/
	    
	    if (AtDestinationPosition)
	    {
		    this.PatherArrived();
		    return;
	    }

	    /*TODO: Things cant be downed
	    if (this.pawn.Downed)
	    {
		    TLog.Error(this.pawn.LabelCap + " tried to path while downed. This should never happen. curJob=" +
		              this.pawn.CurJob.ToStringSafe<Job>());
		    this.PatherFailed();
		    return;
	    }
	    */

	    curPath?.ReleaseToPool();

	    curPath = null;
	    moving = true;
	    //this.pawn.jobs.posture = PawnPosture.Standing;
    }

    public void TryResumePathingAfterLoading()
    {
	    if (moving) 
		    StartPath(destination, peMode, traverseMode);
    }

    public void PatherTick()
    {
	    lastMovedTick = Find.TickManager.TicksGame;
	    if (nextCellCostLeft > 0f)
	    {
		    nextCellCostLeft -= CostToPayThisTick();
		    return;
	    }

	    if (moving) 
		    TryEnterNextPathCell();
    }

    private void SetupMoveIntoNextCell()
    {
	    if (curPath.NodesLeftCount <= 1)
	    {
		    TLog.Error($"{curThing} at {curThing.Position} ran out of path nodes while pathing to {destination}.");
		    PatherFailed();
		    return;
	    }

	    nextCell = curPath.ConsumeNextNode();
	    if (!nextCell.Walkable(Map)) Log.Error($"{curThing} entering {nextCell} which is unwalkable.");
	    int num = CostToMoveIntoCell(curThing, nextCell);
	    this.nextCellCostTotal = (float)num;
	    this.nextCellCostLeft = (float)num;
    }
    
    private void TryEnterNextPathCell()
    {
	    Building building = this.BuildingBlockingNextPathCell();
	    if (building != null)
	    {
		    Building_Door building_Door = building as Building_Door;
		    if (building_Door is not {FreePassage: true})
		    {
			    PatherFailed();
			    return;
		    }
	    }

	    Building_Door building_Door2 = NextCellDoorToWaitForOrManuallyOpen();
	    if (building_Door2 != null)
	    {
		    return;
	    }

	    lastCell = curThing.Position;
	    curThing.Position = nextCell;
	    
	    if (curThing.def.BaseMass > 5f) 
		    curThing.Map.snowGrid.AddDepth(curThing.Position, -0.001f);

	    if (NeedNewPath() && !TrySetNewPath())
	    {
		    return;
	    }

	    if (AtDestinationPosition)
	    {
		    PatherArrived();
		    return;
	    }

	    this.SetupMoveIntoNextCell();
    }
    
    private PawnPath GenerateNewPath()
    {
	    lastPathedTargetPosition = destination.Cell;
	    return AIUtils.TryGetPath(curThing.Position, destination, Map, traverseMode, peMode).Path;
    }

    private bool TrySetNewPath()
    {
	    var pawnPath = GenerateNewPath();
	    if (!pawnPath.Found)
	    {
		    PatherFailed();
		    return false;
	    }

	    curPath?.ReleaseToPool();
	    curPath = pawnPath;
	    return true;
    }
    
    private bool NeedNewPath()
    {
	    if (!destination.IsValid || curPath is not {Found: true} || curPath.NodesLeftCount == 0)
	    {
		    return true;
	    }

	    if (destination.HasThing && destination.Thing.Map != this.curThing.Map)
	    {
		    return true;
	    }

	    if ((curThing.Position.InHorDistOf(curPath.LastNode, 15f) ||
	         curThing.Position.InHorDistOf(destination.Cell, 15f)) &&
	        !curThing.Map.reachability.CanReach(curPath.LastNode, destination, peMode, traverseMode))
	    {
		    return true;
	    }

	    if (curPath.UsedRegionHeuristics && curPath.NodesConsumedCount >= 75) return true;

	    if (lastPathedTargetPosition != destination.Cell)
	    {
		    float num = (curThing.Position - destination.Cell).LengthHorizontalSquared;
		    var num2 = num switch
		    {
			    > 900f => 10f,
			    > 289f => 5f,
			    > 100f => 3f,
			    > 49f => 2f,
			    _ => 0.5f
		    };

		    if ((lastPathedTargetPosition - destination.Cell).LengthHorizontalSquared > num2 * num2) return true;
	    }
	    
	    PathingContext pc = this.curThing.Map.pathing.normal;
	    IntVec3 intVec = IntVec3.Invalid;
	    int num3 = 0;
	    while (num3 < 20 && num3 < this.curPath.NodesLeftCount)
	    {
		    IntVec3 intVec2 = this.curPath.Peek(num3);
		    if (!intVec2.Walkable(pc.map))
		    {
			    return true;
		    }

		    Building_Door building_Door = intVec2.GetEdifice(pc.map) as Building_Door;
		    if (building_Door != null)
		    {
			    if (!building_Door.FreePassage)
			    {
				    return true;
			    }
		    }

		    if (num3 != 0 && intVec2.AdjacentToDiagonal(intVec) &&
		        (PathFinder.BlocksDiagonalMovement(intVec2.x, intVec.z, pc, false) ||
		         PathFinder.BlocksDiagonalMovement(intVec.x, intVec2.z, pc, false)))
		    {
			    return true;
		    }

		    intVec = intVec2;
		    num3++;
	    }

	    return false;
    }

    private float CostToPayThisTick()
    {
	    var num = 1f;
	    if (num < nextCellCostTotal / MaxMoveTicks) 
		    num = nextCellCostTotal / MaxMoveTicks;
	    return num;
    }

    private int TicksPerMove(bool diagonal)
    {
	    float num = moveSpeed;
	    float num2 = num / 60f;
	    float num3;
	    if (num2 == 0f)
	    {
		    num3 = MaxMoveTicks;
	    }
	    else
	    {
		    num3 = 1f / num2;
		    if (curThing.Spawned && !Map.roofGrid.Roofed(curThing.Position))
		    {
			    num3 /= Map.weatherManager.CurMoveSpeedMultiplier;
		    }
		    if (diagonal)
		    {
			    num3 *= 1.41421f;
		    }
	    }
	    return Mathf.Clamp(Mathf.RoundToInt(num3), 1, MaxMoveTicks);
    }
    
    private int CostToMoveIntoCell(Thing thing, IntVec3 c)
    {
	    int num;
	    if (c.x == thing.Position.x || c.z == thing.Position.z) //Cardinal
	    {
		    num = TicksPerMove(false);
	    }
	    else //Diagonal
	    {
		    num = TicksPerMove(true);
	    }
	    num += Map.pathing.normal.pathGrid.CalculatedCostAt(c, false, curThing.Position);
	    if (num > MaxMoveTicks)
	    {
		    num = MaxMoveTicks;
	    }
	    return Mathf.Max(num, 1);
    }
    
    //TODO: Irrelevant for non-pawn pathed things
    public Building_Door NextCellDoorToWaitForOrManuallyOpen()
    {
	    Building_Door building_Door = this.curThing.Map.thingGrid.ThingAt<Building_Door>(this.nextCell);
	    if (building_Door is {SlowsPawns: true} && (!building_Door.Open || building_Door.TicksTillFullyOpened > 0) /*&& building_Door.PawnCanOpen(this.pawn)*/)
	    {
		    return building_Door;
	    }
	    return null;
    }
    
    public Building BuildingBlockingNextPathCell()
    {
	    var edifice = nextCell.GetEdifice(curThing.Map);
	    if (edifice != null && (edifice.def.passability == Traversability.Impassable))
	    {
		    return edifice;
	    }
	    return null;
    }
    
    private bool ThingCanOccupy(IntVec3 c)
    {
	    if (!c.Walkable(Map))
	    {
		    return false;
	    }
	    Building edifice = c.GetEdifice(curThing.Map);
	    Building_Door building_Door = edifice as Building_Door;
	    return building_Door == null || building_Door.Open; //building_Door.PawnCanOpen(this.pawn) 
    }

    public bool TryRecoverFromUnwalkablePosition(bool error = true)
    {
	    var flag = false;
	    var i = 0;
	    while (i < GenRadial.RadialPattern.Length)
	    {
		    var pos = curThing.Position;
		    var intVec = pos + GenRadial.RadialPattern[i];
		    if (ThingCanOccupy(intVec))
		    {
			    if (intVec == pos) return true;
			    if (error)
			    {
				    TLog.Warning($"{curThing} on unwalkable cell {curThing.Position}. Teleporting to {intVec}");
			    }

			    curThing.Position = intVec;
			    //this.curThing.Notify_Teleported(true, false);
			    flag = true;
			    break;
		    }

		    i++;
	    }

	    if (!flag)
	    {
		    curThing.Destroy();
		    TLog.Error($"{curThing.ToStringSafe()} on unwalkable cell {curThing.Position}. Could not find walkable position nearby. Destroyed.");
	    }

	    return flag;
    }
    
    private void PatherArrived()
    {
	   StopDead();
	   /*TODO: No pawn no job notifier
	   if (this.pawn.jobs.curJob != null)
	   {
		   this.pawn.jobs.curDriver.Notify_PatherArrived();
	   }
	   */
    }

    private void PatherFailed()
    {
        StopDead();
    }

    public void ResetToCurrentPosition()
    {
	    nextCell = curThing.Position;
	    nextCellCostLeft = 0f;
	    nextCellCostTotal = 1f;
    }

    public void StopDead()
    {
	    if (curPath != null) 
		    curPath.ReleaseToPool();
	    curPath = null;
	    moving = false;
	    nextCell = curThing.Position;
    }
    
    //
    public void DrawPath()
    {
	    if (curPath is not {Found: true})
	    {
		    return;
	    }
	    
	    float y = AltitudeLayer.Item.AltitudeFor();
	    if (curPath.NodesLeftCount > 0)
	    {
		    for (int i = 0; i < curPath.NodesLeftCount - 1; i++)
		    {
			    Vector3 a = curPath.Peek(i).ToVector3Shifted();
			    a.y = y;
			    Vector3 b = curPath.Peek(i + 1).ToVector3Shifted();
			    b.y = y;
			    GenDraw.DrawLineBetween(a, b);
		    }
		    if (curThing != null)
		    {
			    Vector3 drawPos = curThing.DrawPos;
			    drawPos.y = y;
			    Vector3 b2 = curPath.Peek(0).ToVector3Shifted();
			    b2.y = y;
			    if ((drawPos - b2).sqrMagnitude > 0.01f)
			    {
				    GenDraw.DrawLineBetween(drawPos, b2);
			    }
		    }
	    }
    }
}