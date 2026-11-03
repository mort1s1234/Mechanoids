using RimWorld;
using System.Collections.Generic;
using Verse;

namespace ApexMechanoids
{
    public class CompAbilityEffect_ConvertToBuilding : CompAbilityEffect
    {
        public new CompProperties_ConvertToBuilding Props => (CompProperties_ConvertToBuilding)props;

        public Pawn pawn => parent.pawn;
        public override void PostApplied(List<LocalTargetInfo> targets, Map map)
        {
            base.PostApplied(targets, map);
            Thing thing = ThingMaker.MakeThing(Props.thingDef);
            thing.SetFactionDirect(pawn.Faction);
            GenSpawn.Spawn(thing, pawn.Position, pawn.Map, thing.def.defaultPlacingRot);
            CompPowerPlantDynamo compPowerPlantDynamo = thing.TryGetComp<CompPowerPlantDynamo>();
            if (compPowerPlantDynamo != null)
            {
                compPowerPlantDynamo.SwithcToPowerPlantMode(pawn);
            }
        }

        public override bool CanCast
        {
            get
            {
                Map map = pawn.Map;
                if (!pawn.Spawned || map == null)
                {
                    return false;
                }

                foreach (IntVec3 cell in GenAdj.OccupiedRect(pawn.Position, Props.thingDef.defaultPlacingRot, Props.thingDef.size).Cells)
                {
                    if (!cell.Standable(map))
                    {
                        return false;
                    }
                }
                return base.CanCast;
            }
        }
    }
}
