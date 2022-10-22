using Verse;

namespace TeleCore;

public struct RoomPortal
{
    private readonly Building connector;
    private readonly RoomTracker portalRoom;
    private readonly RoomTracker[] connections;
    private readonly Rot4[] connectionDirections;

    public RoomPortal(Building connector, RoomTracker roomA, RoomTracker roomB, RoomTracker portalRoom)
    {
        this.connector = connector;
        this.portalRoom = portalRoom;
        connections = new[] {roomA, roomB};
        connectionDirections = new Rot4[2];
        
        //Get Directions
        for (int i = 0; i < 4; i++)
        {
            var cell = connector.Position + GenAdj.CardinalDirections[i];
            var room = cell.GetRoomFast(connector.Map);
            if(room == null) continue;
            if (roomA.Room == room)
                connectionDirections[0] = cell.Rot4Relative(connector.Position);
            if (roomB.Room == room)
                connectionDirections[1] = cell.Rot4Relative(connector.Position);
        }
    }

    public bool ConnectsToOutside => connections[0].IsOutside || connections[1].IsOutside;
    public bool ConnectsToSame => connections[0].IsOutside && connections[1].IsOutside || connections[0] == connections[1];
    public bool IsValid => connector != null;
    
    public Thing Connector => connector;
    public RoomTracker PortalRoom => portalRoom;
    public RoomTracker this[int i] => connections[i];
    
    //Helpers
    public int IndexOf(RoomTracker tracker)
    {
        return connections[0] == tracker ? 0 : 1;
    }

    public RoomTracker Opposite(RoomTracker other)
    {
        return other == connections[0] ? connections[1] : connections[0];
    }

    public bool Connects(RoomTracker toThis)
    {
        return toThis == connections[0] || toThis == connections[1];
    }
    
    //
    public override string ToString()
    {
        return $"{connections[0].Room.ID} -[{Connector}]-> {connections[1].Room.ID}";
    }
}