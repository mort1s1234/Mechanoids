using ApexMechanoids;
using CombatExtended;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace ApexMechanoids.CombatExtended
{
    /// <summary>
    /// Keeps the Ravager's artillery toggle and ordered-job behavior while using
    /// CE's mortar aiming, spotting, trajectory, suppression, and collision path.
    /// </summary>
    public class Verb_RavagerArtilleryCE : Verb_ShootMortarCE
    {
        public override bool TryStartCastOn(LocalTargetInfo castTarg, LocalTargetInfo destTarg, bool surpriseAttack = false, bool canHitNonTargetPawns = true, bool preventFriendlyFire = false, bool nonInterruptingSelfCast = false)
        {
            if (CasterIsPawn
                && RavagerArtilleryUtility.IsPlayerControlled(CasterPawn)
                && !RavagerArtilleryUtility.AutoFireEnabled(CasterPawn)
                && !RavagerArtilleryUtility.IsManualArtilleryJob(CasterPawn))
            {
                return false;
            }

            LocalTargetInfo targetCell = RavagerArtilleryUtility.TargetCell(castTarg);
            if (CasterIsPawn && !RavagerArtilleryUtility.CanFireAtCell(CasterPawn, targetCell, this))
            {
                return false;
            }

            LocalTargetInfo destinationCell = destTarg.IsValid
                ? RavagerArtilleryUtility.TargetCell(destTarg)
                : targetCell;
            return base.TryStartCastOn(targetCell, destinationCell, surpriseAttack, canHitNonTargetPawns, preventFriendlyFire, nonInterruptingSelfCast);
        }

        public override void OrderForceTarget(LocalTargetInfo target)
        {
            if (!CasterIsPawn)
            {
                base.OrderForceTarget(target);
                return;
            }

            LocalTargetInfo targetCell = RavagerArtilleryUtility.TargetCell(target);
            float minimumRange = verbProps.EffectiveMinRange(targetCell, CasterPawn);
            if (CasterPawn.Position.DistanceToSquared(targetCell.Cell) < minimumRange * minimumRange
                && CasterPawn.Position.AdjacentTo8WayOrInside(targetCell.Cell))
            {
                Messages.Message("MessageCantShootInMelee".Translate(), CasterPawn, MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (!RavagerArtilleryUtility.CanFireAtCell(CasterPawn, targetCell, this))
            {
                Messages.Message("CannotHitTarget".Translate(), CasterPawn, MessageTypeDefOf.RejectInput, false);
                return;
            }

            Job job = RavagerArtilleryUtility.MakeArtilleryAttackJob(targetCell, this);
            job.playerForced = true;
            job.endIfCantShootInMelee = true;
            CasterPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }
    }

    /// <summary>
    /// CE launch bridge for Starfall. The source ability's targeting and AI remain
    /// unchanged; only projectile creation and trajectory calculation are replaced.
    /// </summary>
    public class CompAbilityEffect_StarfallCE : CompAbilityEffect_Starfall
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn caster = parent.pawn;
            Map map = caster?.MapHeld;
            LocalTargetInfo selectedTarget = target.IsValid ? target : dest;
            ThingDef projectileDef = Props.carrierProjectileDef;
            ProjectilePropertiesCE projectileProperties = projectileDef?.projectile as ProjectilePropertiesCE;
            if (caster == null
                || map == null
                || !caster.Spawned
                || !selectedTarget.IsValid
                || projectileProperties == null)
            {
                return;
            }

            ProjectileCE projectile = ThingMaker.MakeThing(projectileDef) as ProjectileCE;
            if (projectile == null)
            {
                return;
            }

            Vector3 source = caster.TrueCenter();
            source.y = new CollisionVertical(caster).shotHeight;
            Vector3 targetPosition = selectedTarget.HasThing
                ? selectedTarget.Thing.TrueCenter()
                : selectedTarget.Cell.ToVector3Shifted();
            if (selectedTarget.HasThing)
            {
                targetPosition.y = new CollisionVertical(selectedTarget.Thing).shotHeight;
            }

            float shotAngle = projectileProperties.TrajectoryWorker.ShotAngle(projectileProperties, source, targetPosition);
            float shotRotation = projectileProperties.TrajectoryWorker.ShotRotation(projectileProperties, source, targetPosition);
            float distance = (targetPosition - source).Yto0().magnitude;
            Thing equipment = caster.equipment?.Primary;

            GenSpawn.Spawn(projectile, caster.PositionHeld, map);
            projectile.ExactPosition = new Vector2(source.x, source.z);
            projectile.canTargetSelf = false;
            projectile.minCollisionDistance = 1f;
            projectile.intendedTarget = selectedTarget;
            projectile.AccuracyFactor = 1f;
            projectile.Launch(
                caster,
                new Vector2(source.x, source.z),
                shotAngle,
                shotRotation,
                source.y,
                projectileProperties.speed,
                equipment,
                distance);

            if (!Props.launchSoundDefName.NullOrEmpty())
            {
                DefDatabase<SoundDef>.GetNamedSilentFail(Props.launchSoundDefName)
                    ?.PlayOneShot(new TargetInfo(caster.PositionHeld, map));
            }
        }
    }

    /// <summary>
    /// CE carrier for Starfall. It follows CE ballistics until the configured
    /// progress point, then launches three CE explosive fragments.
    /// </summary>
    public class Projectile_StarfallCarrierCE : ProjectileCE
    {
        private const float SplitSpawnSeparation = 0.42f;
        private const float CenterForwardOffset = 0.18f;

        private bool splitOccurred;

        public override void Tick()
        {
            base.Tick();
            if (!Destroyed && !splitOccurred && ShouldSplitNow())
            {
                SplitNow();
                Destroy(DestroyMode.Vanish);
            }
        }

        public override void Impact(Thing hitThing)
        {
            if (!splitOccurred)
            {
                SplitNow();
            }

            if (!Destroyed)
            {
                Destroy(DestroyMode.Vanish);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref splitOccurred, nameof(splitOccurred));
        }

        private bool ShouldSplitNow()
        {
            DefModExtension_StarfallCarrier extension = def.GetModExtension<DefModExtension_StarfallCarrier>();
            if (extension == null || startingTicksToImpact <= 0f)
            {
                return false;
            }

            float progress = 1f - ticksToImpact / startingTicksToImpact;
            return progress >= Mathf.Clamp01(extension.splitAfterProgress);
        }

        private void SplitNow()
        {
            if (splitOccurred)
            {
                return;
            }

            splitOccurred = true;
            DefModExtension_StarfallCarrier extension = def.GetModExtension<DefModExtension_StarfallCarrier>();
            if (extension?.splitProjectileDef == null || Map == null || launcher == null)
            {
                return;
            }

            IntVec3 centerCell = intendedTarget.IsValid ? intendedTarget.Cell : new IntVec3(Destination);
            Vector3 center = centerCell.ToVector3Shifted();
            Vector3 splitOrigin = ExactPosition;
            Vector3 direction = (center - splitOrigin).Yto0();
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = Vector3.right;
            }

            direction.Normalize();
            Vector3 side = new Vector3(-direction.z, 0f, direction.x);
            FleckMaker.Static(splitOrigin, Map, FleckDefOf.ExplosionFlash, 1.6f);

            LaunchSplitProjectile(OffsetCell(center + RandomForwardOffset(direction, extension), extension), splitOrigin + direction * CenterForwardOffset, extension);
            LaunchSplitProjectile(RandomizedSideTarget(center, direction, side, extension, true), splitOrigin + side * SplitSpawnSeparation, extension);
            LaunchSplitProjectile(RandomizedSideTarget(center, direction, side, extension, false), splitOrigin - side * SplitSpawnSeparation, extension);
        }

        private IntVec3 RandomizedSideTarget(Vector3 center, Vector3 direction, Vector3 side, DefModExtension_StarfallCarrier extension, bool rightSide)
        {
            float sideDistance = extension.splitSpreadDistance
                + Rand.Range(-extension.splitSpreadJitter, extension.splitSpreadJitter);
            sideDistance = Mathf.Max(sideDistance, 0.4f);
            float sideSign = rightSide ? 1f : -1f;
            Vector3 target = center
                + side * sideSign * sideDistance
                + RandomForwardOffset(direction, extension);
            return OffsetCell(target, extension);
        }

        private static Vector3 RandomForwardOffset(Vector3 direction, DefModExtension_StarfallCarrier extension)
        {
            return direction * Rand.Range(-extension.splitForwardJitter, extension.splitForwardJitter);
        }

        private IntVec3 OffsetCell(Vector3 target, DefModExtension_StarfallCarrier extension)
        {
            target += new Vector3(
                Rand.Range(-extension.splitTargetJitter, extension.splitTargetJitter),
                0f,
                Rand.Range(-extension.splitTargetJitter, extension.splitTargetJitter));
            IntVec3 cell = target.ToIntVec3();
            return cell.InBounds(Map)
                ? cell
                : intendedTarget.IsValid ? intendedTarget.Cell : new IntVec3(Destination);
        }

        private void LaunchSplitProjectile(IntVec3 targetCell, Vector3 splitOrigin, DefModExtension_StarfallCarrier extension)
        {
            ThingDef projectileDef = extension.splitProjectileDef;
            ProjectilePropertiesCE projectileProperties = projectileDef?.projectile as ProjectilePropertiesCE;
            if (projectileProperties == null)
            {
                return;
            }

            ProjectileCE projectile = ThingMaker.MakeThing(projectileDef) as ProjectileCE;
            if (projectile == null)
            {
                return;
            }

            Vector3 source = splitOrigin;
            source.y = Mathf.Max(ExactPosition.y, 0.1f);
            Vector3 target = targetCell.ToVector3Shifted();
            float shotAngle = projectileProperties.TrajectoryWorker.ShotAngle(projectileProperties, source, target);
            float shotRotation = projectileProperties.TrajectoryWorker.ShotRotation(projectileProperties, source, target);
            float distance = (target - source).Yto0().magnitude;
            LocalTargetInfo targetInfo = new LocalTargetInfo(targetCell);

            GenSpawn.Spawn(projectile, splitOrigin.ToIntVec3(), Map);
            projectile.ExactPosition = new Vector2(splitOrigin.x, splitOrigin.z);
            projectile.canTargetSelf = false;
            projectile.minCollisionDistance = 0f;
            projectile.intendedTarget = targetInfo;
            projectile.AccuracyFactor = 1f;
            projectile.Launch(
                launcher,
                new Vector2(splitOrigin.x, splitOrigin.z),
                shotAngle,
                shotRotation,
                source.y,
                projectileProperties.speed,
                equipment,
                distance);
        }
    }
}
