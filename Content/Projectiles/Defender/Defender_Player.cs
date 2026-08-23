using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.SwingHelper;
using Terraria.GameContent;

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

		private Texture2D shieldTex => ModContent.Request<Texture2D>("ArknightsMod/Content/Projectiles/Defender/Shield_tex").Value;
		private Texture2D noiseTex => ModContent.Request<Texture2D>("ArknightsMod/Content/Projectiles/Defender/ShieldNoise_tex").Value;
		public static Effect shieldFx;


		public Color ParryThemeColor = new Color(255, 200, 130);

		#region 格挡特效
		// 时间轴（总 4 秒 = 240 帧，< 5s）：
		//   0~2s   主盾：出现发光 → 变大(0.6→1.4) → 同时消融消散(0→1)
		//   2~4s   第二层盾：更透明(0.45)，放大更快(0.8→2.5)，最后淡出
		private int parryTimer = -1;
		private const int ParryDuration = 240;

		private static float EaseOutBack(float x)
		{
			const float c1 = 1.70158f;
			const float c3 = c1 + 1f;
			return 1f + c3 * MathF.Pow(x - 1f, 3f) + c1 * MathF.Pow(x - 1f, 2f);
		}
		private static float EaseOutCubic(float x) => 1f - MathF.Pow(1f - x, 3f);
		public bool openEffect = false;

		public void TriggerParry()
		{
			parryTimer = 0;
			openEffect = true;
		}
		/// <summary>
		/// 画盾（纯贴图 sb.Draw + 消融 shader，effect 交给 spriteBatch 管理）
		/// </summary>
		private void DrawShield(Vector2 center, float scale, Color color,float uTime = 0)
		{
			var sb = Main.spriteBatch;
			sb.End();
			sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,SamplerState.LinearWrap
				,DepthStencilState.None,RasterizerState.CullNone,shieldFx);
			// 投影矩阵手动传给 uTransform（shader 参数名是 uTransform，与刀光 shader 同款）
			Matrix projection = Matrix.CreateOrthographicOffCenter(0f, Main.screenWidth, Main.screenHeight, 0f, 0f, 1f);
			projection = Main.GameViewMatrix.ZoomMatrix * projection;
			shieldFx.Parameters["uTransform"].SetValue(projection);
			shieldFx.Parameters["uTime"].SetValue(uTime);
			shieldFx.Parameters["uNoiseScale"].SetValue(2.0f);
			shieldFx.Parameters["uEdgeColor"].SetValue(ParryThemeColor.ToVector3());
			Main.graphics.GraphicsDevice.Textures[1] = noiseTex;

			var origin = shieldTex.Size() * 0.5f;
			sb.Draw(shieldTex, center, null, color, 0, origin, scale, SpriteEffects.None, 0f);

			sb.End();
			sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
		}

		public void DrawEffect() {
			if (!openEffect)
				return;
			parryTimer++;

			if (parryTimer > ParryDuration) {
				parryTimer = -1;
				openEffect = false;
				return;
			}
			float t = parryTimer / (float)ParryDuration;
			Vector2 center = Player.Center - Main.screenPosition;

			float mt = t / 0.5f;

			float pop = Math.Min(mt / 0.15f, 1f);
			float popScale = MathHelper.Lerp(0.5f, 1.1f, EaseOutBack(pop));
			float grow = Math.Max(0f, mt - 0.15f) / 0.85f;
			float growScale = MathHelper.Lerp(0.08f, 0.13f, EaseOutCubic(grow));
			float scale = popScale * growScale;

			float glow = 0.8f - mt * 0.4f;
			Color col = Color.White * glow;
			DrawShield(center, scale, col,MathF.Max(parryTimer/60f-2f,0));
			Lighting.AddLight(Player.Center, new Vector3(1f, 0.7f, 0.4f) * (0.4f * glow));
			if(t>=0.5f)
			{
				float st = (t - 0.5f) / 0.5f;

				float grow1 = Math.Min(st / 0.6f, 1f);
				float scale1 = MathHelper.Lerp(0.2f, 0.3f, EaseOutCubic(grow1));
				float alpha = st < 0.6f ? 0.15f : 0.15f * (1f - (st - 0.6f) / 0.4f);

				DrawShield(center, scale1, Color.White * alpha);
			}
		}
		#endregion

		public override void ModifyHitByNPC(NPC npc, ref Player.HurtModifiers modifiers) {
			if (ShieldMode&&CD==0) {
				if (MathF.Sign(npc.Center.X-Player.Center.X) == Player.direction||npc.Center.Distance(Player.Center) <= 3) {
					modifiers.FinalDamage *= 0.5f;
					Player.AddBuff(BuffID.ParryDamageBuff,5*60);
					TriggerParry();
				}
				CD = 10 * 60;
			}
		}

		public override void ModifyHitByProjectile(Projectile proj, ref Player.HurtModifiers modifiers) {
			if (ShieldMode&&CD==0) {
				if (MathF.Sign(proj.Center.X-Player.Center.X) == Player.direction) {
					modifiers.FinalDamage *= 0.5f;
					Player.AddBuff(BuffID.ParryDamageBuff,5*60);
					TriggerParry();
				}
				CD = 10 * 60;
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
