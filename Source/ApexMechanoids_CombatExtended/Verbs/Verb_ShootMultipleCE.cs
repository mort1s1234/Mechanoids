using CombatExtended;
using System.Linq;
using UnityEngine;
using Verse;

namespace ApexMechanoids.CombatExtended
{
    /// <summary>
    /// CE counterpart to Verb_ShootMultiple. The Maul gains primary burst shots as
    /// it is damaged and may launch additional uncontrolled shots around its target.
    /// </summary>
    public class Verb_ShootMultipleCE : Verb_ShootCE
    {
        private const int MaximumOverclockShots = 10;
        private const int SecondaryShotRadius = 3;
        private const float StopSecondaryShotsChance = 0.5f;

        private int OverclockShotCount
        {
            get
            {
                Pawn pawn = CasterPawn;
                if (pawn?.health?.summaryHealth == null)
                {
                    return 0;
                }

                float missingHealth = 1f - pawn.health.summaryHealth.SummaryHealthPercent;
                return Mathf.Clamp((int)(missingHealth * MaximumOverclockShots), 0, MaximumOverclockShots);
            }
        }

        public override int ShotsPerBurst => base.ShotsPerBurst + OverclockShotCount;

        public override bool TryCastShot()
        {
            if (!base.TryCastShot())
            {
                return false;
            }

            int secondaryShots = OverclockShotCount;
            for (int i = 0; i < secondaryShots; i++)
            {
                if (Rand.Chance(StopSecondaryShotsChance))
                {
                    break;
                }

                LaunchSecondaryProjectile(RandomSecondaryTarget());
            }

            return true;
        }

        private LocalTargetInfo RandomSecondaryTarget()
        {
            IntVec3 targetCell;
            do
            {
                targetCell = CurrentTarget.Cell + new IntVec3(
                    Rand.Range(-SecondaryShotRadius, SecondaryShotRadius),
                    0,
                    Rand.Range(-SecondaryShotRadius, SecondaryShotRadius));
            }
            while (targetCell == CurrentTarget.Cell);

            return new LocalTargetInfo(targetCell);
        }

        private void LaunchSecondaryProjectile(LocalTargetInfo target)
        {
            ProjectilePropertiesCE projectileProperties = projectilePropsCE;
            if (projectileProperties == null || Caster?.Map == null)
            {
                return;
            }

            ProjectileCE projectile = SpawnProjectile();
            GenSpawn.Spawn(projectile, Caster.Position, Caster.Map);

            Vector2 origin = sourceLoc;
            Vector3 origin3 = new Vector3(origin.x, ShotHeight, origin.y);
            Vector3 targetPosition = target.Cell.ToVector3Shifted();
            float targetDistance = (new Vector2(targetPosition.x, targetPosition.z) - origin).magnitude;
            float angle = projectileProperties.TrajectoryWorker.ShotAngle(projectileProperties, origin3, targetPosition);
            float rotation = projectileProperties.TrajectoryWorker.ShotRotation(projectileProperties, origin3, targetPosition);

            projectile.ExactPosition = origin;
            projectile.canTargetSelf = false;
            projectile.minCollisionDistance = GetMinCollisionDistance(targetDistance);
            projectile.intendedTarget = target;
            projectile.mount = Caster.Position.GetThingList(Caster.Map).FirstOrDefault(thing => thing is Pawn && thing != Caster);
            projectile.AccuracyFactor = 1f;
            projectile.Launch(
                Shooter,
                origin,
                angle,
                rotation,
                ShotHeight,
                ShotSpeed,
                EquipmentSource,
                targetDistance);
        }
    }
}
