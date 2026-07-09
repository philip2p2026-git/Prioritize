using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Prioritize.HarmonyPatches;

[HarmonyPatch(typeof(GenClosest), nameof(GenClosest.ClosestThing_Global_Reachable))]
public static class GenClosest_RegionalPrioritizedSearch
{
    private const int DefaultMinRegions = 24;
    private const int DefaultMaxRegions = 30;

    public static bool Prefix(
        IntVec3 center,
        Map map,
        IEnumerable<Thing> searchSet,
        PathEndMode peMode,
        TraverseParms traverseParams,
        float maxDistance,
        Predicate<Thing> validator,
        Func<Thing, float> priorityGetter,
        ref Thing __result)
    {
        if (priorityGetter == null || !PriorityState.HasActivePriorities() || traverseParams.pawn == null ||
            !TryGetRegionalThingRequest(searchSet, out var thingReq))
        {
            return true;
        }

        __result = GenClosest.ClosestThing_Regionwise_ReachablePrioritized(
            center,
            map,
            thingReq,
            peMode,
            traverseParams,
            maxDistance,
            validator,
            priorityGetter,
            DefaultMinRegions,
            DefaultMaxRegions);

        return false;
    }

    private static bool TryGetRegionalThingRequest(IEnumerable<Thing> searchSet, out ThingRequest thingReq)
    {
        thingReq = ThingRequest.ForGroup(ThingRequestGroup.Undefined);

        if (searchSet == null)
        {
            return false;
        }

        foreach (var thing in searchSet)
        {
            if (!thing.Spawned)
            {
                continue;
            }

            if (thing is IConstructible)
            {
                thingReq = ThingRequest.ForGroup(ThingRequestGroup.Construction);
                return thingReq.CanBeFoundInRegion;
            }

            if (thing.def.category == ThingCategory.Item)
            {
                thingReq = ThingRequest.ForGroup(ThingRequestGroup.HaulableEver);
                return thingReq.CanBeFoundInRegion;
            }

            thingReq = ThingRequest.ForDef(thing.def);
            return thingReq.CanBeFoundInRegion;
        }

        return false;
    }
}
