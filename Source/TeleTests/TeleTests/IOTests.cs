using System.Collections.Generic;
using NUnit.Framework;
using TeleCore.Network.IO;

namespace TeleTests;

[TestFixture]
public class IOTests
{
    [Test]
    public void ConnectionTest()
    {
        var config = new IOConfig()
        {
            cells = new List<IOCell>()
            {
                new IOCell()
                {
                    direction = Rot4.North,
                    mode = NetworkIOMode.Input,
                    offset = Rot4.North.FacingCell,
                },
                new IOCell()
                {
                    direction = Rot4.South,
                    mode = NetworkIOMode.Output,
                    offset = Rot4.South.FacingCell,
                },
                new IOCell()
                {
                    direction = Rot4.East,
                    mode = NetworkIOMode.TwoWay,
                    offset = Rot4.East.FacingCell,
                },
                new IOCell()
                {
                    direction = Rot4.West,
                    mode = NetworkIOMode.TwoWay,
                    offset = Rot4.West.FacingCell,
                }
            }
        };
        
        //5
        //#  +
        //# +3+
        //#  +
        //#   
        //0####5####0####5####0

        NetworkIO io1 = new NetworkIO(config, new IntVec3(3, 0, 3));
        NetworkIO io2 = new NetworkIO(config, new IntVec3(4, 0, 3));
        
        Assert.IsTrue(io1.ConnectsTo(io2));
    }
}