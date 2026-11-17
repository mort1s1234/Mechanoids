using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ApexMechanoids
{
    [StaticConstructorOnStartup]
    public static class RepairStationUtility
    {
        private static readonly List<ThingDef> repairStationDefs = CollectRepairStationDefs();

        private static List<ThingDef> CollectRepairStationDefs()
        {
            List<ThingDef> stationDefs = new List<ThingDef>();
            List<ThingDef> allThingDefs = DefDatabase<ThingDef>.AllDefsListForReading;
            for (int defIndex = 0; defIndex < allThingDefs.Count; defIndex++)
            {
                ThingDef thingDef = allThingDefs[defIndex];
                if (thingDef.thingClass != null && typeof(Building_RepairStation).IsAssignableFrom(thingDef.thingClass))
                {
                    stationDefs.Add(thingDef);
                }
            }
            return stationDefs;
        }

        public static IEnumerable<Thing> RepairStationsOnMap(Map map)
        {
            for (int defIndex = 0; defIndex < repairStationDefs.Count; defIndex++)
            {
                List<Thing> stations = map.listerThings.ThingsOfDef(repairStationDefs[defIndex]);
                for (int stationIndex = 0; stationIndex < stations.Count; stationIndex++)
                {
                    yield return stations[stationIndex];
                }
            }
        }

        public static bool AnyStationAwaitingPawn(Map map, Pawn awaitedPawn = null)
        {
            for (int defIndex = 0; defIndex < repairStationDefs.Count; defIndex++)
            {
                List<Thing> stations = map.listerThings.ThingsOfDef(repairStationDefs[defIndex]);
                for (int stationIndex = 0; stationIndex < stations.Count; stationIndex++)
                {
                    Pawn selectedPawn = (stations[stationIndex] as Building_RepairStation)?.SelectedPawn;
                    if (selectedPawn == null) continue;
                    if (awaitedPawn == null || selectedPawn == awaitedPawn) return true;
                }
            }
            return false;
        }
    }
}
