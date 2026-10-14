using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ApexMechanoids
{
	public class HediffCompProperties_VampiricMechanites : HediffCompProperties
	{
		public int chargesPerDown = 1;

		public int chargesPerKill = 2;

		public int maxCharges = 25;

		public int healIntervalTicks = 600;

		public float healAmountPerTick = 0.5f;

		public int healTicksPerCharge = 300;

		public bool restoreMissingParts = true;

		public HediffCompProperties_VampiricMechanites()
		{
			compClass = typeof(HediffComp_VampiricMechanites);
		}
	}

	public class HediffComp_VampiricMechanites : HediffComp
	{
		public HediffCompProperties_VampiricMechanites Props => (HediffCompProperties_VampiricMechanites)props;

		private float charges;

		private int healTimer;

		// Cached "is the host currently damaged" flag. Lets the per-pulse tick skip
		// the hediff scan entirely when there is nothing to heal. Set true on damage
		// (see VampiricMechanites_DamagePatch) and re-evaluated after each heal attempt.
		// Internal so the sibling damage-patch class can flip it.
		internal bool cachedDamaged;

		// Pawns that currently carry this comp, mapped directly to their comp instance so
		// the global damage patch resolves it in O(1) without scanning every hediff on
		// every hit. Weak keys let dead/removed pawns be collected; the comp is also
		// explicitly removed in CompPostPostRemoved when its hediff is cured.
		private static readonly ConditionalWeakTable<Pawn, HediffComp_VampiricMechanites> pawnsWithVampiric = new ConditionalWeakTable<Pawn, HediffComp_VampiricMechanites>();

		internal static readonly ConditionalWeakTable<Pawn, object> downedCredited = new ConditionalWeakTable<Pawn, object>();

		public float Charges => charges;

		public override void CompPostMake()
		{
			base.CompPostMake();
			if (Pawn != null)
			{
				// Always refresh: the hediff may have been removed and re-added,
				// in which case this is a different comp instance.
				pawnsWithVampiric.Remove(Pawn);
				pawnsWithVampiric.Add(Pawn, this);
			}
			cachedDamaged = IsDamaged();
		}

		// Whether the host currently has damage worth repairing. Uses vanilla's
		// natural-healing check for injuries and our own missing-part test (vanilla
		// never regrows missing parts on its own).
		private bool IsDamaged()
		{
			HediffSet hediffSet = Pawn.health.hediffSet;
			if (Props.restoreMissingParts && hediffSet.GetMissingPartsCommonAncestors().Count > 0)
			{
				return true;
			}
			return hediffSet.HasNaturallyHealingInjury();
		}

		public override void CompPostTick(ref float severityAdjustment)
		{
			base.CompPostTick(ref severityAdjustment);
			if (!Pawn.Spawned || charges <= 0)
			{
				return;
			}

			healTimer--;
			if (healTimer > 0)
			{
				return;
			}
			healTimer = Props.healIntervalTicks;

			// Nothing to repair: skip the hediff scan entirely until damage occurs.
			if (!cachedDamaged)
			{
				return;
			}

			// TryHealOnce returns true only while something was actually repaired,
			// which is exactly the "still damaged" condition, so reuse it to refresh
			// the cache without a second hediff scan per pulse.
			cachedDamaged = TryHealOnce();
		}

		private bool TryHealOnce()
		{
			HediffSet hediffSet = Pawn.health.hediffSet;

			if (Props.restoreMissingParts)
			{
				Hediff_MissingPart missing = hediffSet.GetMissingPartsCommonAncestors().FirstOrDefault();
				if (missing != null)
				{
				Pawn.health.RestorePart(missing.Part);
				charges = Mathf.Max(0, charges - 1);
				return true;
				}
			}

			float budget = Props.healAmountPerTick;
			List<Hediff> hediffs = hediffSet.hediffs;
			bool healed = false;
			for (int i = 0; i < hediffs.Count; i++)
			{
				if (budget <= 0f)
				{
					break;
				}
				Hediff injury = hediffs[i];
				if (!(injury is Hediff_Injury))
				{
					continue;
				}
				float amount = Mathf.Min(injury.Severity, budget);
				injury.Heal(amount);
				budget -= amount;
				healed = true;
			}

			if (healed)
			{
				// One charge funds healTicksPerCharge heal pulses. Consume it
				// fractionally so a single charge lasts many pulses instead of
				// being spent on the very first one.
				charges = Mathf.Max(0, charges - 1f / Props.healTicksPerCharge);
			}
			return healed;
		}

		public void AddCharges(int amount)
		{
			if (amount <= 0)
			{
				return;
			}
			charges = Mathf.Min(Props.maxCharges, charges + amount);
		}

		public override void CompExposeData()
		{
			base.CompExposeData();
			Scribe_Values.Look(ref charges, "vampiricCharges", 0);
			Scribe_Values.Look(ref healTimer, "vampiricHealTimer", 0);
			if (Scribe.mode == LoadSaveMode.PostLoadInit && Pawn != null)
			{
				pawnsWithVampiric.Remove(Pawn);
				pawnsWithVampiric.Add(Pawn, this);
				// Derive from current state rather than persisting it.
				cachedDamaged = IsDamaged();
			}
		}

		public override string CompTipStringExtra => "APM_VampiricCharges".Translate(charges.ToString("0.#"), Props.maxCharges, (charges * Props.healTicksPerCharge * Props.healAmountPerTick).ToString("0"));

		// Resolves the comp on a pawn in O(1) via the lookup table (used by the damage patch).
		public static HediffComp_VampiricMechanites GetOn(Pawn pawn)
		{
			if (pawn == null || pawn.health == null)
			{
				return null;
			}
			pawnsWithVampiric.TryGetValue(pawn, out HediffComp_VampiricMechanites comp);
			return comp;
		}

		public override void CompPostPostRemoved()
		{
			base.CompPostPostRemoved();
			if (Pawn != null)
			{
				pawnsWithVampiric.Remove(Pawn);
			}
		}
	}

	[HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.PostApplyDamage))]
	public static class VampiricMechanites_DamagePatch
	{
		[HarmonyPostfix]
		public static void PostApplyDamage(Pawn_HealthTracker __instance, DamageInfo dinfo, float totalDamageDealt)
		{
			// The host itself took damage: flag it so the heal tick will scan and
			// repair. Placed before the instigator checks so environmental damage
			// (null instigator) is also caught.
			Pawn victim = __instance.pawn;
			if (victim != null)
			{
				HediffComp_VampiricMechanites victimComp = HediffComp_VampiricMechanites.GetOn(victim);
				if (victimComp != null)
				{
					victimComp.cachedDamaged = true;
				}
			}

			if (dinfo.Instigator == null)
			{
				return;
			}

			Pawn attacker = dinfo.Instigator as Pawn;
			if (attacker == null && dinfo.Instigator is Projectile projectile)
			{
				attacker = projectile.Launcher as Pawn;
			}
			if (attacker == null)
			{
				return;
			}

			if (victim == attacker)
			{
				return;
			}

			HediffComp_VampiricMechanites comp = HediffComp_VampiricMechanites.GetOn(attacker);
			if (comp == null)
			{
				return;
			}

			if (victim.Dead)
			{
				HediffComp_VampiricMechanites.downedCredited.Remove(victim);
				comp.AddCharges(comp.Props.chargesPerKill);
			}
			else if (victim.Downed)
			{
				if (!HediffComp_VampiricMechanites.downedCredited.TryGetValue(victim, out _))
				{
					HediffComp_VampiricMechanites.downedCredited.Add(victim, null);
					comp.AddCharges(comp.Props.chargesPerDown);
				}
			}
			else
			{
				HediffComp_VampiricMechanites.downedCredited.Remove(victim);
			}
		}
	}
}
