using System;

namespace ApexMechanoids
{
    /// <summary>
    /// Who a siren may start singing at.
    ///
    /// Three of the four answers were already here; the fourth is the one that was missing. Nothing
    /// remembered that a pawn had just been lured, so the moment the song ended the same siren could
    /// start it again on the same pawn, and a pair of sirens could keep one colonist walking between
    /// them indefinitely. A pawn who has just been dragged across a firefight now gets a quiet spell
    /// before anything can compel them a second time.
    ///
    /// Kept free of Verse so the table can be run outside the game.
    /// </summary>
    public static class SirenLureTargetRules
    {
        /// <summary>
        /// How long a pawn is deaf to the song after one ends, on top of the song's own length.
        ///
        /// Comfortably longer than the ability's own 1000 tick cooldown, so the limit is the pawn
        /// rather than the caster and a second siren cannot pick up where the first left off.
        /// </summary>
        public const int DefaultQuietTicks = 2500;

        public static bool CanStartLure(bool canAffectTarget, bool alreadyBeingLured, bool recentlyLured, bool anotherSirenLuring)
        {
            return canAffectTarget && !alreadyBeingLured && !recentlyLured && !anotherSirenLuring;
        }

        /// <summary>
        /// How long the mark stays on the target, measured so that the quiet spell is what is left
        /// once the song itself has finished. Setting the quiet time therefore means what it says,
        /// rather than being eaten by however long the lure runs for.
        ///
        /// It is applied when the song starts rather than when it ends, so a lure that is cut short
        /// still buys the pawn its quiet spell. Being yanked halfway across a firefight and dropped
        /// is not a reason to be fair game again immediately.
        /// </summary>
        public static int MarkTicks(int lureDurationTicks, int quietTicks)
        {
            return Math.Max(1, lureDurationTicks) + Math.Max(0, quietTicks);
        }
    }
}
