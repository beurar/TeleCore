using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeleCore
{
    public class UndoRedoAction
    {

    }

    public static class UIUndoRedoStack
    {
        internal static Dictionary<Type, List<UndoRedoAction>> ActionsByContext = new();

        public static void DoNewAction(Type inContext)
        {

        }
    }
}
