using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Prioritize;

public static class PriorityWorkIndex
{
    private static readonly int[] GlobalGroupCounts = new int[(int)PriorityScannerGroup.Count];

    private static readonly Dictionary<Map, int[]> MapGroupCounts = new();

    public static void RebuildAll()
    {
        ClearCounts();

        if (MainMod.save == null)
        {
            return;
        }

        foreach (var map in Find.Maps)
        {
            foreach (var thing in map.spawnedThings)
            {
                if (MainMod.save.TryGetThingPriority(thing, out var pri) && pri != 0)
                {
                    RegisterThing(thing, pri, isAdd: true);
                }
            }
        }
    }

    public static void OnThingPriorityChanged(Thing thing, int oldPri, int newPri)
    {
        if (thing?.Map == null)
        {
            if (oldPri != 0 && newPri == 0)
            {
                RebuildAll();
            }

            return;
        }

        if (oldPri != 0)
        {
            UnregisterThing(thing);
        }

        if (newPri != 0)
        {
            RegisterThing(thing, newPri, isAdd: true);
        }
    }

    public static void UnregisterThing(Thing thing)
    {
        if (thing?.Map == null)
        {
            return;
        }

        var groups = ClassifyThing(thing);
        ApplyGroups(thing.Map, groups, -1);
    }

    public static bool HasPrioritiesFor(WorkGiver workGiver, Map map)
    {
        if (!PriorityState.HasActivePriorities() || workGiver == null)
        {
            return false;
        }

        var group = GetScannerGroup(workGiver);
        if (group >= PriorityScannerGroup.Count)
        {
            return false;
        }

        if (map != null && MapGroupCounts.TryGetValue(map, out var mapCounts))
        {
            return mapCounts[(int)group] > 0;
        }

        return GlobalGroupCounts[(int)group] > 0;
    }

    public static bool HasPrioritiesForAnyMap(WorkGiver workGiver)
    {
        if (!PriorityState.HasActivePriorities() || workGiver == null)
        {
            return false;
        }

        var group = GetScannerGroup(workGiver);
        return group < PriorityScannerGroup.Count && GlobalGroupCounts[(int)group] > 0;
    }

    public static PriorityScannerGroup GetScannerGroup(WorkGiver workGiver)
    {
        var defName = workGiver.def.defName;

        if (workGiver is Workgiver_UniversalConstruct ||
            defName.StartsWith("Construct") ||
            defName is "FixBrokenDownBuilding" or "RemoveBuilding")
        {
            return PriorityScannerGroup.Construction;
        }

        if (workGiver is WorkGiver_Haul || defName.Contains("Haul"))
        {
            return PriorityScannerGroup.Hauling;
        }

        return defName switch
        {
            "Mine" => PriorityScannerGroup.Mining,
            "CutPlant" or "Harvest" or "GrowerSow" => PriorityScannerGroup.Growing,
            "CleanFilth" => PriorityScannerGroup.Cleaning,
            "Repair" => PriorityScannerGroup.Repair,
            "SmoothSurface" or "SmoothFloor" => PriorityScannerGroup.SmoothFloor,
            "Deconstruct" => PriorityScannerGroup.Deconstruct,
            _ => PriorityScannerGroup.Construction
        };
    }

    public static PriorityScannerGroup ClassifyThing(Thing thing)
    {
        if (thing == null || !thing.Spawned)
        {
            return PriorityScannerGroup.Count;
        }

        var map = thing.Map;
        var result = PriorityScannerGroup.Count;

        if (thing is Blueprint or Frame)
        {
            return PriorityScannerGroup.Construction;
        }

        if (thing.def.EverHaulable)
        {
            return PriorityScannerGroup.Hauling;
        }

        if (map == null)
        {
            return result;
        }

        var desMgr = map.designationManager;

        if (desMgr.DesignationAt(thing.Position, DesignationDefOf.Mine) != null)
        {
            return PriorityScannerGroup.Mining;
        }

        if (desMgr.DesignationAt(thing.Position, DesignationDefOf.CutPlant) != null ||
            desMgr.DesignationAt(thing.Position, DesignationDefOf.HarvestPlant) != null)
        {
            return PriorityScannerGroup.Growing;
        }

        if (thing.def.category == ThingCategory.Filth)
        {
            return PriorityScannerGroup.Cleaning;
        }

        if (thing is Building &&
            thing.def.building.repairable &&
            thing.def.useHitPoints &&
            thing.HitPoints < thing.MaxHitPoints &&
            map.areaManager.Home[thing.Position])
        {
            return PriorityScannerGroup.Repair;
        }

        if (desMgr.DesignationAt(thing.Position, DesignationDefOf.SmoothFloor) != null)
        {
            return PriorityScannerGroup.SmoothFloor;
        }

        if (desMgr.DesignationAt(thing.Position, DesignationDefOf.Deconstruct) != null)
        {
            return PriorityScannerGroup.Deconstruct;
        }

        if (desMgr.DesignationOn(thing) != null || map.designationManager.HasMapDesignationAt(thing.Position))
        {
            return PriorityScannerGroup.Construction;
        }

        return result;
    }

    private static void RegisterThing(Thing thing, int pri, bool isAdd)
    {
        if (pri == 0 || thing?.Map == null)
        {
            return;
        }

        var group = ClassifyThing(thing);
        if (group >= PriorityScannerGroup.Count)
        {
            return;
        }

        ApplyGroups(thing.Map, group, 1);
    }

    private static void ApplyGroups(Map map, PriorityScannerGroup group, int delta)
    {
        if (group >= PriorityScannerGroup.Count || map == null)
        {
            return;
        }

        if (!MapGroupCounts.TryGetValue(map, out var mapCounts))
        {
            mapCounts = new int[(int)PriorityScannerGroup.Count];
            MapGroupCounts[map] = mapCounts;
        }

        mapCounts[(int)group] += delta;
        if (mapCounts[(int)group] < 0)
        {
            mapCounts[(int)group] = 0;
        }

        GlobalGroupCounts[(int)group] += delta;
        if (GlobalGroupCounts[(int)group] < 0)
        {
            GlobalGroupCounts[(int)group] = 0;
        }
    }

    private static void ClearCounts()
    {
        for (var i = 0; i < GlobalGroupCounts.Length; i++)
        {
            GlobalGroupCounts[i] = 0;
        }

        MapGroupCounts.Clear();
    }
}
