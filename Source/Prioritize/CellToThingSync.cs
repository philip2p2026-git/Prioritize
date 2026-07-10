using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Prioritize;

public static class CellToThingSync
{
    public const int DefaultSyncIntervalTicks = 600;

    public static void SyncMap(Map map)
    {
        if (map == null || MainMod.save == null)
        {
            return;
        }

        var cellData = PSaveData.GetPriorityMapData(map);
        if (cellData == null)
        {
            return;
        }

        foreach (var cell in cellData.GetPrioritizedCells())
        {
            SyncCell(map, cell, cellData.GetPriorityAt(cell));
        }
    }

    public static void SyncCell(Map map, IntVec3 cell, short cellPriority)
    {
        if (map == null || MainMod.save == null || !cell.InBounds(map))
        {
            return;
        }

        foreach (var thing in map.thingGrid.ThingsListAt(cell))
        {
            SyncThingAtCell(thing, cellPriority);
        }
    }

    private static void SyncThingAtCell(Thing thing, short cellPriority)
    {
        if (thing == null || !MainMod.ThingShowCond(thing))
        {
            return;
        }

        var thingId = thing.thingIDNumber;
        var hasManualPriority = MainMod.save.TryGetThingPriority(thing, out _) &&
                                !MainMod.save.IsCellSourced(thingId);

        if (hasManualPriority)
        {
            return;
        }

        if (cellPriority != 0)
        {
            MainMod.save.ApplyCellSyncedPriority(thing, cellPriority);
        }
        else if (MainMod.save.IsCellSourced(thingId))
        {
            MainMod.save.ClearCellSyncedPriority(thing);
        }
    }

    public static void SyncAllMaps()
    {
        foreach (var map in Find.Maps)
        {
            SyncMap(map);
        }
    }

    public static void SyncCells(Map map, IEnumerable<IntVec3> cells)
    {
        if (map == null || MainMod.save == null)
        {
            return;
        }

        var cellData = PSaveData.GetPriorityMapData(map);
        if (cellData == null)
        {
            return;
        }

        foreach (var cell in cells)
        {
            if (cell.InBounds(map))
            {
                SyncCell(map, cell, cellData.GetPriorityAt(cell));
            }
        }
    }
}
