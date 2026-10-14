using RimWorld;
using System.Collections.Generic;
using Verse;

namespace ApexMechanoids
{
    public static class MeleeScaledProjectileUtility
    {
        public static float CalculateDamage(ThingDef projectileDef, Thing launcher)
        {
            float flatDamage = projectileDef.projectile.GetDamageAmount(null);
            DefModExtension_MeleeScaledProjectile meleeScaling = projectileDef.GetModExtension<DefModExtension_MeleeScaledProjectile>();
            if (meleeScaling == null || !(launcher is Pawn launcherPawn))
            {
                return flatDamage;
            }
            Verb strongestMeleeVerb = FindStrongestMeleeVerb(launcherPawn);
            if (strongestMeleeVerb == null)
            {
                return flatDamage;
            }
            return strongestMeleeVerb.verbProps.AdjustedMeleeDamageAmount(strongestMeleeVerb, launcherPawn) * meleeScaling.meleeDamageMultiplier;
        }

        public static float CalculateArmorPenetration(ThingDef projectileDef, Thing launcher)
        {
            float flatArmorPenetration = projectileDef.projectile.GetArmorPenetration(null);
            if (!projectileDef.HasModExtension<DefModExtension_MeleeScaledProjectile>() || !(launcher is Pawn launcherPawn))
            {
                return flatArmorPenetration;
            }
            Verb strongestMeleeVerb = FindStrongestMeleeVerb(launcherPawn);
            if (strongestMeleeVerb == null)
            {
                return flatArmorPenetration;
            }
            return strongestMeleeVerb.verbProps.AdjustedArmorPenetration(strongestMeleeVerb, launcherPawn);
        }

        private static Verb FindStrongestMeleeVerb(Pawn attacker)
        {
            if (attacker.meleeVerbs == null)
            {
                return null;
            }
            Verb strongestVerb = null;
            float strongestDamage = 0f;
            List<VerbEntry> meleeVerbEntries = attacker.meleeVerbs.GetUpdatedAvailableVerbsList(false);
            for (int index = 0; index < meleeVerbEntries.Count; index++)
            {
                Verb meleeVerb = meleeVerbEntries[index].verb;
                if (meleeVerb?.verbProps == null || !meleeVerb.verbProps.IsMeleeAttack)
                {
                    continue;
                }
                float hitDamage = meleeVerb.verbProps.AdjustedMeleeDamageAmount(meleeVerb, attacker);
                if (hitDamage > strongestDamage)
                {
                    strongestDamage = hitDamage;
                    strongestVerb = meleeVerb;
                }
            }
            return strongestVerb;
        }
    }
}
