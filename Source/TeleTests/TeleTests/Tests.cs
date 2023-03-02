using System;
using NUnit.Framework;
using TeleCore;
using UnityEngine;
using Verse;

namespace TeleTests
{
    [TestFixture]
    public class Tests
    {
        public NetworkDef[] NetworkDefs = new NetworkDef[1]
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

        public NetworkValueDef[] ValueDefs = new NetworkValueDef[2]
        {
            new NetworkValueDef
            {
                defName = null,
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
                cachedLabelCap = default,
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
                defName = null,
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
                cachedLabelCap = default,
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
            public string ContainerTitle { get; }
            public void Notify_ContainerStateChanged(NotifyContainerChangedArgs<NetworkValueDef> args)
            {
                throw new NotImplementedException();
            }

            public Thing Thing { get; }

            public bool ShowStorageGizmo { get; }
            public INetworkSubPart NetworkPart { get; }
            public NetworkContainerSet ContainerSet { get; }
            public NetworkContainer Container { get; }
            public NetworkDef NetworkDef { get; }
        }
        
        [Test]
        public void Test1()
        {
            NetworkContainer container = new NetworkContainer(new ContainerConfig
            {
                containerClass = null,
                baseCapacity = 0,
                containerLabel = null,
                storeEvenly = false,
                dropContents = false,
                leaveContainer = false,
                valueDefs = null,
                explosionProps = null
            }, new NetworkClassTest());
            
            Assert.True(container.TryAddValue(ValueDefs[0], 10, out float actual));
            Assert.Equals(actual, 10);
        }
    }
}