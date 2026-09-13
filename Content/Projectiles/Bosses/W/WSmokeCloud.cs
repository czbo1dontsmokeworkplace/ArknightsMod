using ArknightsMod.Content.NPCs.Enemy.W;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Bosses.W
{
	/// <summary>
	/// 烟幕射击留下的烟雾区（二阶段被动）：无伤害标记物。<br/>
	/// W 处于任意烟雾内获得 60%（大师 90%）受击闪避——判定在 WBoss.ModifyIncomingHit。<br/>
	/// ai[0]=W 的 whoAmI（-1 无主，2 秒自散）；ai[1]=半径(px)；ai[2]=点火倒数（0 未点火）。W 存活期间刷新寿命，boss 战结束后自然消散。<br/>
	/// 「烟幕地狱」：W 点火后烟团由灰转橙、噼啪作响，倒数归零炸成火球（半径 = 烟半径 + 40）并留一圈余火——她的防御资源变成陷阱。
	/// </summary>
	public class WSmokeCloud : ModProjectile
	{
		public override string Texture => "ArknightsMod/Content/Projectiles/Bosses/W/WSmoke";

		private float Radius => Math.Max(Projectile.ai[1], 24f);
		public bool Igniting => Projectile.ai[2] > 0f;
		/// <summary>点火进度 0~1（各端按见过的最大倒数值算）</summary>
		private float IgniteProgress => Igniting ? 1f - Projectile.ai[2] / Math.Max(Projectile.localAI[0], 1f) : 0f;

		public override void SetDefaults() {
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.hostile = false;
			Projectile.friendly = false;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 120;
			Projectile.aiStyle = -1;
		}

		/// <summary>点火：ticks 帧后炸（仅服务端/单机端；已点火且更短则保留）</summary>
		public void Ignite(int ticks, int damage) {
			if (Igniting && Projectile.ai[2] <= ticks)
				return;
			Projectile.ai[2] = ticks;
			Projectile.damage = damage;
			Projectile.netUpdate = true;
		}

		public override void AI() {
			int ownerIdx = (int)Projectile.ai[0];
			if (ownerIdx >= 0 && ownerIdx < Main.maxNPCs) {
				NPC owner = Main.npc[ownerIdx];
				if (owner.active && owner.type == ModContent.NPCType<WBoss>())
					Projectile.timeLeft = 120; // boss 在场 → 烟不散
			}

			float radius = Radius;
			if (Igniting) {
				if (Projectile.ai[2] > Projectile.localAI[0])
					Projectile.localAI[0] = Projectile.ai[2];
				Projectile.ai[2]--;
				float p = IgniteProgress;
				if (!Main.dedServ) {
					Lighting.AddLight(Projectile.Center, 1.0f * p, 0.45f * p, 0.1f * p);
					// 噼啪：火星从烟里蹦出来，越近越密
					int sparks = 1 + (int)(p * 3f);
					for (int i = 0; i < sparks; i++) {
						Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(radius * 0.7f, radius * 0.7f), DustID.Torch,
							new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), -Main.rand.NextFloat(0.8f, 2.2f)), 0, default, Main.rand.NextFloat(0.9f, 1.5f));
						d.noGravity = true;
					}
					int interval = (int)MathHelper.Clamp(12f - p * 9f, 3f, 12f);
					if ((int)Projectile.ai[2] % interval == 0)
						SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.35f, Pitch = 0.2f + p * 0.4f }, Projectile.Center);
				}
				if (Projectile.ai[2] <= 0f && Main.netMode != NetmodeID.MultiplayerClient) {
					Detonate();
					Projectile.Kill();
					return;
				}
			}
			else if (!Main.dedServ && Main.rand.NextBool(3)) {
				Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(radius * 0.8f, radius * 0.8f);
				Dust d = Dust.NewDustPerfect(pos, DustID.Smoke, new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), Main.rand.NextFloat(-0.5f, -0.1f)), 150, default, Main.rand.NextFloat(0.9f, 1.5f));
				d.color = Color.Lerp(new Color(96, 88, 88), new Color(170, 70, 60), Main.rand.NextFloat(0.2f, 0.7f));
				d.noGravity = true;
				d.fadeIn = 0.3f;
			}
		}

		/// <summary>烟团炸成火球 + 一圈余火（仅服务端/单机端）</summary>
		private void Detonate() {
			Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
				ModContent.ProjectileType<WExplosion>(), Math.Max(1, Projectile.damage), 6f, Main.myPlayer,
				ai0: Radius + 40f, ai1: 0f, ai2: 0f);
			int emberDamage = Math.Max(1, (int)(Projectile.damage * 0.4f));
			for (int i = 0; i < 5; i++) {
				float t = (i + 0.5f) / 5f * 2f - 1f;
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, new Vector2(t * 5f, -3f - (1f - Math.Abs(t)) * 1.5f),
					ModContent.ProjectileType<WEmber>(), emberDamage, 0f, Main.myPlayer);
			}
		}

		public override bool PreDraw(ref Color lightColor) {
			// 三层软烟叠绘：错相位缓动，读得出范围又不糊死画面；点火后由灰转橙、越来越亮
			Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
			float radius = Radius;
			float fade = MathHelper.Clamp(Projectile.timeLeft / 60f, 0f, 1f);
			float p = IgniteProgress;
			Vector2 origin = tex.Size() * 0.5f;
			Color baseColor = Color.Lerp(new Color(120, 90, 90), new Color(255, 140, 60), p);
			for (int i = 0; i < 3; i++) {
				float t = Main.GlobalTimeWrappedHourly * (0.6f + i * 0.25f + p * 1.5f) + i * 2.1f;
				Vector2 offset = new((float)Math.Sin(t) * radius * 0.25f, (float)Math.Cos(t * 0.8f) * radius * 0.18f);
				float scale = radius * 2f / tex.Width * (0.75f + 0.15f * i + 0.06f * (float)Math.Sin(t * 1.7f));
				// 三层各自慢慢反向旋转，烟才像在翻卷而不是三张贴纸
				float rot = Main.GlobalTimeWrappedHourly * 0.18f * (i % 2 == 0 ? 1f : -1f) + i * 1.3f;
				Color color = baseColor * ((0.38f - i * 0.08f) * (1f + 0.6f * p)) * fade;
				Main.EntitySpriteDraw(tex, Projectile.Center + offset - Main.screenPosition, null,
					color, rot, origin, scale, SpriteEffects.None, 0);
			}
			if (p > 0f) {
				// 点火核心：一团越来越亮的火光在烟心里跳
				float flicker = 0.8f + 0.2f * MathF.Sin(Main.GlobalTimeWrappedHourly * 25f);
				Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null,
					new Color(255, 120, 40, 0) * (0.7f * p * flicker), 0f, origin, radius * 1.2f / tex.Width, SpriteEffects.None, 0);
			}
			return false;
		}
	}
}
