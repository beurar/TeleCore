using System;
using System.Diagnostics;
using NUnit.Framework;
using TeleCore.Data.Logging;
using TeleCore.Network;

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
            networkPartSet = new NetworkPartSet();

            for (int i = 0; i < 1000; i++)
            {
                var part = new NetworkSubPart();
                part.Id = i;
                part.Name = $"Part {i}";
                part.Value = i * 10;

                networkPartSet.fullSet.Add(part);
            }
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

        [Test]
        public void TestStructuresForRole()
        {
            // Profile the StructuresForRole method
            var stopwatch = Stopwatch.StartNew();

            var result = networkPartSet.StructuresForRole(NetworkRole.Consumer);

            stopwatch.Stop();
            Console.WriteLine($"StructuresForRole took {stopwatch.ElapsedMilliseconds} ms");
        }

        [Test]
        public void TestCachedString()
        {
            // Profile the CachedString property
            var stopwatch = Stopwatch.StartNew();

            var result = networkPartSet.CachedString;
            
            stopwatch.Stop();
            Console.WriteLine($"CachedString took {stopwatch.ElapsedMilliseconds} ms");
        }
    }
}