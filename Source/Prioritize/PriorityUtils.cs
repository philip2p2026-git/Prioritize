using Verse;

namespace Prioritize;

public static class PriorityUtils
{
    public static float GetPriority(Thing t)
    {
        var pr = MainMod.save.TryGetThingPriority(t, out var pri) ? pri : 0;

        if (PrioritizeMod.Instance.Settings.UseLowerAsHighPriority)
        {
            pr = -pr;
        }

        return pr;
    }
}
