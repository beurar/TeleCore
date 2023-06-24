using System;

namespace TeleCore.Network.Flow.Pressure;


public class PressureWorker_WaveEquationDamping : PressureWorker
{
    public override string Description => "Model that can quickly eliminate waves without relying on friction.";

    public override double Friction => 0;
    public override double CSquared => 0.03;
    public double CSquaredDamper => 0.04;

    public override double FlowFunction(FlowBox t0, FlowBox t1, double f) 
    {
        var dp = this.PressureFunction(t0) - this.PressureFunction(t1);
        var c = Math.Sign(f) == Math.Sign(dp) ? CSquared : CSquaredDamper;
        f += dp * c;
        f *= 1 - Friction;
        return f;
    }
    
    public override double PressureFunction(FlowBox t) 
    {
        return t.TotalValue / t.MaxCapacity * 100;
    }
}