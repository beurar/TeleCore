using System;
using UnityEngine;
using Verse;

namespace TeleCore.Network.PressureSystem;

public abstract class PressureModel
{
    public PressureConfig config;

    public virtual string Name { get; set; }
 
    public virtual PressureConfig Config { get; set; }

    public abstract float FlowFunc(FlowBox to, FlowBox from, int flow);

    public abstract float PressureFunc(FlowBox fb);
}

public struct PressureConfig
{
    public ConfigItem cSquared;
    public ConfigItem friction;
}

public struct ConfigItem
{
    public string desc;
    public float value;
    public FloatRange range;

    public ConfigItem(string desc, float value, FloatRange range)
    {
        this.desc = desc;
        this.value = value;
        this.range = range;
    }
}

public static class PressureModels
{
    public class PM_WaveEquation : PressureModel
    {
        public override string Name => "Wave Equation";

        public override PressureConfig Config=> new PressureConfig
        {
            cSquared = new("c-squared", 0.01f, new(0, 1)),
            friction = new("friction", 0.001f, new(0, 1))
        };

        public override float FlowFunc(FlowBox t0, FlowBox t1, int flow)
        {
            flow += Mathf.RoundToInt((PressureFunc(t0) - PressureFunc(t1)) * Config.cSquared.value);
            flow *= 1 - Config.friction.value;
            return flow;
        }

        public override float PressureFunc(FlowBox fb)
        {
            return (fb.content / (float)fb.maxContent) * 100;
        }
    }
    
    static PressureModels()
    {
        var WE_Config = new PressureConfig
        {
            cSquared = new("c-squared", 0.01f, new(0, 1)),
            friction = new("friction", 0.001f, new(0, 1))
        };
        var FLOW_FN = (fb1, fb2, f) =>
        {
            f += (this.pressureFn(t0) - this.pressureFn(t1)) * cfg.cSquared.value;
            f *= 1 - cfg.friction.value;
            return f;
        };
        WaveEquation = new PressureModel
        {
            modelName = "Wave Equation",
            config = WE_Config,
            flowFn = (fb1, fb2, f) =>
            {
                
                f += (this.pressureFn(t0) - this.pressureFn(t1)) * cfg.cSquared.value;
                f *= 1 - cfg.friction.value;
                return f;
            },
            pressureFn = (fb) =>
            {
                return (fb.content / (float)fb.maxContent) * 100;
            }
        }
    }
    
    public static PressureModel WaveEquation { get; private set; }

        "Wave Equation": {
        desc: "Wave equation with linear pressure.",
        flowFn: function(t0, t1, f) {
            var cfg = this.config;
            f += (this.pressureFn(t0) - this.pressureFn(t1)) * cfg.cSquared.value;
            f *= 1 - cfg.friction.value;
            return f;
        },
        pressureFn: function(t) {
            return t.content / t.maxContent * 100;
        },
        config: {
            cSquared: {
                value: 0.01,
                range: [0, 1],
                desc: "C-Squared"
            },
            friction: {
                value: 0.001,
                range: [0, 1],
                desc: "Friction"
            }
        }
    }
}