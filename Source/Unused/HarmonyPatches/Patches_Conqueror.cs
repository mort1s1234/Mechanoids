using HarmonyLib;
using RimWorld;
using Verse;

namespace ApexMechanoids
{
    [HarmonyPatch(typeof(Pawn), "SpawnSetup")]
    public static class Patch_ConquerorSpawnName
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn __instance, bool respawningAfterLoad)
        {
            if (respawningAfterLoad)
                return;
            if (!__instance.def.defName.StartsWith("APM_Mech_Conqueror"))
                return;
            ThingWithComps primary = __instance.equipment?.Primary;
            if (primary == null)
                return;
            __instance.Name = new NameSingle(primary.def.label + " conqueror");
        }
    }
}
