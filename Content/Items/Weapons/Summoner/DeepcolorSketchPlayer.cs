using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Summoner
{
	public class DeepcolorSketchPlayer : ModPlayer
	{
		public override void ModifyHurt(ref Player.HurtModifiers modifiers) {
			if (!DeepcolorSketchSkills.IsOwnerInAnyTentacleAttackRange(Player))
				return;

			if (Main.rand.NextFloat() >= DeepcolorSketchSkills.VisualTrapDodgeChance)
				return;

			modifiers.Cancel();
		}
	}
}
