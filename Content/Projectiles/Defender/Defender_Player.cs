using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Defender
{
	public class Defender_Player : ModPlayer
	{
		/// <summary>
		/// 举盾
		/// </summary>
		public bool ShieldMode = false;
		public int CD = 0;
		public bool OpenDefender = false;
		private bool ParryEffect = false;
		private Texture2D shieldTex => ModContent.Request<Texture2D>("ArknightsMod/Content/Projectiles/Defender/Shield").Value;

		public override void ModifyHitByNPC(NPC npc, ref Player.HurtModifiers modifiers) {
			if (ShieldMode&&CD==0) {
				if (MathF.Sign(npc.Center.X-Player.Center.X) == Player.direction||npc.Center.Distance(Player.Center) <= 3) {
					modifiers.FinalDamage *= 0.5f;
					Player.AddBuff(BuffID.ParryDamageBuff,5*60);
					ParryEffect = true;
				}
				CD = 10 * 60;
			}
		}

		public override void ModifyHitByProjectile(Projectile proj, ref Player.HurtModifiers modifiers) {
			if (ShieldMode&&CD==0) {
				if (MathF.Sign(proj.Center.X-Player.Center.X) == Player.direction) {
					modifiers.FinalDamage *= 0.5f;
					Player.AddBuff(BuffID.ParryDamageBuff,5*60);
						ParryEffect = true;
				}
				CD = 10 * 60;
			}
		}
		private int time=5;
		public void DrawLight() {
			if (ParryEffect) {
				float scale = MathHelper.Lerp(0.1f,0.08f,time / 5);
				var origin = new Vector2(shieldTex.Width / 2f, shieldTex.Height / 2f);
				Main.spriteBatch.Draw(shieldTex, Player.Center - Main.screenPosition,
					null, Color.White * 0.3f, 0, origin,scale, SpriteEffects.None, 0f);
				Lighting.AddLight(Player.Center,Color.White.ToVector3()/3f);
				time--;
				if (time <= 0) {
					ParryEffect = false;
					time = 5;
				}
			}
		}
		public override void UpdateEquips()
		{
			if(OpenDefender&&Main.mouseRight)
			{
				Player.statDefense *= 1.2f;
				Player.noKnockback = true;
				Player.moveSpeed *= 0.4f;
				ShieldMode = true;
			}

			if (ShieldMode && !Main.mouseRight) {
				ShieldMode = false;
				if (CD == 0)
					CD = 5 * 60;
			}
			if(CD >0)
				CD--;
		}

		public override void ResetEffects() {
			OpenDefender = true;
		}
	}

}
