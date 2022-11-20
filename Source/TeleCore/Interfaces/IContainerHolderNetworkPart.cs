using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeleCore
{
    public interface IContainerHolderNetworkPart : IContainerHolderThing
    {
        INetworkSubPart NetworkPart { get; }
        NetworkContainerSet ContainerSet { get; }
    }
}
