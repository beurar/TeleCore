using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeleCore
{
    public interface INetworkComponent
    {
        public INetworkStructure Parent { get; }

        public NetworkDef NetworkDef { get; }
        public Network Network { get; set; }
        public NetworkContainer Container { get; }
        public NetworkComponentSet ConnectedComponentSet { get; }

        public bool IsMainController { get; }
        public bool HasLeak { get; }
        public bool HasConnection { get; }
        public bool HasContainer { get; }
        public bool IsReceiving { get; }

        public NetworkRole NetworkRole { get; }

        bool ConnectsTo(INetworkComponent other);
        bool NeedsValue(NetworkValueDef value);

        void Notify_NewComponentAdded(INetworkComponent component);
        void Notify_NewComponentRemoved(INetworkComponent component);
        void Notify_ReceivedValue();
    }
}
