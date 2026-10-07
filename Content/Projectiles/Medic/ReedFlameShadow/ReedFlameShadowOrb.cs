using ArknightsMod.Common.VisualEffects;
using ArknightsMod.Content.Buffs.Medic.ReedFlameShadow;
using ArknightsMod.Content.Items.Weapons.Medic.ReedFlameShadow;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Medic.ReedFlameShadow
{
	// ============================================================
	//  焰影苇草 二技能「枯荣共息」弹幕 —— 环绕 / 追踪 / 归位 三态
	//
	//  状态机：ai[0] = 状态、ai[1] = 槽位 0/1/2、ai[2] = 目标 NPC（-1 = 无）
	//    Orbiting  环绕：位置 = 玩家中心 + (相位 + slot×120°) 方向 × 160px，顺时针 2 秒一圈；
	//                    待命期间每次都按 600px 范围重新找最近的敌怪，找到就扑。
	//    Homing    追踪：平滑加速到 16px/帧、每帧最多转 6°，可穿墙，一直追到命中
	//                    （目标跑出 600px 也不放弃，只有目标死了才转归位）；
	//    Returning 归位：目标在命中前消失 → 飞回自己的 120° 槽位（途中照常可造成伤害）。
	//
	//  ⚠ 目标是"每颗弹幕各自持有"（ai[2]），不是全局共用一把锁：共用锁会让已经跑远的
	//    目标被后续补充出来的每一颗新弹幕继承，隔着大半张地图扑过去。待命的弹幕在同一帧
	//    算出的"最近敌怪"是同一个人，所以三颗实际上仍然是同时扑向同一目标。
	//
	//  存活与清除：
	//    · 自身没有任何寿命（timeLeft = int.MaxValue，且不随时间衰减），除"技能结束"外
	//      不会被主动清除——环绕、追踪、归位都不设超时；
	//    · 技能结束（SkillActive 变 false：20 秒到期 / 切走武器 / 切技能槽 / 死亡）→ 立即销毁。
	//      这个判定只由属主客户端做，销毁经网络同步到其它端（同 AstraSwordAura 的约定）。
	//
	//  多人：属主客户端负责全部模拟与状态切换；其它端不跑状态机，只跟随同步过来的
	//  位置与速度（ShouldUpdatePosition 在属主端对"环绕"返回 false，因为那一态是精确赋值）。
	//
	//  绘制只有一条顶点火流，不额外画 sprite 装饰：条带走的是顶点 + ZoomMatrix，
	//  而 spriteBatch.Draw 走屏幕空间，缩放不是 100% 时两者会错位成"一条线 + 线外一点"。
	// ============================================================
	public class ReedFlameShadowOrb : ModProjectile
	{
		// ── 状态 ────────────────────────────────────────────
		public const float StateOrbiting = 0f;
		public const float StateHoming = 1f;
		public const float StateReturning = 2f;

		// ── 追踪/归位飞行参数 ───────────────────────────────
		private const float HomingMaxSpeed = 16f;      // 追踪最高速（像素/帧）
		private const float HomingAccel = 0.55f;       // 每帧加速
		private const float MaxTurnPerTick = MathHelper.Pi / 30f; // 6°/帧
		private const float CloseRangeSlowdown = 140f; // 目标进入这个距离后按比例降速
		private const float CloseRangeMinSpeed = 4f;   // 降速下限：转弯半径必须小于距离，否则会绕着目标打转
		private const float ArriveDistance = 8f;       // 归位到位的判定距离

		// ── 绘制参数（红色系，拖尾缩短到 10 个记录点）────────
		private const string NoiseTexPath = "ArknightsMod/Content/Projectiles/Rogue/FireworksHand/NoiseTexture";

		private const int HistoryLength = 10;   // 记录多少个路径点
		private const float MinPointDist = 5f;  // 走够这么多像素才记一个新点（去重，保证线段不退化）
		private const int SubDivide = 3;        // 每段细分几份

		private const float BodyWidth = 8f;
		private const float HeadExtraWidth = 7f;
		private const float HeadBulgeLength = 0.18f;
		private const float TaperPower = 0.62f;
		private const float TailMinWidth = 0.22f;

		private const float WobbleAmplitude = 2.6f;
		private const float WobbleSpeed = 2.6f;
		private const float WobbleStart = 0.34f;

		private const float FlowScrollSpeed = 0.85f;
		private const float DissolveAmount = 0.5f;
		private const float DissolveStart = 0.55f;
		private const float NoiseScaleX = 2.0f;
		private const float NoiseScaleY = 1.0f;
		private const float TrailIntensity = 1.2f;
		private const float HeadRound = 0.09f;
		private const float PixelWarp = 0.10f;
		private const float TailBrightness = 0.26f;

		private const float GlowLength = 0.22f;
		private const float HeadHeat = 2.2f;
		private const float BodyHeat = 0.7f;

		// 配色：白热 → 橙红 → 深红（普攻是白→黄→褐，这里整体更红）
		private static readonly Vector3 HeadColor = new(1.00f, 0.94f, 0.82f);
		private static readonly Vector3 MidColor = new(1.00f, 0.42f, 0.12f);
		private static readonly Vector3 TailColor = new(0.42f, 0.07f, 0.05f);
		private static readonly Vector3 EdgeTint = new(1.00f, 0.34f, 0.14f);

		// 自己维护的路径历史（[0] = 最新 = 弹头）；改用 oldPos 会在弹幕减速时出现退化线段
		private readonly List<Vector2> _history = new();

		private Vector2 flowOffset;
		private Vector2 noiseSeed;
		private float wobblePhase;

		// 贴图不用自己的（本弹幕全程代码绘制），借普攻那张图占位，避免依赖不存在的素材
		public override string Texture =>
			"ArknightsMod/Content/Items/Weapons/Medic/ReedFlameShadow/ReedFlameShadowStaff";

		public override void SetDefaults()
		{
			Projectile.width = 18;
			Projectile.height = 18;
			Projectile.aiStyle = -1;
			Projectile.friendly = true;
			Projectile.hostile = false;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.penetrate = 1;             // 命中即消失
			Projectile.timeLeft = int.MaxValue;   // 不自然消失：只由"技能结束"清除
			Projectile.tileCollide = false;       // 环绕半径常贴着墙，允许穿墙才不会卡住
			Projectile.ignoreWater = true;
			Projectile.netImportant = true;
			Projectile.light = 0.7f;
		}

		public override void OnSpawn(IEntitySource source)
		{
			noiseSeed = new Vector2(Main.rand.NextFloat(), Main.rand.NextFloat());
			wobblePhase = Main.rand.NextFloat(MathHelper.TwoPi);
			_history.Clear();
			_history.Add(Projectile.Center);
		}

		public override bool ShouldUpdatePosition()
		{
			// 环绕状态在属主端是"精确赋值"到轨道点上，不能再让原版按速度积分一遍；
			// 其它端没有本地相位可用，只能靠同步速度插值推进，所以照常积分。
			if (Projectile.owner == Main.myPlayer && Projectile.ai[0] == StateOrbiting)
				return false;
			return true;
		}

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (!owner.active || owner.dead) {
				Projectile.Kill();
				return;
			}

			// "还拿着这把法杖吗"在所有端都成立（物品栏是同步的）：切走武器即销毁。
			// 这只是兜底，真正完整的"技能结束"判定在属主端（见 Simulate）。
			if (owner.HeldItem?.ModItem is not ReedFlameShadowStaff) {
				Projectile.Kill();
				return;
			}

			if (Projectile.owner == Main.myPlayer)
				Simulate(owner);
			else
				Projectile.rotation = Projectile.velocity.ToRotation();

			RecordPath();

			flowOffset.X -= FlowScrollSpeed * 0.016f;
			flowOffset.Y += 0.02f * 0.016f;
			wobblePhase += WobbleSpeed * 0.016f;

			SpawnEmbers();
			Lighting.AddLight(Projectile.Center, 1.05f, 0.42f, 0.16f);
		}

		/// <summary>
		/// 属主端的全部模拟（技能结束判定、索敌、三种运动）。
		/// <para>索敌规则里有两个容易踩的点：
		/// <br/>1. <b>目标记在弹幕自己身上</b>（<c>ai[2]</c>），不是全局共用一把锁——
		///    已经扑出去的弹幕要"追到命中"（目标跑出 600px 也照追），但新补充/刚归位的弹幕
		///    必须<b>重新按 600px 范围</b>找目标，否则目标一旦跑远，后面每一颗新弹幕都会
		///    继承那个陈旧的目标、隔着大半张地图飞过去。
		/// <br/>2. 索敌只在<b>环绕待命</b>状态下进行（电平触发）：范围内有敌怪，待命的弹幕立即扑上去；
		///    同一帧里几颗待命弹幕算出的"最近敌怪"是同一个人，所以三颗实际上是同时扑向同一目标。</para>
		/// </summary>
		private void Simulate(Player owner)
		{
			var weaponPlayer = owner.GetModPlayer<ReedFlameShadowStaffPlayer>();
			if (!weaponPlayer.S2IsActive()) {
				// 技能结束（含切走武器 / 切技能槽 / 死亡）→ 清空，其余任何时候都不主动清除
				Projectile.Kill();
				return;
			}

			int slot = Math.Clamp((int)Projectile.ai[1], 0, ReedFlameShadowStaffPlayer.OrbSlotCount - 1);
			if (slot != (int)Projectile.ai[1]) {
				Projectile.ai[1] = slot;
				Projectile.netUpdate = true;
			}

			int state = (int)Projectile.ai[0];
			int targetIndex = (int)Projectile.ai[2];

			switch (state) {
				case (int)StateHoming:
					// 锁定后一直追到命中，不因为目标跑出 600px 而放弃；目标死了才归位
					if (!IsValidTarget(targetIndex, owner)) {
						targetIndex = -1;
						state = (int)StateReturning;
					}
					break;

				case (int)StateOrbiting:
					// 待命中的弹幕：每次重新按 600px 范围内选最近的敌怪
					targetIndex = FindNearestTarget(owner);
					if (targetIndex >= 0)
						state = (int)StateHoming;
					break;
			}

			if (state == (int)StateOrbiting) {
				Orbit(owner, slot);
			}
			else if (state == (int)StateHoming) {
				Homing(targetIndex);
			}
			else if (ReturnToSlot(owner, slot)) {
				// 归位完成 → 回到待命（下一帧才会重新索敌）
				state = (int)StateOrbiting;
				Orbit(owner, slot);
			}

			if (targetIndex != (int)Projectile.ai[2]) {
				Projectile.ai[2] = targetIndex;
				Projectile.netUpdate = true;
			}

			if (state != (int)Projectile.ai[0]) {
				Projectile.ai[0] = state;
				Projectile.netUpdate = true;
			}

			Projectile.rotation = Projectile.velocity.ToRotation();
			// 连续转向的弹幕需要持续同步位置/速度，原版的 netSpam 会自动限流
			Projectile.netUpdate = true;
		}

		/// <summary>目标是否仍然可用（没死、没消失、还能被追击）。</summary>
		private static bool IsValidTarget(int whoAmI, Player owner)
		{
			if (whoAmI < 0 || whoAmI >= Main.maxNPCs)
				return false;

			NPC npc = Main.npc[whoAmI];
			return npc.active && npc.life > 0 && !npc.friendly && !npc.dontTakeDamage
				&& npc.CanBeChasedBy(owner);
		}

		/// <summary>玩家 <see cref="ReedFlameShadowStaffPlayer.TargetSearchRange"/> 范围内最近的可追击敌怪。</summary>
		private static int FindNearestTarget(Player owner)
		{
			int best = -1;
			float bestDistance = ReedFlameShadowStaffPlayer.TargetSearchRange * ReedFlameShadowStaffPlayer.TargetSearchRange;
			Vector2 center = owner.MountedCenter;

			foreach (NPC npc in Main.ActiveNPCs) {
				if (!npc.CanBeChasedBy(owner))
					continue;

				float distance = npc.DistanceSQ(center);
				if (distance > bestDistance)
					continue;

				bestDistance = distance;
				best = npc.whoAmI;
			}

			return best;
		}

		/// <summary>环绕：位置锁死在 160px 轨道上，速度取切向（拖尾朝向 + 其它端插值用）。</summary>
		private void Orbit(Player owner, int slot)
		{
			var weaponPlayer = owner.GetModPlayer<ReedFlameShadowStaffPlayer>();
			Vector2 desired = ReedFlameShadowStaffPlayer.SlotPosition(owner, weaponPlayer.S2OrbitPhase, slot);

			Projectile.velocity = desired - Projectile.Center;
			Projectile.Center = desired;
		}

		/// <summary>追踪：朝自己锁定的目标平滑加速 + 限速转向，一直追到命中。</summary>
		private void Homing(int targetIndex)
		{
			NPC target = Main.npc[targetIndex];
			Vector2 toTarget = target.Center - Projectile.Center;
			float distance = toTarget.Length();

			float currentRotation = Projectile.velocity.ToRotation();
			float turn = MathHelper.Clamp(MathHelper.WrapAngle(toTarget.ToRotation() - currentRotation),
				-MaxTurnPerTick, MaxTurnPerTick);

			// 目标贴近而朝向偏差大时降速：速度 16 配 6°/帧 的转弯半径约 153px，
			// 不降速的话弹幕会绕着目标转圈而不是打中它。
			float speedCap = HomingMaxSpeed;
			if (distance < CloseRangeSlowdown)
				speedCap = MathHelper.Lerp(CloseRangeMinSpeed, HomingMaxSpeed, distance / CloseRangeSlowdown);

			float speed = Math.Min(Projectile.velocity.Length() + HomingAccel, speedCap);
			Projectile.velocity = (currentRotation + turn).ToRotationVector2() * speed;
		}

		/// <summary>归位：飞回自己的槽位；返回 true 表示已到位（可以重新待命/再次出击）。</summary>
		private bool ReturnToSlot(Player owner, int slot)
		{
			var weaponPlayer = owner.GetModPlayer<ReedFlameShadowStaffPlayer>();
			Vector2 desired = ReedFlameShadowStaffPlayer.SlotPosition(owner, weaponPlayer.S2OrbitPhase, slot);
			Vector2 toSlot = desired - Projectile.Center;

			float distance = toSlot.Length();
			if (distance <= ArriveDistance)
				return true;

			// 比例导引：位移即速度，必定收敛到槽位（不像限速转向那样可能绕着槽位转圈）
			Projectile.velocity = distance > HomingMaxSpeed
				? toSlot / distance * HomingMaxSpeed
				: toSlot;
			return false;
		}

		// 只有走够 MinPointDist 才记新点：从源头杜绝重合点导致的退化线段
		private void RecordPath()
		{
			Vector2 now = Projectile.Center;
			if (_history.Count == 0) {
				_history.Add(now);
				return;
			}

			if (Vector2.DistanceSquared(now, _history[0]) >= MinPointDist * MinPointDist) {
				_history.Insert(0, now);
				if (_history.Count > HistoryLength)
					_history.RemoveAt(_history.Count - 1);
			}
			else {
				_history[0] = now;
			}
		}

		private void SpawnEmbers()
		{
			if (Main.dedServ)
				return;

			if (Main.rand.NextBool(2)) {
				Dust d = Dust.NewDustDirect(
					Projectile.position, Projectile.width, Projectile.height,
					DustID.Torch, 0f, 0f, 100,
					default, Main.rand.NextFloat(0.8f, 1.4f));
				d.noGravity = true;
				d.velocity = -Projectile.velocity * Main.rand.NextFloat(0.02f, 0.08f)
					+ Main.rand.NextVector2Circular(1.0f, 1.0f);
				d.fadeIn = 0.8f;
			}

			if (Main.rand.NextBool(5)) {
				Dust e = Dust.NewDustDirect(
					Projectile.position, Projectile.width, Projectile.height,
					DustID.GoldFlame, 0f, 0f, 160,
					default, Main.rand.NextFloat(0.6f, 1.0f));
				e.noGravity = true;
				e.velocity = -Projectile.velocity * 0.06f + Main.rand.NextVector2Circular(0.7f, 0.7f);
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			// 与普攻一致：不施加原版灼烧（避免和灼痕混淆），只保留 30% 概率的灼痕
			// （已有灼痕不刷新剩余时间）
			if (!target.friendly
				&& !target.HasBuff(ModContent.BuffType<BurnedScar>())
				&& Main.rand.NextFloat() < 0.30f)
				target.AddBuff(ModContent.BuffType<BurnedScar>(), 8 * 60);

			FlameBurst(14);
		}

		public override void OnKill(int timeLeft)
		{
			FlameBurst(20);
			SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.35f, Pitch = 0.45f }, Projectile.Center);
		}

		private void FlameBurst(int count)
		{
			if (Main.dedServ)
				return;

			for (int i = 0; i < count; i++) {
				Dust d = Dust.NewDustDirect(
					Projectile.position, Projectile.width, Projectile.height,
					Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.Torch,
					0f, 0f, 100, default, Main.rand.NextFloat(1.0f, 1.7f));
				d.noGravity = true;
				d.velocity = Main.rand.NextVector2Circular(4.5f, 4.5f);
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			DrawFlameTrail();
			return false;
		}

		// 两层不同频率的正弦叠加 = 有机摆动
		private float Wobble(float along)
		{
			return MathF.Sin(along * 5.4f - wobblePhase) * 0.72f
				 + MathF.Sin(along * 11.2f - wobblePhase * 1.6f) * 0.28f;
		}

		// ── 弹头热核 + 整条火流：一次加法混合里画完 ──────────
		private void DrawFlameTrail()
		{
			Effect fx = ArknightsMod.ReedFlameTrail?.Value;
			Texture2D noiseTex = ModContent.Request<Texture2D>(NoiseTexPath).Value;
			if (fx == null || noiseTex == null || _history.Count < 2)
				return;

			// 1) 细分成更密的采样点
			var pts = new List<Vector2>((_history.Count - 1) * SubDivide + 1);
			for (int i = 0; i < _history.Count - 1; i++) {
				for (int s = 0; s < SubDivide; s++) {
					float t = s / (float)SubDivide;
					pts.Add(Vector2.Lerp(_history[i], _history[i + 1], t));
				}
			}
			pts.Add(_history[^1]);
			if (pts.Count < 3)
				return;

			// 2) 生成条带顶点
			var bars = new List<Vertex>(pts.Count * 2);
			int last = pts.Count - 1;
			Vector2 prevNormal = Vector2.Zero;

			for (int i = 0; i <= last; i++) {
				float along = i / (float)last;   // 0 弹头 → 1 尾端

				int a = Math.Max(i - 1, 0);
				int b = Math.Min(i + 1, last);
				Vector2 tangent = pts[a] - pts[b];

				if (tangent.LengthSquared() < 0.0001f) {
					if (prevNormal == Vector2.Zero)
						continue;
					AddPair(bars, pts[i], prevNormal, along);
					continue;
				}

				tangent.Normalize();
				Vector2 normal = new(-tangent.Y, tangent.X);

				// 防止法线突然翻面导致三角带自交成蝴蝶结
				if (prevNormal != Vector2.Zero && Vector2.Dot(normal, prevNormal) < 0f)
					normal = -normal;
				prevNormal = normal;

				AddPair(bars, pts[i], normal, along);
			}

			if (bars.Count < 3)
				return;

			// 3) 绘制
			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
				SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone);

			// 弹头形态完全交给火流条带自己（HeadExtraWidth / HeadHeat / HeadRound 已经在
			// 最前面鼓出一个圆亮头）。这里不再另画"热核"圆点：那个点走的是 spriteBatch 的
			// 屏幕空间，而条带走的是顶点 + ZoomMatrix，缩放不是 100% 时两者会错位成
			// "一条线 + 线外一点"。要加装饰也只能加在条带这一侧。
			Matrix projection = Matrix.CreateOrthographicOffCenter(
				0f, Main.screenWidth, Main.screenHeight, 0f, 0f, 1f);
			Matrix model = Matrix.CreateTranslation(
					new Vector3(-Main.screenPosition.X, -Main.screenPosition.Y, 0f))
				* Main.GameViewMatrix.ZoomMatrix;

			fx.Parameters["uTransform"]?.SetValue(model * projection);
			fx.Parameters["tex0"]?.SetValue(noiseTex);
			fx.Parameters["uFlowOffset"]?.SetValue(flowOffset + noiseSeed);
			fx.Parameters["uNoiseScale"]?.SetValue(new Vector2(NoiseScaleX, NoiseScaleY));
			fx.Parameters["uDissolveAmount"]?.SetValue(DissolveAmount);
			fx.Parameters["uDissolveStart"]?.SetValue(DissolveStart);
			fx.Parameters["uIntensity"]?.SetValue(TrailIntensity);
			fx.Parameters["uHeadRound"]?.SetValue(HeadRound);
			fx.Parameters["uPixelWarp"]?.SetValue(PixelWarp);
			fx.Parameters["uTailBrightness"]?.SetValue(TailBrightness);
			fx.Parameters["uGlowLength"]?.SetValue(GlowLength);
			fx.Parameters["uHeadHeat"]?.SetValue(HeadHeat);
			fx.Parameters["uBodyHeat"]?.SetValue(BodyHeat);
			fx.Parameters["uHeadColor"]?.SetValue(HeadColor);
			fx.Parameters["uMidColor"]?.SetValue(MidColor);
			fx.Parameters["uTailColor"]?.SetValue(TailColor);
			fx.Parameters["uEdgeTint"]?.SetValue(EdgeTint);
			fx.CurrentTechnique.Passes["FlameTrail"].Apply();

			Main.graphics.GraphicsDevice.DrawUserPrimitives(
				PrimitiveType.TriangleStrip, bars.ToArray(), 0, bars.Count - 2);

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
				Main.DefaultSamplerState, DepthStencilState.None,
				RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
		}

		// 在给定路径点处，按彗星宽度剖面 + 尾部摆动，生成条带两侧的一对顶点
		private void AddPair(List<Vertex> bars, Vector2 point, Vector2 normal, float along)
		{
			float bodyTaper = MathF.Pow(1f - along, TaperPower);
			bodyTaper = MathHelper.Lerp(TailMinWidth, 1f, bodyTaper);
			float headBulge = MathHelper.Clamp(1f - along / HeadBulgeLength, 0f, 1f);
			headBulge *= headBulge;
			float w = BodyWidth * bodyTaper + HeadExtraWidth * headBulge;

			float wobbleWeight = MathHelper.Clamp((along - WobbleStart) / (1f - WobbleStart), 0f, 1f);
			float amp = WobbleAmplitude * wobbleWeight * wobbleWeight;
			Vector2 center = point + normal * Wobble(along) * amp;

			bars.Add(new Vertex(center + normal * w, new Vector3(along, 1f, 1f), Color.White));
			bars.Add(new Vertex(center - normal * w, new Vector3(along, 0f, 1f), Color.White));
		}
	}
}
