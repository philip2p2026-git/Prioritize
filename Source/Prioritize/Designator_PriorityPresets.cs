using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Prioritize;

public abstract class Designator_PriorityPresetBase : Designator
{
    protected abstract short GetPresetValue();

    protected virtual bool AllowPresetMenu => false;

    protected virtual bool NegativeMenu => false;

    public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
    {
        get
        {
            if (!AllowPresetMenu)
            {
                yield break;
            }

            foreach (var option in PriorityPresetMenu.Options(NegativeMenu))
            {
                yield return option;
            }
        }
    }

    public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
    {
        var result = base.GizmoOnGUI(topLeft, maxWidth, parms);
        if (AllowPresetMenu)
        {
            Designator_Dropdown.DrawExtraOptionsIcon(topLeft, GetWidth(maxWidth));
        }

        return result;
    }

    public override void ProcessInput(Event ev)
    {
        if (!CheckCanInteract())
        {
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
        icon = ContentFinder<Texture2D>.Get("UI/Prioritize/PriorityLower");
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
        icon = ContentFinder<Texture2D>.Get("UI/Prioritize/PriorityFlat");
        defaultDesc = "P_PriorityZeroDesc".Translate();
    }

    protected override short GetPresetValue() => 0;

    public override string Label => "0";
}

public class Designator_PriorityPresetPositive : Designator_PriorityPresetBase
{
    public Designator_PriorityPresetPositive()
    {
        icon = ContentFinder<Texture2D>.Get("UI/Prioritize/PriorityUpper");
        defaultDesc = "P_PriorityPositiveDesc".Translate();
    }

    protected override short GetPresetValue() => PrioritizeMod.Instance.Settings.PositivePreset;

    protected override bool AllowPresetMenu => true;

    public override string Label => GetPresetValue().ToString();
}
