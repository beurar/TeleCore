using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TeleCore
{
    public interface IContainerHolder
    {
        string ContainerTitle { get; }
        ContainerProperties ContainerProps { get; }
        NetworkContainer Container { get; }
        Thing Thing { get; }

        void Notify_ContainerFull();
        void Notify_ContainerStateChanged();
    }
}
