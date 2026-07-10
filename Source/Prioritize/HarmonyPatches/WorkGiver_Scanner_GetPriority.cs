using HarmonyLib;
using RimWorld;
using Verse;

namespace Prioritize.HarmonyPatches;

[HarmonyPatch(typeof(WorkGiver_Scanner), nameof(WorkGiver_Scanner.GetPriority), typeof(Pawn), typeof(TargetInfo))]
public class WorkGiver_Scanner_GetPriority
{
    public static void Postfix(Pawn pawn, TargetInfo t, ref float __result, WorkGiver_Scanner __instance)
    {
        if (!PriorityState.HasActivePriorities() || !pawn.IsPlayerControlled)
        {
            return;
        }

        if (!PriorityWorkIndex.HasPrioritiesFor(__instance, pawn.Map))
        {
            return;
        }

        var priority = 0f;

        if (t.HasThing)
        {
            priority += MainMod.save.TryGetThingPriority(t.Thing, out var pri) ? pri + 0.1f : 0;
        }

        if (PrioritizeMod.Instance.Settings.UseLowerAsHighPriority)
        {
            __result -= priority;
        }
        else
        {
            __result += priority;
        }
    }
}
