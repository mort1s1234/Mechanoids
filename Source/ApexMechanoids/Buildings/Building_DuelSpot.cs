using RimWorld;
using Verse;

namespace ApexMechanoids
{
    public class Building_DuelSpot : Building
    {
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (mode == DestroyMode.KillFinalize && Spawned)
            {
                PlayBurnoutEffect(Map);
            }

            base.Destroy(DestroyMode.Vanish);
        }

        private void PlayBurnoutEffect(Map map)
        {
            EffecterDef burnoutEffecter = def.building?.destroyEffecter;
            if (burnoutEffecter == null || Position.Fogged(map))
            {
                return;
            }

            Effecter effecter = burnoutEffecter.Spawn(Position, map);
            effecter.Trigger(new TargetInfo(Position, map), TargetInfo.Invalid);
            effecter.Cleanup();
        }
    }
}
