using Verse;

namespace Prioritize;

public static class PriorityState
{
    public static bool HasActivePriorities()
    {
        if (MainMod.save == null)
        {
            return false;
        }

        return MainMod.save.ThingPriority.Count > 0;
    }
}
