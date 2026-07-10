using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Prioritize;

public abstract class Designator_PriorityPresetBase : Designator
{
    protected Designator_PriorityPresetBase()
    {
        icon = MainMod.ShowPriority;
    }

    protected abstract short GetPresetValue();

    protected virtual bool AllowPresetMenu => false;

    protected virtual bool NegativeMenu => false;

    public override void ProcessInput(Event ev)
    {
        if (!CheckCanInteract())
        {
            return;
        }

        if (AllowPresetMenu && ev.button == 1)
        {
            PriorityPresetMenu.Show(NegativeMenu);
            return;
        }

        MainMod.SelectedPriority = GetPresetValue();
        SoundDefOf.Tick_High.PlayOneShotOnCamera();
    }

    public override AcceptanceReport CanDesignateCell(IntVec3 loc)
    {
        return false;
    }
}

public class Designator_PriorityPresetNegative : Designator_PriorityPresetBase
{
    public Designator_PriorityPresetNegative()
    {
        defaultDesc = "P_PriorityNegativeDesc".Translate();
    }

    protected override short GetPresetValue() => PrioritizeMod.Instance.Settings.NegativePreset;

    protected override bool AllowPresetMenu => true;

    protected override bool NegativeMenu => true;

    public override string Label => GetPresetValue().ToString();
}

public class Designator_PriorityPresetZero : Designator_PriorityPresetBase
{
    public Designator_PriorityPresetZero()
    {
        defaultDesc = "P_PriorityZeroDesc".Translate();
    }

    protected override short GetPresetValue() => 0;

    public override string Label => "0";
}

public class Designator_PriorityPresetPositive : Designator_PriorityPresetBase
{
    public Designator_PriorityPresetPositive()
    {
        defaultDesc = "P_PriorityPositiveDesc".Translate();
    }

    protected override short GetPresetValue() => PrioritizeMod.Instance.Settings.PositivePreset;

    protected override bool AllowPresetMenu => true;

    public override string Label => GetPresetValue().ToString();
}
