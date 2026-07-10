using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Prioritize.HarmonyPatches;

[HarmonyPatch(typeof(WorkGiver_Scanner), nameof(WorkGiver_Scanner.Prioritized), MethodType.Getter)]
public class WorkGiver_Scanner_Prioritized
{
    public static void Postfix(WorkGiver_Scanner __instance, ref bool __result)
    {
        if (!PriorityState.HasActivePriorities())
        {
            return;
        }

        if (__result)
        {
            return;
        }

        if (PriorityWorkIndex.HasPrioritiesForAnyMap(__instance))
        {
            __result = true;
        }
    }
}
