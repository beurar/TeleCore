using System;
using TeleCore.FlowCore;

namespace TeleCore.Network.Flow.Pressure;

public class PressureWorker_WaveEquationDamping3 : PressureWorker
{
    public override string Description => "Model that adds friction by making fluid stick to the pipe surface.";

    //Note: Friction is key!!
    public override double Friction => 0.01f;
    public override double CSquared => 0.03;
    public double DampFriction => 0.01;

    public override double FlowFunction(NetworkVolume from, NetworkVolume to, double f)
    {
        var dp = PressureFunction(from) - PressureFunction(to);
        var src = f > 0 ? from : to;
        var dc = Math.Max(0, src.PrevStack.TotalValue - src.TotalValue);
        f += dp * CSquared;
        f *= 1 - Friction;
        f *= 1 - GetTotalFriction(src); //Additional Friction from each fluid/gas
        f *= 1 - Math.Min(0.5, DampFriction * dc);
        return f;
    }
    
    public override double PressureFunction(NetworkVolume t)
    {
        return t.TotalValue / t.MaxCapacity * 100;
    }
}