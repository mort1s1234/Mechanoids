using RimWorld;
using Verse;
using Verse.AI;

namespace ApexMechanoids
{
	public class JobGiver_Tinker : ThinkNode_JobGiver
	{
		public override Job TryGiveJob(Pawn pawn)
		{
			if (pawn.Drafted || !TinkerRepairUtility.CanDoTinkerRepair(pawn))
			{
				return null;
			}
			// Mechs first. A wall at half hit points costs the colony nothing until something walks
			// through it; a mech at half hit points loses the next fight, and the tinker is the only
			// thing on the map that can put it back together in the field. Buildings are only looked
			// for when there is no mech to fix, so this is still one map wide scan and not two.
			Thing mech = TinkerRepairUtility.FindRepairableMech(pawn);
			if (mech != null)
			{
				return JobMaker.MakeJob(ApexDefsOf.APM_RepairMech, mech);
			}
			Thing building = TinkerRepairUtility.FindRepairableBuilding(pawn);
			if (building != null)
			{
				return JobMaker.MakeJob(JobDefOf.Repair, building);
			}
			return null;
		}
	}
}
