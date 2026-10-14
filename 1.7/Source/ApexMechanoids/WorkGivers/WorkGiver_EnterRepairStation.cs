using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;

namespace ApexMechanoids
{
    public class WorkGiver_EnterRepairStation : WorkGiver_EnterBuilding
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            if (!pawn.RaceProps.IsMechanoid) return true;
            if (pawn.Faction != Faction.OfPlayer) return true;
            return !RepairStationUtility.AnyStationAwaitingPawn(pawn.Map, pawn);
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return RepairStationUtility.RepairStationsOnMap(pawn.Map);
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!(t is Building_RepairStation station)) return false;
            if (station.SelectedPawn != pawn) return false;

            return base.HasJobOnThing(pawn, t, forced);
        }
    }
}
