namespace ApexMechanoids
{
    /// <summary>
    /// Whether a haze should leave its toxic mist for later because the ground is already covered.
    ///
    /// Nothing used to ask. Every haze with a flesh target in reach cast on its own 300 tick check,
    /// so a group standing together put three or four clouds on one spot. Thick gas under the haze
    /// is the obvious sign, but it is not the only one; the cloud comes out of its emitter over six
    /// seconds and the cast has a warmup before that, so a neighbour that has only just started is
    /// standing on clear ground. Any of the three now means leave it.
    ///
    /// Kept free of Verse so the table can be run outside the game.
    /// </summary>
    public static class ToxicMistCoverageRules
    {
        /// <summary>The disc round the haze whose gas is counted, 25 cells.</summary>
        public const float CoverageRadius = 2.9f;

        /// <summary>A quarter of full density, out of 255, counts a cell as gassed.</summary>
        public const byte GassedDensity = 64;

        /// <summary>
        /// How close a cloud still coming out, or a neighbour still casting, has to be. The same
        /// reach as the mist itself, so a cloud centred this close will end up on top of the haze.
        /// </summary>
        public const float NeighbourReach = 4.9f;

        /// <summary>
        /// Half the disc is the line; a cloud thinned to a few patches is worth topping up, one
        /// still covering half the ground round the haze is not.
        /// </summary>
        public static bool AlreadyCovered(int gassedCells, int checkedCells, bool emissionPendingNearby, bool neighbourCastingNearby)
        {
            return emissionPendingNearby
                || neighbourCastingNearby
                || (checkedCells > 0 && gassedCells * 2 >= checkedCells);
        }
    }
}
