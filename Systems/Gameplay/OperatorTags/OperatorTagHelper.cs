using ArknightsMod.Common.GlobalNPCs;
using ArknightsMod.Content.Projectiles.Guard.Saki;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Systems.Gameplay.OperatorTags
{
	internal static class OperatorTagHelper
	{
		public static bool TryGetPlayerTags(Player player, out OperatorClass cls, out OperatorFaction factions) {
			cls = OperatorClass.None;
			factions = OperatorFaction.None;

			if (player?.armor == null || player.armor[0].IsAir)
				return false;

			if (!OperatorTagRegistry.TryGetFromHelmet(player.armor[0].type, out OperatorTagRegistry.OperatorTagEntry entry))
				return false;

			cls = entry.Class;
			factions = entry.Factions;
			return true;
		}

		public static bool PlayerHasFaction(Player player, OperatorFaction faction) {
			return TryGetPlayerTags(player, out _, out OperatorFaction factions)
				&& factions.HasFlag(faction);
		}

		public static bool NpcHasFaction(NPC npc, OperatorFaction faction) {
			if (npc == null || !npc.active)
				return false;

			OperatorFaction factions = npc.GetGlobalNPC<OperatorFactionGlobalNPC>().Factions;
			return factions.HasFlag(faction);
		}

		public static int CountHostileEnemies() {
			int count = 0;
			for (int i = 0; i < Main.maxNPCs; i++) {
				NPC npc = Main.npc[i];
				if (npc.active && npc.CanBeChasedBy() && !npc.friendly)
					count++;
			}

			return count;
		}

		public static int CountNotesOnField(int ownerWhoAmI) {
			int count = 0;
			for (int i = 0; i < Main.maxProjectiles; i++) {
				Projectile proj = Main.projectile[i];
				if (!proj.active || proj.owner != ownerWhoAmI)
					continue;

				if (proj.ModProjectile is SakiNoteIdle or SakiNoteCommon or SakiNote1 or SakiNote2Dark or SakiNote2Bright)
					count++;
			}

			return count;
		}

		public static bool AnyPlayerWithHelmet<THelmet>(out Player found)
			where THelmet : ModItem {
			int helmetType = ModContent.ItemType<THelmet>();
			for (int i = 0; i < Main.maxPlayers; i++) {
				Player player = Main.player[i];
				if (player.active && !player.dead && player.armor[0].type == helmetType) {
					found = player;
					return true;
				}
			}

			found = null;
			return false;
		}
	}
}
