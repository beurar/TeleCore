using System.Collections.Generic;
using NUnit.Framework;
using TeleCore.Network.IO;
using TeleCore.Primitive;
using Verse;
using NetworkIO = TeleCore.Network.IO.NetworkIO;

namespace TeleTests;

[TestFixture]
public class IOTests
{
    [Test]
    public void ConnectionTest()
    {
        var config = new NetIOConfig()
        {
            cellsNorth = new List<IOCellPrototype>()
            {
                new ()
                {
                    direction = Rot4.North,
                    mode = NetworkIOMode.Input,
                    offset = Rot4.North.FacingCell,
                },
                new ()
                {
                    direction = Rot4.South,
                    mode = NetworkIOMode.Output,
                    offset = Rot4.South.FacingCell,
                },
                new ()
                {
                    direction = Rot4.East,
                    mode = NetworkIOMode.TwoWay,
                    offset = Rot4.East.FacingCell,
                },
                new ()
                {
                    direction = Rot4.West,
                    mode = NetworkIOMode.TwoWay,
                    offset = Rot4.West.FacingCell,
                }
            }
        };
        
        config.PostLoad(null);
        
        //5
        //#  +
        //# +3+
        //#  +
        //#   
        //0####5####0####5####0

        NetworkIO io1 = new NetworkIO(config, new IntVec3(3, 0, 3), Rot4.North);
        NetworkIO io2 = new NetworkIO(config, new IntVec3(4, 0, 3), Rot4.North);
        
        Assert.IsTrue(io1.ConnectsTo(io2));
    }
}