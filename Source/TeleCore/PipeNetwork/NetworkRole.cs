using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeleCore
{
    [Flags]
    public enum NetworkRole : uint
    {
        Controller = 1U,
        Transmitter = 2U,
        Producer = 4U,
        Consumer = 8U,
        Storage = 16U,
        Requester = 32U,
        All = 64U,

        AllContainers = Producer | Consumer | Storage,
        //All = Transmitter | AllContainers,
    }
}
