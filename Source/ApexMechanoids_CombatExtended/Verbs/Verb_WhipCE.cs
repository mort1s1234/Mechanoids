using CombatExtended;
using Verse;

namespace ApexMechanoids.CombatExtended
{
    /// <summary>
    /// Supplies CE's sharp penetration for Apex's custom area whip verb. The corresponding
    /// blunt penetration is carried by APM_Whip.defaultArmorPenetration for CE deflections.
    /// </summary>
    public class Verb_WhipCE : Verb_Whip
    {
        protected override float GetArmorPenetration(Tool tool, float damageAmount)
        {
            return tool is ToolCE toolCE
                ? toolCE.armorPenetrationSharp
                : base.GetArmorPenetration(tool, damageAmount);
        }
    }
}
