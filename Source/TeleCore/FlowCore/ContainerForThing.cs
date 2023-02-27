using System.Collections.Generic;
using Verse;

namespace TeleCore;

public class ContainerForThing<TValue, THolder, TContainer> : BaseContainer<TValue, THolder, TContainer>
    where TValue : FlowValueDef 
    where THolder : IContainerHolderThing<TValue, THolder, TContainer>
    where TContainer : ContainerForThing<TValue, THolder, TContainer>
{
    //Cache
    private Gizmo_ContainerStorage<TValue, THolder, TContainer> containerGizmoInt = null!;


    public Gizmo_ContainerStorage<TValue, THolder, TContainer> ContainerGizmo
    {
        get
        {
            return containerGizmoInt ??= new Gizmo_ContainerStorage<TValue, THolder, TContainer>((TContainer)this);
        }
    }
    
    public Thing ParentThing => Parent.Thing;

    #region Constructors

    public ContainerForThing(THolder parent) : base(parent) { }

    public ContainerForThing(THolder parent, DefValueStack<TValue> valueStack) : base(parent, valueStack) { }

    public ContainerForThing(THolder parent, List<TValue> acceptedTypes) : base(parent, acceptedTypes) { }
    
    #endregion
    
    
    public override IEnumerable<Gizmo> GetGizmos()
    {
        if (Capacity <= 0) yield break;


        if (Parent.ShowStorageForThingGizmo)
        {
            if (Find.Selector.NumSelected == 1 && Find.Selector.IsSelected(ParentThing)) yield return ContainerGizmo;
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

public class ContainerForThing<TValue> : ContainerForThing<TValue, IContainerHolderThing<TValue>, ContainerForThing<TValue>>
    where TValue : FlowValueDef
{
    #region Constructor

    public ContainerForThing(IContainerHolderThing<TValue> parent) : base(parent)
    {
    }

    public ContainerForThing(IContainerHolderThing<TValue> parent, DefValueStack<TValue> valueStack) : base(parent, valueStack)
    {
    }

    public ContainerForThing(IContainerHolderThing<TValue> parent, List<TValue> acceptedTypes) : base(parent, acceptedTypes)
    {
    }

    #endregion
}