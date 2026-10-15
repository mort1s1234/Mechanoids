using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using LudeonTK;
using RimWorld;
using Verse;

namespace ApexMechanoids
{
    /// <summary>
    /// Test helpers for exercising Apex mechanoids through RimWorld's real mech-cluster generator.
    /// Vanilla cluster buildings are retained because they provide the cluster's power, wake-up,
    /// and defensive behavior; only the pawn-kind selection is restricted to Apex Mechanoids.
    /// </summary>
    public static class ApexMechanoidDebugActions
    {
        private static readonly float[] ClusterPointOptions =
        {
            500f,
            1000f,
            2000f,
            5000f,
            10000f
        };

        internal static bool GeneratingApexOnlyCluster { get; private set; }

        [DebugAction("Apex Mechanoids", "Spawn Apex-only mech cluster", requiresBiotech: true,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static DebugActionNode SpawnApexOnlyMechCluster()
        {
            DebugActionNode root = new DebugActionNode();
            foreach (float points in ClusterPointOptions)
            {
                float selectedPoints = points;
                root.AddChild(new DebugActionNode(
                    $"{selectedPoints:0} points, click place",
                    DebugActionType.ToolMap,
                    () => SpawnAtMouse(selectedPoints)));
            }

            return root;
        }

        internal static bool IsApexPawnKind(PawnKindDef def)
        {
            // Apex owns this prefix across all of its concrete mech PawnKindDefs. Using the def
            // namespace is stable even when another mod copies or reparents a resolved def.
            return def?.defName != null
                && def.defName.StartsWith("APM_Mech_", StringComparison.Ordinal);
        }

        private static void SpawnAtMouse(float points)
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                return;
            }

            MechClusterSketch sketch = null;
            bool previousScope = GeneratingApexOnlyCluster;
            GeneratingApexOnlyCluster = true;
            try
            {
                // Vanilla has a chance to make a building-only cluster. A testing command needs
                // actual mechs, so allow a few bounded rerolls without affecting normal incidents.
                for (int attempt = 0; attempt < 8; attempt++)
                {
                    sketch = MechClusterGenerator.GenerateClusterSketch(
                        points,
                        map,
                        startDormant: true,
                        forceNoConditionCauser: false);
                    if (!sketch.pawns.NullOrEmpty())
                    {
                        break;
                    }
                }
            }
            finally
            {
                GeneratingApexOnlyCluster = previousScope;
            }

            if (sketch == null || sketch.pawns.NullOrEmpty())
            {
                Messages.Message("Could not generate an Apex mech cluster with pawns.", MessageTypeDefOf.RejectInput);
                return;
            }

            EnforceApexOnlyPawns(sketch);
            if (sketch.pawns.NullOrEmpty())
            {
                Messages.Message("No cluster-eligible Apex mech kinds are loaded.", MessageTypeDefOf.RejectInput);
                return;
            }

            MechClusterUtility.SpawnCluster(
                UI.MouseCell(),
                map,
                sketch,
                dropInPods: false,
                canAssaultColony: false,
                questTag: null);

            string spawnedKinds = string.Join(", ", sketch.pawns
                .GroupBy(mech => mech.kindDef.label)
                .OrderBy(group => group.Key)
                .Select(group => $"{group.Key} x{group.Count()}"));
            Log.Message($"[ApexMechanoids] Apex-only cluster spawned {sketch.pawns.Count} mech(s): {spawnedKinds}");
            Messages.Message($"Spawned {sketch.pawns.Count} Apex mech(s).", MessageTypeDefOf.TaskCompletion);
        }

        private static void EnforceApexOnlyPawns(MechClusterSketch sketch)
        {
            List<PawnKindDef> apexKinds = DefDatabase<PawnKindDef>.AllDefsListForReading
                .Where(def => IsApexPawnKind(def) && MechClusterGenerator.MechKindSuitableForCluster(def))
                .ToList();

            if (apexKinds.Count == 0)
            {
                sketch.pawns.Clear();
                return;
            }

            for (int index = 0; index < sketch.pawns.Count; index++)
            {
                MechClusterSketch.Mech mech = sketch.pawns[index];
                if (!IsApexPawnKind(mech.kindDef))
                {
                    float originalPower = mech.kindDef?.combatPower ?? 0f;
                    float closestDifference = apexKinds.Min(kind => Math.Abs(kind.combatPower - originalPower));
                    List<PawnKindDef> closestKinds = apexKinds
                        .Where(kind => Math.Abs(kind.combatPower - originalPower) == closestDifference)
                        .ToList();
                    mech.kindDef = closestKinds.RandomElement();
                    sketch.pawns[index] = mech;
                }
            }
        }
    }

    /// <summary>
    /// Narrows vanilla's normal eligibility result only while the Apex debug command is generating
    /// its sketch. Normal incidents and every other cluster-generation call remain unchanged.
    /// </summary>
    [HarmonyPatch(typeof(MechClusterGenerator), "MechKindSuitableForCluster")]
    internal static class Patch_MechClusterGenerator_ApexOnlyDebug
    {
        private static void Postfix(PawnKindDef def, ref bool __result)
        {
            if (__result && ApexMechanoidDebugActions.GeneratingApexOnlyCluster
                && !ApexMechanoidDebugActions.IsApexPawnKind(def))
            {
                __result = false;
            }
        }
    }
}
