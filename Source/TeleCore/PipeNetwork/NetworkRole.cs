using System;

namespace TeleCore
{
    [Flags]
    public enum NetworkRole : int
    {
        Controller = 1,
        Transmitter = 2,
        Passthrough = 4,
        Producer = 8,
        Consumer = 16,
        Storage = 32,
        Requester = 64,
        Valve = 128,
        Pump = 356,
        //All = 64,

        AllContainers = Producer | Consumer | Storage | Requester,
        AllFlag = Controller | Transmitter | Valve | AllContainers,
        //All = Transmitter | AllContainers,
    }
}
