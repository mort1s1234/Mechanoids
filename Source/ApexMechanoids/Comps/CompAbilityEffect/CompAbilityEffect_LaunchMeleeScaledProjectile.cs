using RimWorld;
using Verse;

namespace ApexMechanoids
{
    public class CompAbilityEffect_LaunchMeleeScaledProjectile : CompAbilityEffect_LaunchProjectile
    {
        public override string ExtraTooltipPart()
        {
            ThingDef projectileDef = Props.projectileDef;
            if (projectileDef?.projectile == null)
            {
                return null;
            }
            int damage = UnityEngine.Mathf.RoundToInt(MeleeScaledProjectileUtility.CalculateDamage(projectileDef, parent.pawn));
            float armorPenetration = MeleeScaledProjectileUtility.CalculateArmorPenetration(projectileDef, parent.pawn);
            return "Damage".Translate() + ": " + damage + "\n" + "ArmorPenetration".Translate() + ": " + armorPenetration.ToStringPercent();
        }
    }
}
