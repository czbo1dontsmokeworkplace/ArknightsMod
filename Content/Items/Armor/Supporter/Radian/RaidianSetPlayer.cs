using System.IO;
using ArknightsMod.Common.GlobalNPCs;
using ArknightsMod.Content.Items.Armor;
using ArknightsMod.Content.Items.Armor.NeoArmorReforge;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Supporter.Radian
{
	internal class RaidianSetPlayer : ArknightsArmorPlayer
	{
		/// <summary>套装效果给被标记敌人追加的伤害占召唤物攻击力的比例。</summary>
		public const float MarkedBonusDamageRatio = 0.08f;

		/// <summary>头盔鼓舞提供相当于主人生命上限5%的固定攻击力。</summary>
		public const float InspireDamageRatio = 0.05f;

		/// <summary>召唤物"优先攻击被标记敌人"的搜索半径（像素）。</summary>
		private const float MarkedSearchRangePx = 1200f;

		public bool RaidianHelmetActive;
		public bool RaidianSetActive;

		public override void ResetEffects() {
			RaidianHelmetActive = false;
			RaidianSetActive = false;
		}

		// NeoArmor Reforge：套装件是独立 ItemID，穿上它本身就代表"已经是套装形态"，
		// 不需要再查 hasUpgraded；player.setBonus 文本交给 RaidianHead 的
		// SetProfile.SetBonusKey 统一设置，这里不再重复设置一遍。
		public override void PostUpdateEquips() {
			RaidianHelmetActive = Player.armor[0].type == NeoArmorReforgeSetLoader.GetSetType<RaidianHead>();
			RaidianSetActive = RaidianHelmetActive
				&& Player.armor[1].type == NeoArmorReforgeSetLoader.GetSetType<RaidianBody>()
				&& Player.armor[2].type == NeoArmorReforgeSetLoader.GetSetType<RaidianLegs>();

			int slotBonus = 0;
			if (RaidianHelmetActive)
				slotBonus++;
			if (RaidianSetActive)
				slotBonus++;

			Player.maxMinions += slotBonus;

		}

		public override void PostUpdate() {
			if (RaidianSetActive)
				RetargetMinionsToMarked();
		}

		// 套装效果的「召唤物优先攻击被标记的敌人」。
		//
		// 直接改每个召唤物的 AI 目标是不现实的（每种召唤物选敌逻辑都不一样，还有别的
		// 模组的），所以走原版统一的"指定目标"通道 Player.MinionAttackTargetNPC ——
		// 这是玩家右键点敌人手动指定目标时用的同一个字段，原版和绝大多数模组的召唤物
		// AI 都会优先认它。
		//
		// ⚠ 只在玩家自己没有手动指定目标时才接管，否则会把玩家的手动指定顶掉。
		private void RetargetMinionsToMarked() {
			if (Player.HasMinionAttackTargetNPC)
				return;

			NPC best = null;
			float bestDistSq = MarkedSearchRangePx * MarkedSearchRangePx;

			foreach (NPC npc in Main.ActiveNPCs) {
				if (npc.friendly || npc.dontTakeDamage || npc.lifeMax <= 5)
					continue;
				if (!npc.GetGlobalNPC<RaidianMarkGlobalNPC>().RaidianMarked)
					continue;
				if (!npc.CanBeChasedBy(Player))
					continue;

				float distSq = Vector2.DistanceSquared(Player.Center, npc.Center);
				if (distSq >= bestDistSq)
					continue;

				best = npc;
				bestDistSq = distSq;
			}

			if (best != null)
				Player.MinionAttackTargetNPC = best.whoAmI;
		}

		internal void TryActivateMark() {
			if (Player.whoAmI != Main.myPlayer || !WearsSet(Player))
				return;

			NPC selected = null;
			float nearestMouseDistance = float.MaxValue;
			float nearestPlayerDistance = float.MaxValue;
			foreach (NPC npc in Main.ActiveNPCs) {
				if (!CanMark(npc))
					continue;
				float distance = Vector2.DistanceSquared(Main.MouseWorld, npc.Center);
				float playerDistance = Vector2.DistanceSquared(Player.Center, npc.Center);
				if (distance < nearestMouseDistance
					|| (distance == nearestMouseDistance && playerDistance < nearestPlayerDistance)) {
					selected = npc;
					nearestMouseDistance = distance;
					nearestPlayerDistance = playerDistance;
				}
			}
			if (selected == null)
				return;

			Player.MinionAttackTargetNPC = selected.whoAmI;
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				var packet = ModContent.GetInstance<ArknightsMod>().GetPacket();
				packet.Write((short)ArknightsMod.ArkMessageID.RaidianMarkRequest);
				packet.Write((short)selected.whoAmI);
				packet.Send();
			}
			else
				MarkTarget(selected);
		}

		private static bool WearsSet(Player player) => player.active && !player.dead
			&& player.armor[0].type == NeoArmorReforgeSetLoader.GetSetType<RaidianHead>()
			&& player.armor[1].type == NeoArmorReforgeSetLoader.GetSetType<RaidianBody>()
			&& player.armor[2].type == NeoArmorReforgeSetLoader.GetSetType<RaidianLegs>();

		private bool CanMark(NPC target) => target.active && !target.friendly && !target.dontTakeDamage
			&& target.lifeMax > 5 && target.CanBeChasedBy(Player)
			&& Vector2.DistanceSquared(Player.Center, target.Center) <= MarkedSearchRangePx * MarkedSearchRangePx;

		private void MarkTarget(NPC target) {
			if (!WearsSet(Player) || !CanMark(target))
				return;
			target.GetGlobalNPC<RaidianMarkGlobalNPC>().RaidianMarked = true;
			target.netUpdate = true;
			Player.MinionAttackTargetNPC = target.whoAmI;
		}

		internal static void ReceiveMarkRequest(BinaryReader reader, int sender) {
			int target = reader.ReadInt16();
			if (Main.netMode != NetmodeID.Server || sender < 0 || sender >= Main.maxPlayers
				|| target < 0 || target >= Main.maxNPCs)
				return;
			Main.player[sender].GetModPlayer<RaidianSetPlayer>().MarkTarget(Main.npc[target]);
		}
	}
}
