using RimWorld;
using Verse;
using Verse.AI;

namespace ApexMechanoids
{
    public class JobGiver_Duel : ThinkNode_JobGiver
    {
        private const int OpponentInFlightWaitTicks = 15;

        public override Job TryGiveJob(Pawn pawn)
        {
            if (!Utils.CanRunAutonomousPawn(pawn))
            {
                return null;
            }

            Thing target = pawn.mindState?.enemyTarget;
            if (!DuelUtility.IsValidActiveDuelOpponent(pawn, target))
            {
                pawn.mindState?.mentalStateHandler?.CurState?.RecoverFromState();
                return null;
            }

            // The opponent is in the air on the end of a hook. Hold here rather than wander off,
            // and go after it again once it lands.
            if (DuelUtility.IsInFlight(target as Pawn))
            {
                return JobMaker.MakeJob(JobDefOf.Wait_Combat, OpponentInFlightWaitTicks, checkOverrideOnExpiry: true);
            }

            if (!pawn.CanReach(target, PathEndMode.Touch, Danger.Deadly))
            {
                return null;
            }

            Job job = JobMaker.MakeJob(JobDefOf.AttackMelee, target);
            job.maxNumMeleeAttacks = 1;
            job.expiryInterval = Rand.Range(420, 900);
            job.checkOverrideOnExpire = true;
            job.canBashDoors = true;
            return job;
        }
    }
}
