using System;

namespace TeleCore.Network.Flow.Pressure;

public class PressureWorker_WaveEquationDamping3 : PressureWorker
{
    public override string Description => "Model that adds friction by making fluid stick to the pipe surface.";

    public override double Friction => 0;
    public override double CSquared => 0.03;
    public double DampFriction => 0.01;

    public override double FlowFunction(FlowBox t0, FlowBox t1, double f) 
    {
        var dp = this.PressureFunction(t0) - this.PressureFunction(t1);
        var src = f > 0 ? t0 : t1;
        var dc = Math.Max(0, src.PrevStack.TotalValue - src.TotalValue);
        f += dp * CSquared;
        f *= 1 - Friction;
        f *= (1 - Math.Min(0.5,(DampFriction * dc))); 
        return f;
    }
    
    public override double PressureFunction(FlowBox t) 
    {
        return t.TotalValue / t.MaxCapacity * 100;
    }
}
