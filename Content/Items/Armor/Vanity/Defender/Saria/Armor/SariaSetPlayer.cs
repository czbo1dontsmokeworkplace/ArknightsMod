using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Defender.Saria.Armor
{
	internal class SariaSetPlayer : ModPlayer
	{
		public bool SariaSetActive;

		public override void ResetEffects() {
			SariaSetActive = false;
		}

		public override void ModifyHurt(ref Player.HurtModifiers modifiers) {
			if (SariaSetActive)
			{
				/*周围有敌人累计20秒，攻击力+5%，防御力+4%,最多叠加5层  
				 * 每次回复友方单位生命值时额外回复该单位1点技力*/
			}
		}
	}
}
