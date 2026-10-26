using ArknightsMod.Content.Items.Armor;
using ArknightsMod.Content.Items.Armor.NeoArmorReforge;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Specialist.TexasAlter
{
	internal class TexalterSetPlayer : ArknightsArmorPlayer
	{
		public bool TexalterHelmetActive;
		public bool TexalterSetActive;

		public int KillProcCooldown;
		public bool KillProcReady;

		public override void ResetEffects() {
			TexalterHelmetActive = false;
			TexalterSetActive = false;
		}

		// NeoArmor Reforge：套装件是独立 ItemID，穿上它本身就代表"已经是套装形态"，
		// 不需要再查 hasUpgraded；player.setBonus 文本交给 TexalterHead 的
		// SetProfile.SetBonusKey 统一设置，这里不再重复设置一遍。
		public override void PostUpdateEquips() {
			TexalterHelmetActive = Player.armor[0].type == NeoArmorReforgeSetLoader.GetSetType<TexalterHead>();
			TexalterSetActive = TexalterHelmetActive
				&& Player.armor[1].type == NeoArmorReforgeSetLoader.GetSetType<TexalterBody>()
				&& Player.armor[2].type == NeoArmorReforgeSetLoader.GetSetType<TexalterLegs>();
		}

		private void ApplyArmorDamageStats() {
			if (TexalterHelmetActive && OperatorPassiveSkillHelper.IsPassiveSkillActive(Player))
				Player.GetDamage(DamageClass.Generic) *= 1.2f;
		}

		public override void PostUpdate() {
			if (!TexalterSetActive)
				return;

			if (KillProcCooldown > 0) {
				KillProcCooldown--;
				if (KillProcCooldown <= 0)
					KillProcReady = true;
			}
		}

		public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone) {
			TryProcOnKill(target);
		}

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone) {
			TryProcOnKill(target);
		}

		private void TryProcOnKill(NPC target) {
			if (!TexalterSetActive || target.life > 0 || Main.netMode == NetmodeID.MultiplayerClient)
				return;

			int lifeBefore = Player.statLife;
			Player.GetModPlayer<global::ArknightsMod.Players.IncomingHealingPlayer>().RestoreLife(Player.statLifeMax2);
			if (Player.statLife > lifeBefore)
				Player.HealEffect(Player.statLife - lifeBefore);
			OperatorPassiveSkillHelper.TryRetriggerPassiveSkill(Player);
			KillProcCooldown = 20 * 60;
			KillProcReady = false;
		}

		public override void PostUpdateMiscEffects() {
			ApplyArmorDamageStats();
			if (TexalterSetActive && KillProcReady)
				Player.endurance = 1f - (1f - Player.endurance) * 0.85f;
		}
	}
}
