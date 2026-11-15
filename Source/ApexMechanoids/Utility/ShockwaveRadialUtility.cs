using Verse;

namespace ApexMechanoids
{
    internal static class ShockwaveRadialUtility
    {
        public static int FirstRadialIndexAtOrBeyond(float minRadius)
        {
            if (minRadius <= 0f)
            {
                return 0;
            }

            int index = GenRadial.NumCellsInRadius(minRadius);
            while (index > 0 && GenRadial.RadialPatternRadii[index - 1] >= minRadius)
            {
                index--;
            }

            return index;
        }
    }
}
