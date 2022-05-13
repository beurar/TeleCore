using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeleCore
{
    public class ActionCompositionHandler
    {
        private static ActionCompositionHandler instance;

        //Local Instance
        private List<ActionComposition> currentCompositions = new List<ActionComposition>();

        public ActionCompositionHandler()
        {
            instance = this;
        }

        //Static Accessors
        public static void InitComposition(ActionComposition composition)
        {
            instance.currentCompositions.Add(composition);
        }

        public static void RemoveComposition(ActionComposition composition)
        {
            instance.currentCompositions.Remove(composition);
        }

        public void TickActionComps()
        {
            for (int i = currentCompositions.Count - 1; i >= 0; i--)
            {
                currentCompositions[i].Tick();
            }
        }
    }
}
