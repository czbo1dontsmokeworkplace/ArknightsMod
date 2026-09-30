using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Dusts
{
	public class surtrDamage_Dust : ModDust
	{
		public override void OnSpawn(Dust dust) {
			dust.noGravity = true;
			dust.noLight = true;
			dust.rotation = 0;
			dust.frame = new Rectangle(0, 0, 128, 128);
			dust.velocity = new Vector2(0f, 0f);
			dust.customData = 0;
		}

		public override bool Update(Dust dust) {
			dust.customData = (int)dust.customData + 1;
			Lighting.AddLight(dust.position, Color.OrangeRed.ToVector3()*0.5f);
			if((int)dust.customData>30)
				dust.active = false;
			return false;
		}
		public Texture2D surtrTexture => ModContent.Request<Texture2D>("ArknightsMod/Content/Dusts/surtrDamage_Dust").Value;
		public override bool PreDraw(Dust dust) {
			SpriteBatch sb = Main.spriteBatch;
			float len = 128 * dust.scale / 2;
			Vector2 pos = dust.position - Main.screenPosition;
			Rectangle rect = new Rectangle((int)(pos.X - len / 2), (int)(pos.Y - len / 2), (int)len, (int)len);
			if((int)dust.customData <=15)
				sb.Draw(surtrTexture,rect ,null,Color.White * ((int)dust.customData / 30f));
			else {
				sb.Draw(surtrTexture,rect ,null,Color.OrangeRed* (1-((int)dust.customData / 30f)));
			}
			return false;
		}
	}
}

