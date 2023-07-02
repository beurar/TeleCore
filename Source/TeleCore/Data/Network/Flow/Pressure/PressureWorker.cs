namespace TeleCore.Network.Flow.Pressure;

public abstract class PressureWorker
{
    public abstract string Description { get; }

    public abstract double CSquared { get; }
    public abstract double Friction { get; }

    public abstract double FlowFunction(NetworkVolume t0, NetworkVolume t1, double f);

    public abstract double PressureFunction(NetworkVolume t);
}