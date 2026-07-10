using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Prioritize;

public class Designator_PrioritySettings : Designator
{
    private static readonly List<IntVec3> HighlightCells = [];

    public Designator_PrioritySettings()
    {
        icon = ContentFinder<Texture2D>.Get("UI/Prioritize/PrioritySettings");
        defaultLabel = "P_SelectionOptions".Translate();
        defaultDesc = "P_SelectionOptionsDesc".Translate();
    }

    public override void ProcessInput(Event ev)
    {
        if (CheckCanInteract())
        {
            PriorityShowConditions.ShowConditionsMenuBox();
        }
    }

    public override AcceptanceReport CanDesignateCell(IntVec3 loc)
    {
        if (loc.Fogged(Map) || !loc.InBounds(Map))
        {
            return false;
        }

        foreach (var t in Map.thingGrid.ThingsAt(loc))
        {
            if (CanDesignateThing(t).Accepted)
            {
                return true;
            }
        }

        return false;
    }

    public override AcceptanceReport CanDesignateThing(Thing t)
    {
        return MainMod.IsSelectableThing(t);
    }

    public override void SelectedUpdate()
    {
        var map = Map;
        if (map == null)
        {
            return;
        }

        var rect = MainMod.GetMapRect();
        if (rect.Area >= 10000)
        {
            GenUI.RenderMouseoverBracket();
            return;
        }

        HighlightCells.Clear();
        foreach (var cell in rect)
        {
            if (cell.InBounds(map))
            {
                HighlightCells.Add(cell);
            }
        }

        DesignatorUtility.RenderHighlightOverSelectableThings(this, HighlightCells);
        GenUI.RenderMouseoverBracket();
    }

    public override void RenderHighlight(List<IntVec3> dragCells)
    {
        DesignatorUtility.RenderHighlightOverSelectableThings(this, dragCells);
    }
}
