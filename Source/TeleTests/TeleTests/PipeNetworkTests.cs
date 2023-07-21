using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using NUnit.Framework;
using TeleCore;
using TeleCore.Network.Data;
using TeleCore.Network.Flow;
using TeleCore.Primitive;
using Verse;

namespace TeleTests;

[TestFixture]
public class PipeNetwork_EqualizationTests
{
    private static List<NetworkVolume> volumes;
    private static List<FlowInterface<NetworkVolume>> interfaces;
    private static Dictionary<NetworkVolume, List<FlowInterface<NetworkVolume>>> connections;
    
    public static NetworkValueDef[] defs = new NetworkValueDef[2]
    {
        new NetworkValueDef
        {
            defName = "GasA",
            label = "gas A",
            labelShort = "a",
            valueUnit = "°",
            valueColor = Color.red,
            viscosity = 1,
            friction = 0
        },
        new NetworkValueDef
        {
            defName = "GasB",
            label = "gas B",
            labelShort = "b",
            valueUnit = "°",
            valueColor = Color.blue,
            viscosity = 1,
            friction = 0
        },
    };
    
    [SetUp]
    public void Setup()
    {
        var config = new FlowVolumeConfig<NetworkValueDef>
        {
            allowedValues = new List<NetworkValueDef>(),
            capacity = 1000
        };

        volumes = new List<NetworkVolume>();
        connections = new Dictionary<NetworkVolume, List<FlowInterface<NetworkVolume>>>();
        volumes.Add(new NetworkVolume(config));
        volumes.Add(new NetworkVolume(config));

        var atmosInterface = new List<FlowInterface<NetworkVolume>> {new(volumes[0], volumes[1])};
        connections.Add(volumes[0], atmosInterface);
        connections.Add(volumes[1], atmosInterface);

        interfaces = new List<FlowInterface<NetworkVolume>>(atmosInterface);
    }
    
    [Test]
    public void Equalization()
    {
        var res1 = volumes[0].TryAdd(defs[0], 250);
        var res2 = volumes[0].TryAdd(defs[1], 250);

        int count = 0;
        do
        {
            Equalize(count);
            count++;
            Console.WriteLine($"[{count}][{interfaces[0].NextFlow}]");

        } 
        while (interfaces[0].NextFlow > 0.00001);
                
        var calc = Math.Abs(500d - (volumes[0].TotalValue + volumes[1].TotalValue));
        Assert.IsTrue(calc < 0.00001d);
    }
    
    private static void Equalize(int step)
    {
        //Prepare
        foreach (NetworkVolume volume in volumes)
        {
            volume.PrevStack = volume.Stack;
        }
            
        //Update Flow
        foreach (var conn in interfaces)
        {
            double flow = conn.NextFlow;      
            var from = conn.From;
            var to = conn.To;
            flow = AtmosphericSystem.FlowFunc(from, to, flow, out double dp);
            conn.UpdateBasedOnFlow(flow);
            flow = Math.Abs(flow);
            conn.NextFlow = AtmosphericSystem.ClampFunc(connections,from, to, flow);
            conn.Move = AtmosphericSystem.ClampFunc(connections, from, to, flow);
        }
            
        //Upate Content
        foreach (var conn in interfaces)
        {
            DefValueStack<NetworkValueDef, double> res = conn.From.RemoveContent(conn.Move);
            conn.To.AddContent(res);
            //Console.WriteLine($"Moved: " + conn.Move + $":\n{res}");
                    
            //TODO: Structify for: _connections[fb][i] = conn;
        }
    }
}