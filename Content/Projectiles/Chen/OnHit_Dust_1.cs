using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Chen
{
	public class OnHit_Dust_1 : ModDust
	{
		static Texture2D t1;
		static Texture2D t2;
		public override void Load() {
			t1 = ModContent.Request<Texture2D>("ArknightsMod/Content/Projectiles/Chen/OnHit_Dust_1", AssetRequestMode.ImmediateLoad).Value;
			t2 = ModContent.Request<Texture2D>("ArknightsMod/Content/Projectiles/Chen/OnHit_Dust_2", AssetRequestMode.ImmediateLoad).Value;

			base.Load();
		}
		public override void OnSpawn(Dust d) {
			d.color = Color.White;
			d.color.A = 0;
			d.scale = 0;
			d.alpha = 255;
			base.OnSpawn(d);
		}
		public override bool Update(Dust dust) {
			dust.scale = MathHelper.Lerp(dust.scale, 0.7f, 0.1f);
			dust.alpha -= 15;
			if (dust.alpha < 0)
				dust.active = false;
			return false;
		}
		public override bool PreDraw(Dust dust) {
			var sb = Main.spriteBatch;
			//for (int i = 0; i < 2; i++)
			{
				sb.Draw(t2, dust.position - Main.screenPosition, null, dust.color * (dust.alpha / 255f), 0, t2.Size() / 2f, dust.scale * 0.4f, default, 0);

				sb.Draw(t1, dust.position - Main.screenPosition, null, dust.color * (dust.alpha / 255f), 0, t1.Size() / 2f, dust.scale * 0.4f, default, 0);
			}
			return false;
		}
	}
}