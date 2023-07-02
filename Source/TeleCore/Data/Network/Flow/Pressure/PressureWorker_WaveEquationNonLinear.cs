namespace TeleCore.Network.Flow.Pressure;

public class PressureWorker_WaveEquationNonLinear : PressureWorker
{
    public override string Description => "Wave equation with non-linear pressure.";

    public override double Friction => 0.001;
    public override double CSquared => 0.01;

    public override double FlowFunction(NetworkVolume t0, NetworkVolume t1, double f)
    {
        f += (PressureFunction(t0) - PressureFunction(t1)) * CSquared;
        f *= 1 - Friction;
        return f;
    }

    public override double PressureFunction(NetworkVolume t)
    {
        var p = t.TotalValue / t.MaxCapacity * 100;
        return p <= 60 ? p : 60 + (p - 60) * 10;
    }
}