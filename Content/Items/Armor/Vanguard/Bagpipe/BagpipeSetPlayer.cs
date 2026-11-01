using ArknightsMod.Content.Items.Armor;
using ArknightsMod.Content.Items.Armor.NeoArmorReforge;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanguard.Bagpipe
{
	internal class BagpipeSetPlayer : ArknightsArmorPlayer
	{
		public bool BagpipeHelmetActive;
		public bool BagpipeSetActive;

		private bool applyingExtraHit;

		public override void ResetEffects() {
			BagpipeHelmetActive = false;
			BagpipeSetActive = false;
		}

		// NeoArmor Reforge：套装件是独立 ItemID，穿上它本身就代表"已经是套装形态"，
		// 不需要再查 hasUpgraded；player.setBonus 文本交给 BagpipeHead 的
		// SetProfile.SetBonusKey 统一设置，这里不再重复设置一遍。
		public override void PostUpdateEquips() {
			BagpipeHelmetActive = Player.armor[0].type == NeoArmorReforgeSetLoader.GetSetType<BagpipeHead>();
			BagpipeSetActive = BagpipeHelmetActive
				&& Player.armor[1].type == NeoArmorReforgeSetLoader.GetSetType<BagpipeBody>()
				&& Player.armor[2].type == NeoArmorReforgeSetLoader.GetSetType<BagpipeLegs>();
		}

		// 技能在 ResetEffects 中初始化；直接读取装备，避免依赖尚未刷新的光环标记。
		internal static int GetSwitchInitialSpBonus(Player recipient, Item weapon) {
			if (!recipient.active || recipient.dead || !OperatorVanguardSetHelper.WearsFullVanguardSet(recipient))
				return 0;
			string weaponNamespace = weapon.ModItem?.GetType().Namespace;
			const string vanguardNamespace = "ArknightsMod.Content.Items.Weapons.Vanguard";
			if (weaponNamespace == null || (weaponNamespace != vanguardNamespace
				&& !weaponNamespace.StartsWith(vanguardNamespace + ".", StringComparison.Ordinal)))
				return 0;

			int helmetType = NeoArmorReforgeSetLoader.GetSetType<BagpipeHead>();
			foreach (Player source in Main.ActivePlayers) {
				if (!source.dead && source.armor[0].type == helmetType)
					return 8;
			}
			return 0;
		}

		public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone) {
			if (item.DamageType.CountsAsClass(DamageClass.Melee))
				TryExtraHit(target, hit, damageDone);
		}

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone) {
			if (proj.DamageType.CountsAsClass(DamageClass.Melee))
				TryExtraHit(target, hit, damageDone);
		}

		private void TryExtraHit(NPC target, NPC.HitInfo hit, int damageDone) {
			// 仅攻击者客户端掷骰并同步已结算的额外命中，服务端不重复触发。
			if (applyingExtraHit || Main.netMode == NetmodeID.Server || Player.whoAmI != Main.myPlayer
				|| !BagpipeSetActive || damageDone <= 0 || !target.active || target.life <= 0
				|| target.dontTakeDamage || Main.rand.NextFloat() >= 0.2f)
				return;

			NPC.HitInfo extraHit = hit;
			extraHit.Damage = Math.Max(1, (int)Math.Min(int.MaxValue, hit.Damage * 1.5d));
			extraHit.SourceDamage = Math.Max(1, (int)Math.Min(int.MaxValue, hit.SourceDamage * 1.5d));
			applyingExtraHit = true;
			try {
				target.PlayerInteraction(Player.whoAmI);
				// 复制已结算的命中，避免再次计算防御或暴击；原命中保持不变。
				target.StrikeNPC(extraHit);
				if (Main.netMode != NetmodeID.SinglePlayer)
					NetMessage.SendStrikeNPC(target, ref extraHit);
			}
			finally { applyingExtraHit = false; }
		}
	}
}
