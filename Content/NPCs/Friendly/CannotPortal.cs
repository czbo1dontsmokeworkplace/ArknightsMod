using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Friendly
{
	/// <summary>
	/// 坎诺特召唤增援时用的传送门：在玩家屏幕范围内从地面"长"出来，随后从里面放出一只怪物，
	/// 再合上消失。取代了原来"在屏幕外直接刷怪"的做法。
	///
	/// 贴图 CannotPortal.png 是一张 190x321、共 21 帧的竖向帧图，本身是一个一直循环的
	/// "黑色椭圆 + 红色锯齿边缘 + 飘散的红白粒子"，并不自带开启/关闭动画，
	/// 所以"从地面长出来 / 缩回去"是这里用缩放做出来的。
	///
	/// ai[0] = 要放出来的怪物类型；ai[1] = 召唤者（坎诺特）的 NPC 下标；ai[2] = 目标玩家下标。
	/// 生成怪物只在服务器/单人侧做（多人客户端只负责画）。
	/// </summary>
	public class CannotPortal : ModProjectile
	{
		private const int FrameWidth = 190;
		private const int FrameHeight = 321;
		private const int FrameCount = 21;
		private const int TicksPerFrame = 3;

		// 原图 190x321 放到世界里太大了（约 12x20 格，是玩家的好几倍），缩到比最高的精英怪
		// （冰裂者 60px）略高一点。
		private const float DrawScale = 0.33f;
		public static int PortalWidth => (int)(FrameWidth * DrawScale);
		public static int PortalHeight => (int)(FrameHeight * DrawScale);

		private const int OpenTicks = 20;        // 从地面长出来
		private const int SpawnTick = 26;        // 开到差不多时放出怪物
		private const int CloseStartTick = 56;   // 怪物出来后开始合上
		private const int TotalTicks = 76;

		private int Age => (int)Projectile.localAI[0];

		public override void SetDefaults() {
			Projectile.width = PortalWidth;
			Projectile.height = PortalHeight;
			Projectile.friendly = false;
			Projectile.hostile = false;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.penetrate = -1;
			Projectile.aiStyle = -1;
			Projectile.timeLeft = TotalTicks + 30;
		}

		public override bool? CanDamage() => false;

		public override bool ShouldUpdatePosition() => false;

		public override void AI() {
			Projectile.velocity = Vector2.Zero;
			Projectile.localAI[0]++;
			int age = Age;

			float strength = GetOpenAmount(age);
			Lighting.AddLight(Projectile.Center, 0.9f * strength, 0.08f * strength, 0.08f * strength);

			if (age == SpawnTick) {
				SpawnBurst();
				SpawnMonster();
			}

			if (age >= TotalTicks)
				Projectile.Kill();
		}

		// 多人下：只有服务器（或单人）真正生成 NPC，客户端收到的是同步过来的 NPC。
		private void SpawnMonster() {
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			int type = (int)Projectile.ai[0];
			int cannot = (int)Projectile.ai[1];
			int target = (int)Projectile.ai[2];
			if (type <= 0)
				return;

			int targetIndex = (uint)target < Main.maxPlayers ? target : 255;
			int index = NPC.NewNPC(Projectile.GetSource_FromAI(), (int)Projectile.Center.X, (int)Projectile.Bottom.Y, type, Target: targetIndex);
			if ((uint)index >= Main.maxNPCs)
				return;

			Main.npc[index].GetGlobalNPC<CannotSummonedTag>().OwnerCannot = cannot;
		}

		private void SpawnBurst() {
			if (Main.dedServ)
				return;

			for (int i = 0; i < 18; i++) {
				Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(PortalWidth * 0.45f, PortalHeight * 0.45f);
				Dust dust = Dust.NewDustPerfect(pos, DustID.RedTorch, Main.rand.NextVector2Circular(2.5f, 2.5f), 100, default, Main.rand.NextFloat(1f, 1.7f));
				dust.noGravity = true;
			}
		}

		// 0（完全合上）~ 1（完全张开）
		private static float GetOpenAmount(int age) {
			float open = age < OpenTicks ? EaseOut(age / (float)OpenTicks) : 1f;
			float close = age >= CloseStartTick
				? 1f - EaseIn((age - CloseStartTick) / (float)(TotalTicks - CloseStartTick))
				: 1f;
			return MathHelper.Clamp(open * close, 0f, 1f);
		}

		private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
		private static float EaseIn(float t) => t * t;

		// 画在 NPC 后面：怪物从传送门里走出来，而不是被门盖住。
		public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) {
			behindNPCs.Add(index);
		}

		public override bool PreDraw(ref Color lightColor) {
			float open = GetOpenAmount(Age);
			if (open <= 0.01f)
				return false;

			Texture2D texture = TextureAssets.Projectile[Type].Value;
			int frame = (Age / TicksPerFrame) % FrameCount;
			var source = new Rectangle(0, frame * FrameHeight, FrameWidth, FrameHeight);

			// 以贴图底边中点为原点：门是贴着地面向上"长"出来的，而不是从中心向四周鼓开。
			var origin = new Vector2(FrameWidth / 2f, FrameHeight);
			Vector2 position = new Vector2(Projectile.Center.X, Projectile.Bottom.Y) - Main.screenPosition;
			var scale = new Vector2(DrawScale * (0.35f + 0.65f * open), DrawScale * open);

			// 传送门自己发光，不受环境光照影响，直接用纯白（再按开合程度淡入淡出）。
			Main.EntitySpriteDraw(texture, position, source, Color.White * MathHelper.Clamp(open * 1.5f, 0f, 1f), 0f, origin, scale, SpriteEffects.None);
			return false;
		}
	}
}
