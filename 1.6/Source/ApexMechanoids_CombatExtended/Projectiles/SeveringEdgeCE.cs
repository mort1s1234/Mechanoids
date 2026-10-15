using System;
using CombatExtended;
using Verse;

namespace ApexMechanoids.CombatExtended
{
    /// <summary>
    /// Runs Severing Edge impacts with the same CE melee verb used to scale their damage.
    /// CE can therefore obtain both sharp penetration and deflected blunt penetration from
    /// the wielder's actual ToolCE, including its normal CE multipliers.
    /// </summary>
    public class SeveringEdgeCE : SeveringEdge
    {
        private Verb_MeleeAttackCE StrongestMeleeVerbCE
        {
            get
            {
                Pawn pawn = launcher as Pawn;
                return pawn == null
                    ? null
                    : MeleeScaledProjectileUtility.FindStrongestMeleeVerb(pawn) as Verb_MeleeAttackCE;
            }
        }

        public override float ArmorPenetration
        {
            get
            {
                Verb_MeleeAttackCE verb = StrongestMeleeVerbCE;
                return verb?.ArmorPenetrationSharp ?? base.ArmorPenetration;
            }
        }

        protected override void Impact(Thing thing)
        {
            Verb_MeleeAttackCE verb = StrongestMeleeVerbCE;
            if (verb == null)
            {
                base.Impact(thing);
                return;
            }

            MeleePenetrationContext.Run(verb, () => base.Impact(thing));
        }

        private sealed class MeleePenetrationContext : Verb_MeleeAttackCE
        {
            internal static void Run(Verb_MeleeAttackCE verb, Action impact)
            {
                Verb_MeleeAttackCE previousVerb = LastAttackVerb;
                LastAttackVerb = verb;
                try
                {
                    impact();
                }
                finally
                {
                    LastAttackVerb = previousVerb;
                }
            }
        }
    }
}
