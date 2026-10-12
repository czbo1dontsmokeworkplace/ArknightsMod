using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Dusts
{
	//高:64 宽:64
	public class SurtrAttack_Dust : ModDust
	{
		public override void OnSpawn(Dust dust) {
			dust.noGravity = true;
			dust.noLight = false;
			dust.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
			dust.customData = 0;
		}

		public override bool Update(Dust dust)
		{
			dust.position += dust.velocity;
			dust.velocity *= 0.9f;
			int frame = (int)dust.customData;
			if (frame >= 16) {
				dust.active = false;
				return false;
			}
			dust.frame = new Rectangle(64 * (frame % 4), 64 * (frame / 4), 64, 64);
			Lighting.AddLight(dust.position, Color.OrangeRed.ToVector3() * 0.5f * (16 - frame) / 16f);
			dust.customData = frame + 1;
			return false;
		}
	}
}

