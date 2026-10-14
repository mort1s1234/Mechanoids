using Verse;
using Verse.Sound;

namespace ApexMechanoids
{
    public class DefModExtension_CelerusSmokescreenProjectile : DefModExtension
    {
        public ThingDef gasDef;
        public float maxRadius = 4.9f;
        public int spreadIntervalTicks = 15;
    }

    public class Projectile_CelerusSmokescreen : Projectile
    {
        public override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = Map;
            IntVec3 cell = Position;
            DefModExtension_CelerusSmokescreenProjectile extension = def.GetModExtension<DefModExtension_CelerusSmokescreenProjectile>();

            base.Impact(hitThing, blockedByShield);

            if (map == null || extension?.gasDef == null)
            {
                return;
            }

            if (def.projectile.soundExplode != null)
            {
                def.projectile.soundExplode.PlayOneShot(new TargetInfo(cell, map));
            }

            if (cell.GetGas(map) == null)
            {
                GenSpawn.Spawn(extension.gasDef, cell, map);
            }
            GasSpreadManager.StartSpread(cell, map, extension.gasDef, extension.maxRadius, extension.spreadIntervalTicks);
        }
    }
}
