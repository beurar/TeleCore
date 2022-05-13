using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeleCore
{
    public class NetworkRoleProperties
    {
        public NetworkRole role = NetworkRole.Transmitter;
        //TODO... handled values?


        public static implicit operator NetworkRole(NetworkRoleProperties props) => props.role;

        public bool CheckIs(NetworkRole role)
        {
            return this == role;
        }
    }
}
