namespace TeleCore.Network.Flow.Pressure;

public class PressureWorker_WaveEquation : PressureWorker
{
    public override string Description => "Wave equation with linear pressure.";

    public override double Friction => 0.001;
    public override double CSquared => 0.01;

    public override double FlowFunction(NetworkVolume from, NetworkVolume to, double f)
    {
        f += (PressureFunction(from) - PressureFunction(to)) * CSquared;
        f *= 1 - Friction;
        return f;
    }

    public override double PressureFunction(NetworkVolume t)
    {
        return t.TotalValue / t.MaxCapacity * 100;
    }
}