namespace ApexMechanoids
{
    /// <summary>
    /// Whether two duelists are still in their duel.
    ///
    /// A pawn being pulled by a Terminus hook, or jumping, spends a second or so inside a PawnFlyer.
    /// It is not spawned for that time, and the duel used to read that as the pawn having left, so
    /// a hook ended the duel the tick it landed on either duelist. Vanilla still ticks the pawn in
    /// the air, so the duel is asked about it every tick of the flight. In the air now counts as
    /// still here; everything else that ended a duel still does.
    ///
    /// Kept free of Verse so the table can be run outside the game.
    /// </summary>
    public static class DuelPresenceRules
    {
        /// <summary>
        /// Whether one duelist is still in the fight. The flyer has to be on the map itself, so a
        /// pawn in the air counts and one packed into a caravan or a shuttle does not.
        /// </summary>
        public static bool IsPresent(bool gone, bool downed, bool spawned, bool inSpawnedFlyer)
        {
            return !gone && !downed && (spawned || inSpawnedFlyer);
        }

        public static bool CanKeepDueling(bool selfPresent, bool opponentPresent, bool sameMap, bool opponentIsSelf)
        {
            return selfPresent && opponentPresent && sameMap && !opponentIsSelf;
        }
    }
}
