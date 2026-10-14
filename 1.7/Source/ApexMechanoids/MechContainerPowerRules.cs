namespace ApexMechanoids
{
    /// <summary>What a stasis container is doing with its occupant right now.</summary>
    public enum ContainerPowerState
    {
        /// <summary>Powered, or holding nothing that can fall out. Nothing is counting down.</summary>
        Sealed,

        /// <summary>The power is gone and the container is running the preservation cycle on reserves.</summary>
        Draining,

        /// <summary>The reserves are out. Whatever is inside comes out now.</summary>
        Releasing,
    }

    /// <summary>
    /// What losing power does to a stasis container that has a mech walked into it.
    ///
    /// Kept free of Verse so the table can be run outside the game. The countdown and the two flags
    /// either side of it are the whole of the behaviour; reading a power comp is not the part that
    /// goes wrong.
    /// </summary>
    public static class MechContainerPowerRules
    {
        /// <summary>
        /// How long a container keeps its occupant after the power goes.
        ///
        /// Vanilla's solar flare runs 0.15 to 0.5 days (SolarFlare IncidentDef, durationDays), so
        /// half a day, 30000 ticks, is the longest one there is. Sitting a little over that is the
        /// point: a flare can no longer empty every container on the map at once, while a grid
        /// somebody actually let fail still does, and the 150W is still worth paying.
        /// </summary>
        public const int DefaultPowerLossGraceTicks = 33000;

        /// <summary>
        /// The countdown to run with, given whatever the container is carrying.
        ///
        /// A negative value is a container that has never counted anything down: one just built, one
        /// loaded from a save written before any of this existed, or one that had a mech walked into
        /// it and lost power in the same moment. All three start on full reserves. Without this the
        /// unset value reads as "reserves already spent" and the container empties on its first
        /// unpowered tick, which is the behaviour this whole change exists to remove.
        /// </summary>
        public static int NormalizeGraceTicks(int graceTicksLeft, int graceTicksTotal)
        {
            return graceTicksLeft < 0 ? graceTicksTotal : graceTicksLeft;
        }

        /// <summary>
        /// Whether the container is holding, draining, or letting go.
        ///
        /// A container is only ever draining while it is holding a mech somebody walked in there.
        /// One that was placed already sealed generates its occupant when it is opened rather than
        /// storing it, so there is nothing inside it for the power to drop.
        /// </summary>
        public static ContainerPowerState Resolve(bool holdingStoredMech, bool powerOn, int graceTicksLeft)
        {
            if (!holdingStoredMech || powerOn)
            {
                return ContainerPowerState.Sealed;
            }
            return graceTicksLeft > 0 ? ContainerPowerState.Draining : ContainerPowerState.Releasing;
        }

        /// <summary>
        /// Whether the mech inside may be taken out.
        ///
        /// Not while the power is off, which is the other half of what a solar flare should do here:
        /// the squad in reserve is unreachable for the duration rather than loose on the map.
        ///
        /// A container that has not stored anything is not covered by this. The ones found in ruins
        /// and the ones standing in the Buried Legacy base run on the same 150W and none of them
        /// will ever see any, so gating them on power would seal them shut for good.
        /// </summary>
        public static bool CanExtract(bool holdingStoredMech, bool powerOn)
        {
            return powerOn || !holdingStoredMech;
        }
    }
}
