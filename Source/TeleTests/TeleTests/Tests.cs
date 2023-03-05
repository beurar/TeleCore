using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using TeleCore;
using TeleCore.FlowCore;
using TeleCore.FlowCore.Implementations;
using Verse;

namespace TeleTests
{
    [TestFixture]
    public class Tests
    {
        public static NetworkDef[] NetworkDefs = new NetworkDef[1]
        {
            new NetworkDef
            {
                defName = "TestNetworkDef",
                containerLabel = "ContainerLabel",
                portableContainerDef = null,
                transmitterGraphic = null,
                overlayGraphic = null,
                controllerDef = null,
                transmitterDef = null
            },
        };

        public static NetworkValueDef[] ValueDefs = new NetworkValueDef[2]
        {
            new NetworkValueDef
            {
                defName = "Value1",
                label = null,
                description = null,
                descriptionHyperlinks = null,
                ignoreConfigErrors = false,
                ignoreIllegalLabelCharacterConfigError = false,
                modExtensions = null,
                shortHash = 0,
                index = 0,
                modContentPack = null,
                fileName = null,
                generated = false,
                debugRandomId = 0,
                labelShort = null,
                valueUnit = null,
                valueColor = default,
                sharesCapacity = false,
                viscosity = 0,
                capacityFactor = 0,
                networkDef = null,
                specialDroppedContainerDef = null,
                thingDroppedFromContainer = null,
                valueToThingRatio = 0
            },
            new NetworkValueDef
            {
                defName = "Value2",
                label = null,
                description = null,
                descriptionHyperlinks = null,
                ignoreConfigErrors = false,
                ignoreIllegalLabelCharacterConfigError = false,
                modExtensions = null,
                shortHash = 0,
                index = 0,
                modContentPack = null,
                fileName = null,
                generated = false,
                debugRandomId = 0,
                labelShort = null,
                valueUnit = null,
                valueColor = default,
                sharesCapacity = false,
                viscosity = 0,
                capacityFactor = 0,
                networkDef = null,
                specialDroppedContainerDef = null,
                thingDroppedFromContainer = null,
                valueToThingRatio = 0
            },
        };
        
        public class NetworkClassTest : IContainerImplementer<NetworkValueDef, IContainerHolderNetwork, NetworkContainer>, IContainerHolderNetwork
        {
            public NetworkClassTest()
            {
                NetworkPart = null;
            }
            
            public string ContainerTitle { get; }
            public void Notify_ContainerStateChanged(NotifyContainerChangedArgs<NetworkValueDef> args)
            {
                //Console.WriteLine($"Funny result args:\n{args}");
            }

            public Thing Thing { get; }

            public bool ShowStorageGizmo { get; }
            public INetworkSubPart NetworkPart { get; }
            public NetworkContainerSet ContainerSet { get; }
            public NetworkContainer Container { get; }
            public NetworkDef NetworkDef { get; }
        }
        
        public NetworkContainer TestContainer { get; set; }
        
        [SetUp]
        public void ContainerInitTest()
        {
            TestContainer = new NetworkContainer(new ContainerConfig
            {
                containerClass = null,
                baseCapacity = 10000,
                containerLabel = null,
                storeEvenly = false,
                dropContents = false,
                leaveContainer = false,
                valueDefs = new List<FlowValueDef>(ValueDefs),
                explosionProps = null
            }, new NetworkClassTest());
        }
        
        [Test]
        public void BasicContainerFunctionTest()
        {
            var result = TestContainer.TryAddValue((ValueDefs[0], 10));
            Assert.True(result);
            Assert.AreEqual(result.ActualAmount, 10);
        }
        
        [Test]
        public void ProfileTest1()
        {
            var values = new List<DefValue<NetworkValueDef, float>>();
            for (int i = 0; i < 1000; i++)
            {
                values.Add(new DefValue<NetworkValueDef, float>(ValueDefs[0], 1+i));
            }
            
            Console.WriteLine("Running 100 additions.");
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var failCount = 0;
            foreach (var value in values)
            {
                var result = TestContainer.TryAddValue(value);
                if (result.State == ValueState.Failed)
                    failCount++;
            }

            stopwatch.Stop();
            Console.WriteLine($"Container value:\n{TestContainer}");
            Console.WriteLine($"Execution time: {stopwatch.ElapsedMilliseconds} ms with {failCount} failures to add");
        }
    }
}