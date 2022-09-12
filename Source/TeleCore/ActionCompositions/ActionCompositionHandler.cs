using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TeleCore
{
    public class ActionCompositionHandler : IExposable
    {
        private static ActionCompositionHandler instance;

        //Local Instance
        private List<ActionComposition> currentCompositions = new List<ActionComposition>();

        public ActionCompositionHandler()
        {
            instance = this;
        }

        public void ExposeData()
        {
            //TODO:
            Scribe_Collections.Look(ref currentCompositions, nameof(currentCompositions), LookMode.Deep);
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
