using System;
using TeleCore.FlowCore;

namespace TeleCore.Network.Flow.Pressure;

public class PressureWorker_WaveEquationDamping3 : PressureWorker
{
    public override string Description => "Model that adds friction by making fluid stick to the pipe surface.";

    //Note: Friction is key!!
    public override double Friction => 0.1f;
    public override double CSquared => 0.03;
    public double DampFriction => 0.01;

    public override double FlowFunction(FlowInterface<NetworkVolume, NetworkValueDef> iface, double f)
    {
        var from = iface.From;
        var to = iface.To;
        var dp = PressureFunction(from) - PressureFunction(to);

        if (iface.Mode == InterfaceFlowMode.FromTo && dp <= 0)
        {
            return 0;
        }
        
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