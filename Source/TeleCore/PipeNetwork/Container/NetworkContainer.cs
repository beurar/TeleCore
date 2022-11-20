using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using TeleCore.Static;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public class NetworkContainer : BaseContainer<NetworkValueDef>
    {
        //
        private Gizmo_NetworkStorage containerGizmoInt = null!;
        
        public Gizmo_NetworkStorage ContainerGizmo
        {
            get
            {
                return containerGizmoInt ??= new Gizmo_NetworkStorage()
                {
                    container = this
                };
            }
        }
        
        //
        public IContainerHolderNetworkPart ParentNetworkPart => Parent as IContainerHolderNetworkPart ?? null!;

        
        public NetworkContainer()
        {
        }

        public NetworkContainer(IContainerHolder parent) : base(parent)
        {
        }

        public NetworkContainer(IContainerHolder parent, DefValueStack<NetworkValueDef> valueStack) : base(parent, valueStack)
        {
        }

        public NetworkContainer(IContainerHolder parent, List<NetworkValueDef> acceptedTypes) : base(parent, acceptedTypes)
        {
        }

        //TODO: Drop unique containers for types with it set
        public void Notify_ParentDestroyed(DestroyMode mode, Map previousMap)
        {
            if (Parent == null || TotalStored <= 0 || mode == DestroyMode.Vanish) return;
            if ((mode is DestroyMode.Deconstruct or DestroyMode.Refund) && Props.leaveContainer && ParentNetworkPart.NetworkPart.NetworkDef.portableContainerDef != null)
            {
                PortableContainer container = (PortableContainer)ThingMaker.MakeThing(ParentNetworkPart.NetworkPart.NetworkDef.portableContainerDef);
                container.SetupProperties(ParentNetworkPart.NetworkPart.NetworkDef, (NetworkContainer)Copy(container), Props);
                GenSpawn.Spawn(container, ParentThing.Position, previousMap);
            }

            if (mode is DestroyMode.KillFinalize)
            {
                if (Props.explosionProps != null)
                {
                    if (TotalStored > 0)
                    {
                        //float radius = Props.explosionProps.explosionRadius * StoredPercent;
                        //int damage = (int)(10 * StoredPercent);
                        //var mainTypeDef = MainValueType.dropThing;
                        Props.explosionProps.DoExplosion(ParentThing.Position, previousMap, ParentThing);

                        //GenExplosion.DoExplosion(Parent.Thing.Position, previousMap, radius, DamageDefOf.Bomb, Parent.Thing, damage, 5, null, null, null, null, mainTypeDef, 0.18f);
                    }
                }

                if (Props.dropContents)
                {
                    int i = 0;
                    List<Thing> drops = this.Get_ThingDrops().ToList();
                    Predicate<IntVec3> pred = c => c.InBounds(previousMap) && c.GetEdifice(previousMap) == null;
                    Action<IntVec3> action = delegate(IntVec3 c)
                    {
                        if (i < drops.Count)
                        {
                            Thing drop = drops[i];
                            if (drop != null)
                            {
                                GenSpawn.Spawn(drop, c, previousMap);
                                drops.Remove(drop);
                            }

                            i++;
                        }
                    };
                    _ = TeleFlooder.Flood(previousMap, ParentThing.OccupiedRect(), action, pred, drops.Count);
                }
            }

            Data_Clear();
        }

        //Virtual Functions
        public override IEnumerable<Thing> Get_ThingDrops()
        {
            foreach (var storedValue in StoredValuesByType)
            {
                if(storedValue.Key.thingDroppedFromContainer == null) continue;
                int count = Mathf.RoundToInt(storedValue.Value / storedValue.Key.valueToThingRatio);
                if(count <= 0) continue;
                yield return ThingMaker.MakeThing(storedValue.Key.thingDroppedFromContainer);
            }
            yield break;
        }

        public override void Notify_AddedValue(NetworkValueDef valueType, float value)
        {
            ParentNetworkPart?.ContainerSet?.Notify_AddedValue(valueType, value, ParentNetworkPart.NetworkPart);
            base.Notify_AddedValue(valueType, value);
            
        }

        public override void Notify_RemovedValue(NetworkValueDef valueType, float value)
        {
            ParentNetworkPart?.ContainerSet?.Notify_RemovedValue(valueType, value, ParentNetworkPart.NetworkPart);
            base.Notify_RemovedValue(valueType, value);
        }

        //
        public override IEnumerable<Gizmo> GetGizmos()
        {
            if (Capacity <= 0) yield break;


            if (Find.Selector.NumSelected == 1 && Find.Selector.IsSelected(ParentThing))
            {
                yield return ContainerGizmo;
            }

            /*
            if (DebugSettings.godMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = $"DEBUG: Container Options {Props.maxStorage}",
                    icon = TiberiumContent.ContainMode_TripleSwitch,
                    action = delegate
                    {
                        List<FloatMenuOption> list = new List<FloatMenuOption>();
                        list.Add(new FloatMenuOption("Add ALL", delegate
                        {
                            foreach (var type in AcceptedTypes)
                            {
                                TryAddValue(type, 500, out _);
                            }
                        }));
                        list.Add(new FloatMenuOption("Remove ALL", delegate
                        {
                            foreach (var type in AcceptedTypes)
                            {
                                TryRemoveValue(type, 500, out _);
                            }
                        }));
                        foreach (var type in AcceptedTypes)
                        {
                            list.Add(new FloatMenuOption($"Add {type}", delegate
                            {
                                TryAddValue(type, 500, out var _);
                            }));
                        }
                        FloatMenu menu = new FloatMenu(list, $"Add NetworkValue", true);
                        menu.vanishIfMouseDistant = true;
                        Find.WindowStack.Add(menu);
                    }
                };
            }
            */
        }
    }
}
