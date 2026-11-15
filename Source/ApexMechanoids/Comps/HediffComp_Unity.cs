using RimWorld;
using System.Collections.Generic;
using Verse;

namespace ApexMechanoids
{
    public class CompProperties_Unity : CompProperties
    {
        public List<PawnKindDef> kindDefs = new List<PawnKindDef>();

        public HediffDef hediff;

        public int checkInterval = 250;

        public float severityPerAlly = 0.1f;

        public float maxSeverity = 0f;

        public float range = 0f;

        public CompProperties_Unity()
        {
            compClass = typeof(CompUnity);
        }
    }

    public class CompUnity : ThingComp
    {
        public CompProperties_Unity Props => (CompProperties_Unity)props;

        private Pawn Pawn => (Pawn)parent;

        private int tickOffset;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            tickOffset = Rand.Range(0, Props.checkInterval);
            if (!respawningAfterLoad && Props.hediff != null)
            {
                Hediff existing = Pawn.health.hediffSet.GetFirstHediffOfDef(Props.hediff);
                if (existing == null)
                {
                    Hediff hediff = HediffMaker.MakeHediff(Props.hediff, Pawn);
                    hediff.Severity = Props.hediff.initialSeverity;
                    Pawn.health.AddHediff(hediff);
                }
            }
        }

        public void ForceUpdateNow()
        {
            if (!Pawn.Spawned || Props.hediff == null)
            {
                return;
            }
            int allyCount = CountAllies();
            float targetSeverity = allyCount * Props.severityPerAlly;
            if (Props.maxSeverity > 0f && targetSeverity > Props.maxSeverity)
            {
                targetSeverity = Props.maxSeverity;
            }
            Hediff hediff = Pawn.health.hediffSet.GetFirstHediffOfDef(Props.hediff);
            if (targetSeverity <= 0f)
            {
                if (hediff == null)
                {
                    hediff = HediffMaker.MakeHediff(Props.hediff, Pawn);
                    hediff.Severity = Props.hediff.initialSeverity;
                    Pawn.health.AddHediff(hediff);
                }
                return;
            }
            if (hediff == null)
            {
                hediff = HediffMaker.MakeHediff(Props.hediff, Pawn);
                hediff.Severity = targetSeverity;
                Pawn.health.AddHediff(hediff);
            }
            else
            {
                hediff.Severity = targetSeverity;
            }
        }

        public override void CompTick()
        {
            base.CompTick();

            if (!Pawn.Spawned || Props.hediff == null)
            {
                return;
            }

            int currentTick = Find.TickManager.TicksGame;
            if ((currentTick + tickOffset) % Props.checkInterval != 0)
            {
                return;
            }

            int allyCount = CountAllies();
            float targetSeverity = allyCount * Props.severityPerAlly;

            if (Props.maxSeverity > 0f && targetSeverity > Props.maxSeverity)
            {
                targetSeverity = Props.maxSeverity;
            }

            Hediff hediff = Pawn.health.hediffSet.GetFirstHediffOfDef(Props.hediff);
            if (hediff == null)
            {
                hediff = HediffMaker.MakeHediff(Props.hediff, Pawn);
                hediff.Severity = targetSeverity > 0f ? targetSeverity : Props.hediff.initialSeverity;
                Pawn.health.AddHediff(hediff);
                return;
            }

            hediff.Severity = targetSeverity > 0f ? targetSeverity : Props.hediff.initialSeverity;
        }

        private int CountAllies()
        {
            Map map = Pawn.Map;
            bool useRange = Props.range > 0f;
            float rangeSq = Props.range * Props.range;
            Faction ownFaction = Pawn.Faction;

            int count = 0;
            IReadOnlyList<Pawn> allPawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < allPawns.Count; i++)
            {
                Pawn p = allPawns[i];
                if (p == Pawn || p.Dead || !p.Spawned)
                {
                    continue;
                }
                if (p.Faction != ownFaction)
                {
                    continue;
                }
                if (!IsCountedKind(p))
                {
                    continue;
                }
                if (useRange && p.Position.DistanceToSquared(Pawn.Position) > rangeSq)
                {
                    continue;
                }
                count++;
            }
            return count;
        }

        private bool IsCountedKind(Pawn p)
        {
            if (Props.kindDefs.Count == 0)
            {
                return p.kindDef == Pawn.kindDef;
            }
            for (int i = 0; i < Props.kindDefs.Count; i++)
            {
                if (p.kindDef == Props.kindDefs[i])
                {
                    return true;
                }
            }
            return false;
        }
    }
}
