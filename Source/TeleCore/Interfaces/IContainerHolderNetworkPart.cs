using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeleCore
{
    public interface IContainerHolderNetworkPart<T> : IContainerHolderThing<T> where T : FlowValueDef
    {
        INetworkSubPart NetworkPart { get; }
        NetworkContainerSet ContainerSet { get; }
    }
}
