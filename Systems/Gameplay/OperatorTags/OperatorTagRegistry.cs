using System.Collections.Generic;
using ArknightsMod.Content.Items.Armor.Defender.Mudrock;
using ArknightsMod.Content.Items.Armor.Guard.Mlynar;
using ArknightsMod.Content.Items.Armor.Guard.Oblivionis;
using ArknightsMod.Content.Items.Armor.Guard.Skadi;
using ArknightsMod.Content.Items.Armor.Guard.Ulpianus;
using ArknightsMod.Content.Items.Armor.Specialist.Dorothy;
using ArknightsMod.Content.Items.Armor.Specialist.ExusiaiAlter;
using ArknightsMod.Content.Items.Armor.Sniper.Exusiai;
using ArknightsMod.Content.Items.Armor.Sniper.Fartooth;
using ArknightsMod.Content.Items.Armor.Sniper.Rosmontis;
using ArknightsMod.Content.Items.Armor.Caster.Mostima;
using ArknightsMod.Content.Items.Armor.Supporter.CivilightEterna;
using ArknightsMod.Content.Items.Armor.Supporter.Ling;
using ArknightsMod.Content.Items.Armor.Vanguard.Bagpipe;
using Terraria.ModLoader;

namespace ArknightsMod.Systems.Gameplay.OperatorTags
{
	internal static class OperatorTagRegistry
	{
		private static readonly Dictionary<int, OperatorTagEntry> ByHelmet = new();

		internal readonly struct OperatorTagEntry
		{
			public readonly OperatorClass Class;
			public readonly OperatorFaction Factions;

			public OperatorTagEntry(OperatorClass cls, OperatorFaction factions) {
				Class = cls;
				Factions = factions;
			}
		}

		internal static void Register(int helmetType, OperatorClass cls, OperatorFaction factions) {
			ByHelmet[helmetType] = new OperatorTagEntry(cls, factions);
		}

		internal static bool TryGetFromHelmet(int helmetType, out OperatorTagEntry entry) {
			return ByHelmet.TryGetValue(helmetType, out entry);
		}

		internal static void Clear() => ByHelmet.Clear();
	}

	internal class OperatorTagLoader : ModSystem
	{
		public override void OnModLoad() {
			OperatorTagRegistry.Clear();
			RegisterAll();
		}

		private static void RegisterAll() {
			Register<MudrockHead>(OperatorClass.Defender, OperatorFaction.Sarkaz | OperatorFaction.RhodesIsland);
			Register<MlynarHelmet>(OperatorClass.Guard, OperatorFaction.Kazimierz);
			Register<OblivionisHelmet>(OperatorClass.Guard, OperatorFaction.AveMujica);
			Register<UlpianusHelmet>(OperatorClass.Guard, OperatorFaction.AbyssalHunter);
			Register<SkadiHelmet>(OperatorClass.Guard, OperatorFaction.AbyssalHunter);
			Register<RosmontisHelmet>(OperatorClass.Sniper, OperatorFaction.RhodesIsland);
			Register<DorothyHelmet>(OperatorClass.Specialist, OperatorFaction.RhodesIsland);
			Register<ExusiaiHelmet>(OperatorClass.Sniper, OperatorFaction.Laterano);
			Register<ExusiaiAlterHelmet>(OperatorClass.Specialist, OperatorFaction.Laterano);
			Register<MostimaHelmet>(OperatorClass.Caster, OperatorFaction.Laterano);
			Register<FartoothHelmet>(OperatorClass.Sniper, OperatorFaction.Laterano);
			Register<CivilightEternaHelmet>(OperatorClass.Supporter, OperatorFaction.Sarkaz | OperatorFaction.RhodesIsland);
			Register<LingHelmet>(OperatorClass.Supporter, OperatorFaction.RhodesIsland);
			Register<BagpipeHelmet>(OperatorClass.Vanguard, OperatorFaction.RhodesIsland);
		}

		private static void Register<THelmet>(OperatorClass cls, OperatorFaction factions)
			where THelmet : ModItem {
			OperatorTagRegistry.Register(ModContent.ItemType<THelmet>(), cls, factions);
		}
	}
}
