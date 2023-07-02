using System;
using System.Diagnostics;
using NUnit.Framework;
using TeleCore;
using TeleCore.Network.Data;
using Verse;

namespace TeleTests;

[TestFixture]
public class PipeNetworkTests
{
    public class NetworkPartSetTest
    {
        private NetworkPartSet networkPartSet;

        [SetUp]
        public void Setup()
        {
            // Create a new NetworkPartSet with 1000 items
            networkPartSet = new NetworkPartSet(new NetworkDef());
            
        }

        [Test]
        public void TestPartByPos()
        {
            // Profile the PartByPos method
            var stopwatch = Stopwatch.StartNew();

            var result = networkPartSet.PartByPos(new IntVec3(0, 0, 0));

            stopwatch.Stop();
            Console.WriteLine($"PartByPos took {stopwatch.ElapsedMilliseconds} ms");
        }
    }
}