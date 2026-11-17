using HarmonyLib;
using RimWorld;
using Verse;


namespace ApexMechanoids
{
	public class GestatorExtension : DefModExtension
	{
	}

	[HarmonyPatch(typeof(Bill_Mech), nameof(Bill_Mech.PawnAllowedToStartAnew))]
	public static class Bill_Mech_PawnAllowedToStartAnew
	{
		public static void Prefix(ref Pawn p)
		{
			if (p.def.HasModExtension<GestatorExtension>() && p.GetOverseer() != null)
			{
				p = p.GetOverseer();
			}
		}
	}

	[HarmonyPatch(typeof(Bill_ResurrectMech), nameof(Bill_ResurrectMech.PawnAllowedToStartAnew))]
	public static class Bill_ResurrectMech_PawnAllowedToStartAnew
	{
		public static void Prefix(ref Pawn p)
		{
			if (p.def.HasModExtension<GestatorExtension>() && p.GetOverseer() != null)
			{
				p = p.GetOverseer();
			}
		}
	}

	[HarmonyPatch(typeof(Bill_Mech), nameof(Bill_Mech.Notify_DoBillStarted))]
	public static class Bill_Mech_Notify_DoBillStarted
	{
		public static void Postfix(ref Pawn ___boundPawn, Pawn billDoer)
		{
			if (billDoer.def.HasModExtension<GestatorExtension>())
			{
				___boundPawn = billDoer.GetOverseer();
			}
		}
	}
}
