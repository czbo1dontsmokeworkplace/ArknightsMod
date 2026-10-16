using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.GameContent.Events;

namespace ArknightsMod.Content.Items.Accessories.Rogue.Rarity_l4
{
	public class RoundStoneAltar : ModItem
	{
		public override void ModifyTooltips(List<TooltipLine> tooltips) {
			var altarPlayer = Main.LocalPlayer.GetModPlayer<RoundStoneAltarPlayer>();
			string text = Language.GetTextValue("Mods.ArknightsMod.Items.RoundStoneAltar.StackTooltip", altarPlayer.buffStacks);
			tooltips.Add(new TooltipLine(Mod, "BuffStacks", text) { OverrideColor = Color.Gold });
		}

		public override void SetDefaults() {
			Item.width = 34;
			Item.height = 34;
			Item.accessory = true;
			Item.rare = ItemRarityID.Master;
			Item.value = Item.sellPrice(0, 16, 0, 0);
		}

		public override void UpdateAccessory(Player player, bool hideVisual) {
			player.GetModPlayer<RoundStoneAltarPlayer>().active = true;
		}
	}

	public class RoundStoneAltarPlayer : ModPlayer
	{
		public bool active;
		private int legacyStacks;
		private readonly HashSet<string> completedEncounters = new();
		public int buffStacks => legacyStacks + completedEncounters.Count;

		public override void SaveData(TagCompound tag) {
			tag["RoundStoneAltarStacks"] = buffStacks;
			tag["RoundStoneAltarLegacyStacks"] = legacyStacks;
			tag["RoundStoneAltarEncounters"] = new List<string>(completedEncounters);
		}

		public override void LoadData(TagCompound tag) {
			completedEncounters.Clear();
			if (tag.ContainsKey("RoundStoneAltarEncounters")) {
				legacyStacks = Math.Max(0, tag.GetInt("RoundStoneAltarLegacyStacks"));
				completedEncounters.UnionWith(tag.GetList<string>("RoundStoneAltarEncounters"));
			}
			else {
				// Old saves recorded only the stack count; preserve those earned stacks.
				legacyStacks = Math.Clamp(tag.GetInt("RoundStoneAltarStacks"), 0, 10);
			}
		}

		public override void ResetEffects() => active = false;

		public override void UpdateEquips() {
			if (!active) return;
			Player.GetDamage(DamageClass.Generic) += 0.01f * buffStacks;
			Player.statDefense += buffStacks;
		}

		internal void RecordCompletion(string key) {
			if (!active || Player.dead || !completedEncounters.Add(key)) return;
			if (Main.netMode == NetmodeID.Server)
				SendState();
			else
				ShowGrowth();
		}

		private void ShowGrowth() {
			CombatText.NewText(Player.getRect(), Color.Blue, $"+1 ({buffStacks})");
		}

		public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) => SendState(toWho, fromWho);

		private void SendState(int toWho = -1, int fromWho = -1) {
			ModPacket packet = Mod.GetPacket();
			packet.Write((short)global::ArknightsMod.ArknightsMod.ArkMessageID.RoundStoneAltarState);
			packet.Write((byte)Player.whoAmI);
			packet.Write(legacyStacks);
			packet.Write(completedEncounters.Count);
			foreach (string key in completedEncounters) packet.Write(key);
			packet.Send(toWho, fromWho);
		}

		internal static void ReceiveState(BinaryReader reader, int whoAmI) {
			int playerIndex = reader.ReadByte();
			int legacy = reader.ReadInt32();
			int count = reader.ReadInt32();
			if ((uint)playerIndex >= Main.maxPlayers || count < 0 || count > 4096) return;
			if (Main.netMode == NetmodeID.Server && playerIndex != whoAmI) return;

			var altarPlayer = Main.player[playerIndex].GetModPlayer<RoundStoneAltarPlayer>();
			int previousStacks = altarPlayer.buffStacks;
			altarPlayer.legacyStacks = Math.Clamp(legacy, 0, 10);
			altarPlayer.completedEncounters.Clear();
			for (int i = 0; i < count; i++) altarPlayer.completedEncounters.Add(reader.ReadString());
			if (Main.netMode == NetmodeID.Server)
				altarPlayer.SendState(-1, whoAmI);
			else if (playerIndex == Main.myPlayer && altarPlayer.buffStacks > previousStacks)
				altarPlayer.ShowGrowth();
		}
	}

	public class RoundStoneAltarGlobalNPC : GlobalNPC
	{
		public override void OnKill(NPC npc) {
			string key = GetEncounterKey(npc);
			if (key == null) return;
			// Multipart bosses grant a stack only after the entire encounter is defeated.
			foreach (NPC other in Main.ActiveNPCs)
				if (other.whoAmI != npc.whoAmI && other.life > 0 && GetEncounterKey(other) == key) return;
			RoundStoneAltarEventSystem.RecordCompletion(key);
		}

		private static string GetEncounterKey(NPC npc) {
			if (npc.type is NPCID.EaterofWorldsHead or NPCID.EaterofWorldsBody or NPCID.EaterofWorldsTail)
				return "Boss:Terraria/EaterOfWorlds";
			if (npc.type is NPCID.Retinazer or NPCID.Spazmatism)
				return "Boss:Terraria/Twins";
			if (!npc.boss) return null;
			if (npc.ModNPC?.FullName is "ArknightsMod/Faust" or "ArknightsMod/Mephisto")
				return "Boss:ArknightsMod/FaustAndMephisto";
			return npc.ModNPC != null ? "Boss:" + npc.ModNPC.FullName : "Boss:Terraria/" + npc.type;
		}
	}

	public class RoundStoneAltarEventSystem : ModSystem
	{
		private HashSet<string> previousEvents = new();
		private bool initialized;
		private bool oldOnesArmyWon;

		public override void OnWorldLoad() {
			previousEvents.Clear();
			initialized = false;
			oldOnesArmyWon = false;
		}

		public override void OnWorldUnload() => OnWorldLoad();

		public override void PostUpdateWorld() {
			if (Main.netMode == NetmodeID.MultiplayerClient) return;
			HashSet<string> current = GetActiveEvents();
			if (DD2Event.WonThisRun) oldOnesArmyWon = true;
			if (initialized) {
				foreach (string key in previousEvents)
					if (!current.Contains(key) && (key != "Event:OldOnesArmy" || oldOnesArmyWon))
						RecordCompletion(key);
			}
			if (!DD2Event.Ongoing) oldOnesArmyWon = false;
			previousEvents = current;
			initialized = true;
		}

		private static HashSet<string> GetActiveEvents() {
			HashSet<string> events = new();
			if (Main.invasionType > 0) events.Add("Event:Invasion/" + Main.invasionType);
			if (Main.bloodMoon) events.Add("Event:BloodMoon");
			if (Main.eclipse) events.Add("Event:SolarEclipse");
			if (Main.pumpkinMoon) events.Add("Event:PumpkinMoon");
			if (Main.snowMoon) events.Add("Event:FrostMoon");
			if (DD2Event.Ongoing) events.Add("Event:OldOnesArmy");
			return events;
		}

		internal static void RecordCompletion(string key) {
			foreach (Player player in Main.ActivePlayers)
				player.GetModPlayer<RoundStoneAltarPlayer>().RecordCompletion(key);
		}
	}
}
