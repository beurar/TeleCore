using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeleCore.FlowCore.Implementations;

namespace TeleCore
{
    public interface IContainerLeaker
    {
        bool ShouldLeak { get; }
        NetworkContainer Container { get; }
    }
}
