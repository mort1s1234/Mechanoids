using RimWorld;
using Verse;
using System;
using System.Collections.Generic;
using System.Text;

namespace ApexMechanoids
{
	public class CompProperties_StatFactorsFromHealth : CompProperties
	{
		public class StatFactorFromHealth
		{
			public StatFactorFromHealth() { }

			public StatDef stat;
			public HediffDef hediff;

			public SimpleCurve curve;
		}

		public List<StatFactorFromHealth> statFactors;

		public CompProperties_StatFactorsFromHealth()
		{
			compClass = typeof(CompStatFactorsFromHealth);
		}
	}
	public class CompStatFactorsFromHealth : ThingComp
	{
		public Pawn Pawn => parent as Pawn;

		public CompProperties_StatFactorsFromHealth Props => (CompProperties_StatFactorsFromHealth)props;

		public override void CompTick()
		{
			base.CompTick();
			if (Pawn.IsHashIntervalTick(120)) // Update every 60 ticks (1 second)
			{
				UpdateStatHediffs();
			}
		}

		private void UpdateStatHediffs()
		{
			if (Props.statFactors == null) return;

			foreach(var statFactor in Props.statFactors)
			{
				if (statFactor.stat == null || statFactor.hediff == null || statFactor.curve == null) continue;

				// Get or add hediff
				Hediff existingHediff = Pawn.health.hediffSet.GetFirstHediffOfDef(statFactor.hediff);
				float healthPercent = Pawn.health.summaryHealth.SummaryHealthPercent;
				float factor = statFactor.curve.Evaluate(healthPercent);
				
				if (factor != 1f) // Only add hediff if there's actually a modifier
				{
					if (existingHediff == null)
					{
						existingHediff = HediffMaker.MakeHediff(statFactor.hediff, Pawn);
						Pawn.health.AddHediff(existingHediff);
					}
					
					// Update the severity to reflect the current factor
					existingHediff.Severity = Math.Abs(factor - 1f) * 10; // Scale for visibility
				}
				else if (existingHediff != null)
				{
					// Remove hediff if no modifier
					Pawn.health.RemoveHediff(existingHediff);
				}
			}
		}

		public override float GetStatFactor(StatDef stat)
		{
			float num = base.GetStatFactor(stat);
			foreach(CompProperties_StatFactorsFromHealth.StatFactorFromHealth item in Props.statFactors)
			{
				if(item.stat == null)
					{
						Log.Error("[ApexMechanoids] CompStatFactorsFromHealth: statFactor has null stat def.");
					}
				if(item.stat == stat)
				{
					float num2 = Pawn.health.summaryHealth.SummaryHealthPercent;
					num = item.curve.Evaluate(num2);
					return num;
				}
			}
			return num;
		}
	}

	public class Hediff_StatFactorFromHealth : HediffWithComps
	{
		public override string TipStringExtra
		{
			get
			{
				StringBuilder sb = new StringBuilder();
				sb.AppendLine(base.TipStringExtra);
				
				// Get the stat factor information
				if (pawn?.GetComp<CompStatFactorsFromHealth>() is CompStatFactorsFromHealth comp)
				{
					foreach(var statFactor in comp.Props.statFactors)
					{
						if (statFactor.hediff == def && statFactor.stat != null && statFactor.curve != null)
						{
							float healthPercent = pawn.health.summaryHealth.SummaryHealthPercent;
							float factor = statFactor.curve.Evaluate(healthPercent);
							float percentageChange = (factor - 1f) * 100f;
							
							string changeText = percentageChange > 0 ? "+" + percentageChange.ToString("F0") : percentageChange.ToString("F0");
							sb.AppendLine($"{statFactor.stat.LabelCap}: {changeText}%");
							break;
						}
					}
				}
				
				return sb.ToString().TrimEnd();
			}
		}
	}
}
