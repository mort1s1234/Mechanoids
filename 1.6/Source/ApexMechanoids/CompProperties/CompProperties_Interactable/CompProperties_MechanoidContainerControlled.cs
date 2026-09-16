using Verse;

namespace ApexMechanoids
{
    public class CompProperties_MechanoidContainerControlled : CompProperties_MechanoidContainer
    {
        [MustTranslate]
        public string ChooseMechLabel;
        [MustTranslate]
        public string ChooseMechDesc;

        public JobDef enterJobDef;

        /// <summary>
        /// How long the container keeps a stored mech after the power goes, before the preservation
        /// cycle runs dry and drops it. See <see cref="MechContainerPowerRules"/> for why the
        /// default sits where it does.
        /// </summary>
        public int powerLossGraceTicks = MechContainerPowerRules.DefaultPowerLossGraceTicks;

        public CompProperties_MechanoidContainerControlled()
        {
            compClass = typeof(Comp_MechanoidContainerControlled);
        }
    }
}
