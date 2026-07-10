using System.Collections.Generic;
using Verse;

namespace Prioritize;

public class PSaveData : GameComponent
{
    public Dictionary<int, int> ThingPriority = new();

    public HashSet<int> CellSourcedThingIds = [];

    public PSaveData()
    {
    }

    public PSaveData(Game game)
    {
    }

    public bool TryGetThingPriority(Thing t, out int pri)
    {
        if (t != null)
        {
            return ThingPriority.TryGetValue(t.thingIDNumber, out pri);
        }

        Log.ErrorOnce("TryGetThingPriority called with null Thing.", "P_TGTP".GetHashCode());
        pri = 0;
        return false;
    }

    public bool IsCellSourced(int thingId)
    {
        return CellSourcedThingIds.Contains(thingId);
    }

    public void SetThingPriorityManual(Thing t, int p)
    {
        if (t == null)
        {
            Log.ErrorOnce("SetThingPriority called with null Thing.", "P_STP".GetHashCode());
            return;
        }

        CellSourcedThingIds.Remove(t.thingIDNumber);
        SetThingPriorityInternal(t, p);
    }

    public void ApplyCellSyncedPriority(Thing t, int p)
    {
        if (t == null)
        {
            return;
        }

        if (TryGetThingPriority(t, out _) && !IsCellSourced(t.thingIDNumber))
        {
            return;
        }

        if (p == 0)
        {
            ClearCellSyncedPriority(t);
            return;
        }

        CellSourcedThingIds.Add(t.thingIDNumber);
        SetThingPriorityInternal(t, p);
    }

    public void ClearCellSyncedPriority(Thing t)
    {
        if (t == null)
        {
            return;
        }

        if (!IsCellSourced(t.thingIDNumber))
        {
            return;
        }

        CellSourcedThingIds.Remove(t.thingIDNumber);
        SetThingPriorityInternal(t, 0);
    }

    public void SetThingPriority(Thing t, int p)
    {
        SetThingPriorityManual(t, p);
    }

    private void SetThingPriorityInternal(Thing t, int p)
    {
        ThingPriority.TryGetValue(t.thingIDNumber, out var oldPri);

        if (ThingPriority.ContainsKey(t.thingIDNumber))
        {
            if (p == 0)
            {
                ThingPriority.Remove(t.thingIDNumber);
            }
            else
            {
                ThingPriority[t.thingIDNumber] = p;
            }
        }
        else if (p != 0)
        {
            ThingPriority.Add(t.thingIDNumber, p);
        }

        PriorityWorkIndex.OnThingPriorityChanged(t, oldPri, p);
    }

    public static PriorityMapData GetPriorityMapData(Map m)
    {
        if (m != null)
        {
            return m.GetComponent<PriorityMapData>();
        }

        Log.Error("GetOrCreatePriorityMapData called with null Map.");
        return null;
    }

    public void ClearUnusedThingPriority()
    {
        var newThingPri = new Dictionary<int, int>();
        var newCellSourced = new HashSet<int>();
        foreach (var map in Find.Maps)
        {
            var things = map.spawnedThings;
            foreach (var thing in things)
            {
                if (ThingPriority.TryGetValue(thing.thingIDNumber, out var v))
                {
                    newThingPri.Add(thing.thingIDNumber, v);
                    if (CellSourcedThingIds.Contains(thing.thingIDNumber))
                    {
                        newCellSourced.Add(thing.thingIDNumber);
                    }
                }
            }
        }

        ThingPriority = newThingPri;
        CellSourcedThingIds = newCellSourced;
        PriorityWorkIndex.RebuildAll();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref ThingPriority, "thingPriority", LookMode.Value, LookMode.Value);
        Scribe_Collections.Look(ref CellSourcedThingIds, "cellSourcedThingIds", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            CellSourcedThingIds ??= [];
            PriorityWorkIndex.RebuildAll();
        }
    }
}
