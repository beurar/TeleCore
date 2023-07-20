using Verse;

namespace TeleCore;

public class RoomComponentLink
{
    private readonly Rot4[] _connectionDirections;
    private readonly RoomComponent[] _connections;
    private readonly Thing _connector;
    
    public Thing Connector => _connector;
    
    public RoomComponentLink(Thing connector, RoomComponent roomA)
    {
        _connector = connector;
        _connections = new[] {roomA, null};
        _connectionDirections = new Rot4[2];
        
        //Get Directions
        for (var i = 0; i < 4; i++)
        {
            var cell = connector.Position + GenAdj.CardinalDirections[i];
            var room = cell.GetRoomFast(connector.Map);
            if (room == null) continue;
            if (roomA.Room == room)
            {
                _connectionDirections[0] = cell.Rot4Relative(connector.Position);
            }

            if (roomA.Room != room)
            {
                _connections[1] = room.GetRoomComp(roomA.GetType());
                _connectionDirections[1] = cell.Rot4Relative(connector.Position);
            }
        }
    }
    
    public RoomComponentLink(Thing b, RoomComponent roomA, RoomComponent roomB)
    {
        _connector = b;
        _connections = new[] {roomA, roomB};
    }
    
    public RoomComponent Opposite(RoomComponent other)
    {
        return other == _connections[0] ? _connections[1] : _connections[0];
    }
}