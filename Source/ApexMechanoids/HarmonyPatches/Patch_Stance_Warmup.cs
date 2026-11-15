using HarmonyLib;
using Verse;
using Verse.AI;

namespace ApexMechanoids
{
    [HarmonyPatch(typeof(Stance_Warmup), "StanceTick")]
    static class Patch_Stance_Warmup_StanceTick
    {
        static void Postfix(Stance_Warmup __instance)
        {
            Verb verb = Traverse.Create(__instance).Field("verb").GetValue<Verb>();
            if (verb is Verb_ShootSunBeamAbility sunBeamVerb)
            {
                sunBeamVerb.WarmupSoundTick(__instance.ticksLeft);
            }
        }
    }
}
