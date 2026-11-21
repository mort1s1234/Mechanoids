using RimWorld;
using UnityEngine;
using Verse;

namespace ApexMechanoids
{
    public class CompProperties_GazerBeamOriginOffsets : CompProperties
    {
        public Vector2 northOffset = Vector2.zero;
        public Vector2 eastOffset = Vector2.zero;
        public Vector2 southOffset = Vector2.zero;
        public Vector2 westOffset = Vector2.zero;
        public string fireCommandIconPath;
        public string cancelCommandIconPath;

        public CompProperties_GazerBeamOriginOffsets()
        {
            compClass = typeof(CompGazerBeamOriginOffsets);
        }

        public Vector2 GetOffset(Rot4 rotation)
        {
            if (rotation == Rot4.North)
            {
                return northOffset;
            }

            if (rotation == Rot4.East)
            {
                return eastOffset;
            }

            if (rotation == Rot4.South)
            {
                return southOffset;
            }

            return westOffset;
        }

        public Vector3 GetWorldOffset(Rot4 rotation)
        {
            Vector2 offset = GetOffset(rotation);
            return new Vector3(offset.x, 0f, offset.y);
        }

        public override void DrawGhost(IntVec3 center, Rot4 rot, ThingDef thingDef, Color ghostCol,
            AltitudeLayer drawAltitude, Thing thing = null)
        {
            VerbProperties beamProps = Building_GazerEmplacement.SunRayAbilityDef?.verbProperties;
            if (beamProps == null)
            {
                return;
            }

            Vector3 origin = GenThing.TrueCenter(center, rot, thingDef.Size,
                AltitudeLayer.MetaOverlays.AltitudeFor()) + GetWorldOffset(rot);
            float halfAngle = thingDef.GetModExtension<DefModExtension_GazerEmplacement>()
                ?.firingArcHalfAngle ?? 18f;

            Building_GazerEmplacement.DrawFiringArc(origin, rot, beamProps.range, halfAngle);
        }
    }

    public class CompGazerBeamOriginOffsets : ThingComp
    {
        public CompProperties_GazerBeamOriginOffsets Props
        {
            get { return (CompProperties_GazerBeamOriginOffsets)props; }
        }

        public Vector2 GetOffset(Rot4 rotation) => Props.GetOffset(rotation);

        public Vector3 GetWorldOffset(Rot4 rotation) => Props.GetWorldOffset(rotation);

        public Texture2D GetFireCommandIcon()
        {
            return GetIcon(Props.fireCommandIconPath);
        }

        public Texture2D GetCancelCommandIcon()
        {
            return GetIcon(Props.cancelCommandIconPath);
        }

        private Texture2D GetIcon(string path)
        {
            if (path.NullOrEmpty())
            {
                return null;
            }

            return ContentFinder<Texture2D>.Get(path, false);
        }
    }
}
