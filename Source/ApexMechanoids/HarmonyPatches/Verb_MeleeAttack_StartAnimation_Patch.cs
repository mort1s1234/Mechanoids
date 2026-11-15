using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApexMechanoids.HarmonyPatches
{
    [HarmonyLib.HarmonyPatch(typeof(Verb_MeleeAttack), nameof(Verb_MeleeAttack.TryCastShot))]
    internal static class Verb_MeleeAttack_StartAnimation_Patch
    {
        public static void Postfix(Verb_MeleeAttack __instance)
        {
            if (__instance.CasterIsPawn)
            {
                var ext = __instance.Caster.def.GetModExtension<DefModExtension_AnimationOnAttack>();
                if (ext != null && ext.animation != null)
                {
                    if (ext.maneuver == null || ext.maneuver == __instance.maneuver)
                    {
                        __instance.CasterPawn.Drawer.renderer.SetAnimation(ext.animation);
                    }
                }
            }
        }
    }
}
