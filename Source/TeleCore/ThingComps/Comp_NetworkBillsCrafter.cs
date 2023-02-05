using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public class Comp_NetworkBillsCrafter : Comp_NetworkStructure
    {
        public new Building_WorkTable parent;
        public NetworkBillStack billStack;

        //CompFX
        public Color CurColor => Color.clear;
        public override bool ShouldDoEffects => IsWorkedOn;

        public override bool FX_AffectsLayerAt(int index)
        {
            return base.FX_AffectsLayerAt(index);
        }

        public override bool FX_ShouldDrawAt(int index)
        {
            return index switch
            {
                0 => IsWorkedOn,
                _ => base.FX_ShouldDrawAt(index),
            };
        }

        public override Color? FX_GetColorAt(int index)
        {
            return index switch
            {
                0 => CurColor,
                _ => base.FX_GetColorAt(index),
            };
        }

        //Crafter Code
        public new CompProperties_NetworkBillsCrafter Props => (CompProperties_NetworkBillsCrafter)base.Props;

        public bool IsWorkedOn => BillStack.CurrentBill != null;
        public NetworkBillStack BillStack => billStack;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            parent = base.parent as Building_WorkTable;
            if (!respawningAfterLoad)
                billStack = new NetworkBillStack(this);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look(ref billStack, "tiberiumBillStack", this);
        }
    }
}
