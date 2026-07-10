using Verse;

namespace Prioritize;

/// <summary>
///     Definition of the settings for the mod
/// </summary>
internal class PrioritizeSettings : ModSettings
{
    public bool UseLowerAsHighPriority;
    public bool UseUnsafePatches;
    public CellSyncMode CellSyncMode = CellSyncMode.ImmediateAndPeriodic;
    public int CellSyncIntervalTicks = CellToThingSync.DefaultSyncIntervalTicks;
    public short NegativePreset = -3;
    public short PositivePreset = 3;

    /// <summary>
    ///     Saving and loading the values
    /// </summary>
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref UseUnsafePatches, "UseUnsafePatches");
        Scribe_Values.Look(ref UseLowerAsHighPriority, "UseLowerAsHighPriority");
        Scribe_Values.Look(ref CellSyncMode, "CellSyncMode", CellSyncMode.ImmediateAndPeriodic);
        Scribe_Values.Look(ref CellSyncIntervalTicks, "CellSyncIntervalTicks",
            CellToThingSync.DefaultSyncIntervalTicks);
        Scribe_Values.Look(ref NegativePreset, "NegativePreset", (short)-3);
        Scribe_Values.Look(ref PositivePreset, "PositivePreset", (short)3);
    }
}
