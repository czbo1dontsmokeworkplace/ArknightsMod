using ArknightsMod.Content.Projectiles.Supporter.Deepcolor;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Supporter.Deepcolor
{
	public class DeepcolorSketchPlayer : ModPlayer
	{
		public const int RedeployCooldownMax = 300;

		public int RedeployCooldown;
		public bool CanRedeploy => RedeployCooldown <= 0;
		public bool IsLogoChargeActive { get; private set; }

		// 首次右键 LOGO 时锁定，供打击弹幕使用（弹幕 ai 仅 3 槽）
		public Vector2 LogoStrikeWorld;

		private static int _logoChargeType = -1;
		private bool logoRightClickArmed = true;

		private static int LogoChargeType =>
			_logoChargeType >= 0 ? _logoChargeType : _logoChargeType = ModContent.ProjectileType<DeepcolorSketchLogoAttack>();

		public void StartRedeployCooldown() {
			RedeployCooldown = RedeployCooldownMax;
		}

		public override void PostUpdate() {
			if (RedeployCooldown > 0)
				RedeployCooldown--;

			UpdateLogoChargeActive();

			if (!IsLocalHoldingSketch())
				return;

			if (!Main.mouseRight)
				logoRightClickArmed = true;

			TryHandleRightClickPress();
			TryHandleRightClickRelease();
		}

		private void UpdateLogoChargeActive() {
			IsLogoChargeActive = false;
			if (Player.HeldItem.ModItem is not DeepcolorSketch)
				return;

			for (int i = 0; i < Main.maxProjectiles; i++) {
				Projectile proj = Main.projectile[i];
				if (proj.active && proj.owner == Player.whoAmI && proj.type == LogoChargeType) {
					IsLogoChargeActive = true;
					return;
				}
			}
		}

		private bool IsLocalHoldingSketch() =>
			Player.whoAmI == Main.myPlayer && Player.HeldItem.ModItem is DeepcolorSketch;

		private bool BlocksLogoInput() =>
			Player.mouseInterface
			|| Main.LocalPlayerHasPendingInventoryActions()
			|| Main.HoveringAnInteractable;

		private void TryHandleRightClickPress() {
			if (!PlayerInput.Triggers.JustPressed.MouseRight || BlocksLogoInput())
				return;
			if (DeepcolorSketch.IsSkillActivateModifierHeld(Player) || IsLogoChargeActive)
				return;

			DeepcolorSketch.TryTriggerLogoAttack(Player, Player.HeldItem);
		}

		private void TryHandleRightClickRelease() {
			if (!PlayerInput.Triggers.JustReleased.MouseRight || BlocksLogoInput())
				return;
			if (!logoRightClickArmed || !DeepcolorSketch.IsSkillActivateModifierHeld(Player))
				return;

			if (DeepcolorSketch.TryActivateSkill(Player))
				logoRightClickArmed = false;
		}

		public override void ModifyHurt(ref Player.HurtModifiers modifiers) {
			if (!DeepcolorSketchSkills.IsOwnerInAnyTentacleAttackRange(Player))
				return;

			if (Main.rand.NextFloat() >= DeepcolorSketchSkills.VisualTrapDodgeChance)
				return;

			modifiers.Cancel();
		}
	}
}
