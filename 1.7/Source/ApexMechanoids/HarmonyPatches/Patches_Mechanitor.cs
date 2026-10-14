using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ApexMechanoids
{
    [HarmonyPatch(typeof(Pawn_MechanitorTracker), "CanControlMechs", MethodType.Getter)]
    public class Patch_Pawn_MechanitorTracker_CanControlMechs
    {
        public static void Postfix(Pawn_MechanitorTracker __instance, ref AcceptanceReport __result)
        {
            if (!__result)
            {
                if (Utils.IsUplinkActiveFor(__instance.Pawn))
                {
                    __result = true;
                    return;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_MechanitorTracker), nameof(Pawn_MechanitorTracker.CanCommandTo))]
    public static class Patch_Pawn_MechanitorTracker_CanCommandTo
    {
        public static void Postfix(Pawn_MechanitorTracker __instance, ref bool __result)
        {
            if(__result == false)
            {
                if (__instance.pawn != null && Utils.IsUplinkActiveFor(__instance.Pawn))
                {
                    __result = true;
                }
            }
        }
    }


    [HarmonyPatch(typeof(MechanitorUtility), "InMechanitorCommandRange")]
    public class Patch_MechanitorUtility_InMechanitorCommandRange
    {
        public static void Postfix(Pawn mech, LocalTargetInfo target, ref bool __result)
        {
            if (__result)
            {
                return;
            }
            if (mech.HasComp<CompMechanitorRangeExtender>())
            {
                __result = true;
                return;
            }
            Pawn overseer = mech.GetOverseer();
            if (Utils.IsUplinkActiveFor(overseer))
            {
                __result = true;
                return;
            }
            List<Pawn> ops = mech.GetOverseer()?.mechanitor?.OverseenPawns;
            if (ops.NullOrEmpty())
            {
                return;
            }
            foreach (Pawn p in ops.Where((Pawn x) => x.Spawned && x.MapHeld == mech.MapHeld))
            {
                if (p.TryGetComp<CompMechanitorRangeExtender>(out var c) && (((LocalTargetInfo)p).Cell.DistanceToSquared(target.Cell) <= c.SquaredDistance))
                {
                    __result = true;
                    return;
                }
            }
        }
    }
 
}
