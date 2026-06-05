using ArknightsMod.Content.Items.Armor;
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

		public override void PostUpdateEquips() {
			TexalterHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<TexalterHelmet>());
			TexalterSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<TexalterHelmet>(),
				ModContent.ItemType<TexalterChestplate>(),
				ModContent.ItemType<TexalterGreaves>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, TexalterSetActive, "Mods.ArknightsMod.ArmorSets.Texalter.SetBonus");
		}

		public override void ModifyWeaponDamage(Item item, ref StatModifier damage) {
			if (TexalterHelmetActive && OperatorPassiveSkillHelper.IsPassiveSkillActive(Player))
				damage *= 1.2f;
		}

		public override void ModifyHurt(ref Player.HurtModifiers modifiers) {
			if (TexalterSetActive && KillProcReady)
				modifiers.FinalDamage *= 0.85f;
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

			Player.statLife = Player.statLifeMax2;
			Player.HealEffect(Player.statLifeMax2);
			OperatorPassiveSkillHelper.TryRetriggerPassiveSkill(Player);
			KillProcCooldown = 20 * 60;
			KillProcReady = false;
		}
	}
}
