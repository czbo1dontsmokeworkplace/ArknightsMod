using ArknightsMod.Common.GlobalNPCs;
using ArknightsMod.Content.Items.Armor;
using ArknightsMod.Content.Items.Armor.NeoArmorReforge;
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

			// 干员标签（职业/阵营）仅在头部件作为「已升级盔甲」穿戴时生效，与套装效果一致（纯时装不触发）。
			if (!IsUpgradedHelmet(player.armor[0]))
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
				if (player.active && !player.dead && IsUpgradedHelmetOf(player.armor[0], helmetType)) {
					found = player;
					return true;
				}
			}

			found = null;
			return false;
		}

		// 「这件东西是不是某位干员的**盔甲形态**头部件」。
		//
		// 两套系统的"盔甲形态"表示方式不同，都要认：
		//   · 旧 NeoArmor：物品还是那个时装 ItemID，靠 GlobalItem 上的 hasUpgraded 标记区分；
		//   · NeoArmor Reforge：套装件是**另一个独立的 ItemID**，压根没有 hasUpgraded 这个
		//     状态，只查旧标记的话所有已迁移干员都会被判成"没升级"，标签系统直接失效
		//     且没有任何报错。
		private static bool IsUpgradedHelmet(Item helmet) {
			return helmet.neoarmor().hasUpgraded
				|| NeoArmorReforgeSetLoader.GetVanity(helmet.type) != null;
		}

		// 同上，外加"必须是指定那位干员的"。传入的 helmetType 是**时装**的 ItemID。
		private static bool IsUpgradedHelmetOf(Item helmet, int vanityHelmetType) {
			if (helmet.type == vanityHelmetType && helmet.neoarmor().hasUpgraded)
				return true;

			NeoArmorReforgeVanityItem vanity = NeoArmorReforgeSetLoader.GetVanity(helmet.type);
			return vanity != null && vanity.Type == vanityHelmetType;
		}
	}
}
