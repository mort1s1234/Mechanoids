using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace ApexMechanoids
{
    public static class GroundTargetLineOfSight
    {
        public static LocalTargetInfo Resolve(Verb verb, LocalTargetInfo targ)
        {
            IntVec3 cell = targ.Cell;
            if (verb?.verbProps == null || !verb.verbProps.requireLineOfSight)
            {
                return cell;
            }
            if (!(targ.Thing is Pawn targetPawn) || !targetPawn.Spawned)
            {
                return cell;
            }
            Thing caster = verb.Caster;
            if (caster == null || !caster.Spawned || caster.Map != targetPawn.Map)
            {
                return cell;
            }
            if (verb.TryFindShootLineFromTo(caster.Position, new LocalTargetInfo(cell), out ShootLine _))
            {
                return cell;
            }
            if (verb.TryFindShootLineFromTo(caster.Position, targ, out ShootLine leanLine) && leanLine.Dest.IsValid && leanLine.Dest != cell)
            {
                return leanLine.Dest;
            }
            return cell;
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.TryStartAttack))]
    internal static class Patch_Pawn_TryStartAttack_GroundTarget
    {
        private static bool Prefix(Pawn __instance, LocalTargetInfo targ, ref bool __result)
        {
            if (!(targ.Thing is Pawn))
            {
                return true;
            }
            if (__instance.stances == null || __instance.stances.FullBodyBusy || __instance.WorkTagIsDisabled(WorkTags.Violent))
            {
                return true;
            }
            Verb verb = __instance.TryGetAttackVerb(targ.Thing, !__instance.IsColonist);
            if (verb?.verbProps == null || !verb.verbProps.ai_RangedAlawaysShootGroundBelowTarget)
            {
                return true;
            }
            LocalTargetInfo resolved = GroundTargetLineOfSight.Resolve(verb, targ);
            if (resolved.Cell == targ.Cell)
            {
                return true;
            }
            __result = verb.TryStartCastOn(resolved);
            return false;
        }
    }

    [HarmonyPatch]
    internal static class Patch_Verb_TryStartCastOn_GroundBeam
    {
        private static readonly Type[] Signature =
        {
            typeof(LocalTargetInfo), typeof(LocalTargetInfo), typeof(bool), typeof(bool), typeof(bool), typeof(bool)
        };

        private static IEnumerable<MethodBase> TargetMethods()
        {
            HashSet<MethodBase> methods = new HashSet<MethodBase>();
            MethodInfo beamMethod = AccessTools.DeclaredMethod(typeof(Verb_ShootBeam), "TryStartCastOn", Signature);
            if (beamMethod != null)
            {
                methods.Add(beamMethod);
            }
            foreach (Type type in typeof(Verb).AllSubclassesNonAbstract())
            {
                if (type.ContainsGenericParameters)
                {
                    continue;
                }
                MethodInfo method = AccessTools.DeclaredMethod(type, "TryStartCastOn", Signature);
                if (method != null && !method.ContainsGenericParameters)
                {
                    methods.Add(method);
                }
            }
            return methods;
        }

        private static void Prefix(Verb __instance, ref LocalTargetInfo castTarg)
        {
            if (__instance?.verbProps == null || !__instance.verbProps.beamTargetsGround)
            {
                return;
            }
            if (!(castTarg.Thing is Pawn))
            {
                return;
            }
            castTarg = GroundTargetLineOfSight.Resolve(__instance, castTarg);
        }
    }
}
