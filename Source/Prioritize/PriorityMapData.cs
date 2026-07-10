using System.Collections.Generic;
using Verse;

namespace Prioritize;

public class PriorityMapData : MapComponent
{
    private const ushort DefaultGridValue = 32768;

    public static int MapsWithCellPriorities { get; private set; }

    private byte[] griddata;
    private int nonZeroCellCount;
    private int numCells;
    private readonly HashSet<int> prioritizedCellIndices = [];
    private ushort[] priorityGrid;

    public int NonZeroCellCount => nonZeroCellCount;

    public PriorityMapData(Map map)
        : base(map)
    {
        priorityGrid = new ushort[map.cellIndices.NumGridCells];
        for (var i = 0; i < priorityGrid.Length; i++)
        {
            priorityGrid[i] = DefaultGridValue;
        }
    }

    public short GetPriorityAt(IntVec3 loc)
    {
        var index = map.cellIndices.CellToIndex(loc);
        var gridValue = priorityGrid[index];
        if (gridValue != 0)
        {
            return (short)(gridValue - DefaultGridValue);
        }

        Log.ErrorOnce($"Priority grid {loc} priority is -32767, Resetting to 0..",
            "PG32767Error".GetHashCode());
        priorityGrid[index] = DefaultGridValue;

        return 0;
    }

    public void SetPriorityAt(IntVec3 loc, short pri)
    {
        var index = map.cellIndices.CellToIndex(loc);
        var oldPri = (short)(priorityGrid[index] - DefaultGridValue);
        if (oldPri == pri)
        {
            return;
        }

        UpdateNonZeroCellCount(oldPri, pri);
        priorityGrid[index] = (ushort)(pri + DefaultGridValue);
        UpdatePrioritizedCellIndex(index, pri);
    }

    public IEnumerable<IntVec3> GetPrioritizedCells()
    {
        foreach (var index in prioritizedCellIndices)
        {
            yield return map.cellIndices.IndexToCell(index);
        }
    }

    private void UpdatePrioritizedCellIndex(int index, short pri)
    {
        if (pri != 0)
        {
            prioritizedCellIndices.Add(index);
        }
        else
        {
            prioritizedCellIndices.Remove(index);
        }
    }

    public override void FinalizeInit()
    {
        base.FinalizeInit();
        RecountNonZeroCells();
    }

    public override void ExposeData()
    {
        if (map != null)
        {
            numCells = map.cellIndices.NumGridCells;
        }

        Scribe_Values.Look(ref numCells, "numCells");
        switch (Scribe.mode)
        {
            case LoadSaveMode.Saving:
                MapExposeUtility.ExposeUshort(map, c => priorityGrid[map.cellIndices.CellToIndex(c)],
                    delegate(IntVec3 c, ushort val) { priorityGrid[map.cellIndices.CellToIndex(c)] = val; },
                    "priorityGrid");
                break;
            case LoadSaveMode.LoadingVars:
                priorityGrid = new ushort[numCells];
                DataExposeUtility.LookByteArray(ref griddata, "priorityGrid");
                DataSerializeUtility.LoadUshort(griddata, numCells,
                    delegate(int c, ushort val) { priorityGrid[c] = val; });
                griddata = null;
                break;
            case LoadSaveMode.PostLoadInit:
                RecountNonZeroCells();
                RebuildPrioritizedCellIndices();
                break;
        }
    }

    private void UpdateNonZeroCellCount(short oldPri, short newPri)
    {
        if (oldPri == 0 && newPri != 0)
        {
            nonZeroCellCount++;
            if (nonZeroCellCount == 1)
            {
                MapsWithCellPriorities++;
            }
        }
        else if (oldPri != 0 && newPri == 0)
        {
            nonZeroCellCount--;
            if (nonZeroCellCount == 0)
            {
                MapsWithCellPriorities--;
            }
        }
    }

    private void RecountNonZeroCells()
    {
        if (nonZeroCellCount > 0)
        {
            MapsWithCellPriorities--;
        }

        nonZeroCellCount = 0;
        if (priorityGrid == null)
        {
            return;
        }

        for (var i = 0; i < priorityGrid.Length; i++)
        {
            if (priorityGrid[i] != DefaultGridValue)
            {
                nonZeroCellCount++;
            }
        }

        if (nonZeroCellCount > 0)
        {
            MapsWithCellPriorities++;
        }
    }

    private void RebuildPrioritizedCellIndices()
    {
        prioritizedCellIndices.Clear();
        if (priorityGrid == null)
        {
            return;
        }

        for (var i = 0; i < priorityGrid.Length; i++)
        {
            if (priorityGrid[i] != DefaultGridValue)
            {
                prioritizedCellIndices.Add(i);
            }
        }
    }
}
