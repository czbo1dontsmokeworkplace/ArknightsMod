using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Supporter.Deepcolor
{
	public class DeepcolorSketchPlayer : ModPlayer
	{
		public const int RedeployCooldownMax = 300;

		public int RedeployCooldown;

		public bool CanRedeploy => RedeployCooldown <= 0;

		public void StartRedeployCooldown() {
			RedeployCooldown = RedeployCooldownMax;
		}

		public override void PostUpdate() {
			if (RedeployCooldown > 0)
				RedeployCooldown--;
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
