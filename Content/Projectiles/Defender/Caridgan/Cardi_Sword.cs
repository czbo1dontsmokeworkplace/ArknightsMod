using ArknightsMod.Content.Items.Weapons.Defender.Cardigan;
using ArknightsMod.Content.Projectiles.Defender.Durnar;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RuneSKill.Content.NeedTool;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Defender.Caridgan;

public class Cardi_Sword : ModProjectile
{
	// 使用新的持握弹幕贴图：刀尖朝右，刀柄在左侧，旋转角度即世界坐标下的实际朝向角
	public override string Texture => "ArknightsMod/Content/Projectiles/Defender/Caridgan/Cardi_Sword_protile";

	Player player => Main.player[Projectile.owner];
	Item item => player.HeldItem;

	private const float Reach = 96f; // 攻击有效长度，独立于贴图大小，适当拉长以弥补贴图过短
	private const float DrawScale = 1.6f; // 持握时的绘制缩放，配合 Reach 拉长后的视觉表现
	private const float ThrowDistance = 200f; // 回旋投掷的最大飞出距离

	private ProjMode projMode = ProjMode.Move;
	private int comboStep; // 0 = 下劈，1 = 戳刺，2 = 回旋投掷，三种攻击依次循环
	private int attackTime;
	private int attackMaxTime; // 与 Item.useAnimation 同步，保证攻击频率与挥砍动画一致

	private float mouseRad;
	private float walkPhase;
	private float throwSpin;
	private bool hasHit; // 保证劈砍/戳刺每次只造成一次判定；回旋投掷不受此限制，路径上所有目标都可命中
	private bool throwReturning;
	private Vector2 handPos;
	private Vector2 swordEnd;

	public override void SetDefaults() {
		Projectile.width = 10;
		Projectile.height = 10;
		Projectile.friendly = true;
		Projectile.penetrate = -1;
		Projectile.tileCollide = false;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.ownerHitCheck = true;
		Projectile.DamageType = DamageClass.MeleeNoSpeed;
		Projectile.ignoreWater = true;
		Projectile.localNPCHitCooldown = 10;
	}

	public override void AI() {
		if (player.dead || !player.active || item.type != ModContent.ItemType<CardiganShield>()) {
			Projectile.Kill();
			return;
		}
		Projectile.timeLeft = 2;
		switch (projMode) {
			case ProjMode.Move:
				Move();
				break;
			case ProjMode.Attack:
				Attack();
				break;
		}
	}

	// SetCompositeArmBack 接受的并非世界坐标旋转角，而是“以玩家朝向为基准的相对角”；
	// 我们的刀身旋转 Projectile.rotation 始终是标准世界角(0=朝右)，需要换算后再喂给手臂 IK。
	private float ToArmRot(float worldRot) {
		return worldRot + MathHelper.PiOver2 - MathHelper.PiOver2 * player.direction;
	}

	public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
		if (projMode != ProjMode.Attack)
			return false;
		if (comboStep == 2) {
			// 回旋投掷：飞行路径上的所有目标都应造成伤害，不做单次命中限制
			Rectangle hitbox = new((int)(Projectile.Center.X - 16f), (int)(Projectile.Center.Y - 16f), 32, 32);
			return hitbox.Intersects(targetHitbox);
		}
		if (hasHit)
			return false;
		float point = 0f;
		return Collision.CheckAABBvLineCollision(
			targetHitbox.TopLeft(),
			targetHitbox.Size(),
			handPos,
			swordEnd,
			24f,
			ref point);
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
		if (comboStep != 2)
			hasHit = true;
	}

	private void Move() {
		// 待机姿态：手臂随移动小幅摆动，刀身随玩家朝向自然下垂
		float vx = Math.Abs(player.velocity.X);
		bool isAirborne = Math.Abs(player.velocity.Y) > 0.01f;
		float targetOffsetDeg;
		if (isAirborne) {
			targetOffsetDeg = -20f;
			walkPhase = 0f;
		}
		else if (vx > 0.1f) {
			walkPhase += 0.12f + vx * 0.04f;
			float progress = (MathF.Sin(walkPhase) + 1f) * 0.5f;
			targetOffsetDeg = MathHelper.Lerp(50f, -20f, progress);
		}
		else {
			walkPhase = 0f;
			targetOffsetDeg = 0f;
		}

		// 待机时刀身朝向玩家面前的斜下方（世界角，方向相关）
		float idleAngleAbs = MathHelper.ToRadians(20f - targetOffsetDeg * 0.3f);
		float worldRot = player.direction == 1 ? idleAngleAbs : MathHelper.Pi - idleAngleAbs;

		float armRot = ToArmRot(worldRot);
		player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, armRot);
		handPos = player.GetBackHandPosition(Player.CompositeArmStretchAmount.Full, armRot);
		Projectile.rotation = worldRot;
		Projectile.Center = handPos;

		if (Main.myPlayer == player.whoAmI) {
			if (!Main.mouseRight && player.controlUseItem) {
				mouseRad = MathF.Atan2((Main.MouseWorld - player.MountedCenter).Y, (Main.MouseWorld - player.MountedCenter).X);
				player.direction = (Main.MouseWorld - player.MountedCenter).X >= 0 ? 1 : -1;
				projMode = ProjMode.Attack;
				attackTime = 0;
				attackMaxTime = comboStep == 2 ? 40 : item.useAnimation;
				hasHit = false;
				throwReturning = false;
				throwSpin = 0f;
			}
		}
	}

	private void Attack() {
		player.itemTime = player.itemAnimation = Projectile.timeLeft = 2;
		float progress = Math.Clamp((float)attackTime / attackMaxTime, 0f, 1f);

		switch (comboStep) {
			case 0:
				AttackChop(progress);
				break;
			case 1:
				AttackThrust(progress);
				break;
			case 2:
				AttackThrow(progress);
				break;
		}

		attackTime++;
		if (attackTime > attackMaxTime) {
			projMode = ProjMode.Move;
			comboStep = (comboStep + 1) % 3;
		}
	}

	// 第一击：朝玩家面前方向，沿光标方向自上而下劈砍
	// 注意：不要用 RotationHelper.GetSwingRotation，它会把 playerDir 乘进起始角里，
	// 而 mouseRad 本身已经是绝对世界角(自带朝向信息)，两者叠加会导致左右朝向时挥砍轨迹不对称且都错误。
	private void AttackChop(float progress) {
		float startRot = mouseRad - MathHelper.ToRadians(70f);
		float endRot = mouseRad + MathHelper.ToRadians(70f);
		float easedT = RotationHelper.EaseOutCubic(progress);
		Projectile.rotation = MathHelper.Lerp(startRot, endRot, easedT);

		float armRot = ToArmRot(Projectile.rotation);
		player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, armRot);
		handPos = player.GetBackHandPosition(Player.CompositeArmStretchAmount.Full, armRot);
		swordEnd = handPos + Projectile.rotation.ToRotationVector2() * Reach;
		Projectile.Center = handPos;
	}

	// 第二击：朝光标位置方向直线戳刺，手部本身要随戳刺动作向前探出再收回
	private void AttackThrust(float progress) {
		Projectile.rotation = mouseRad;
		float armRot = ToArmRot(mouseRad);
		player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, armRot);
		Vector2 baseHandPos = player.GetBackHandPosition(Player.CompositeArmStretchAmount.Full, armRot);

		// 0 -> 1 -> 0 的三角形曲线：先伸出再收回
		float extend = progress < 0.5f ? progress / 0.5f : 1f - (progress - 0.5f) / 0.5f;
		const float LungeDistance = 28f; // 手部本身向前探出的距离
		handPos = baseHandPos + mouseRad.ToRotationVector2() * LungeDistance * extend;
		swordEnd = handPos + mouseRad.ToRotationVector2() * Reach * MathHelper.Lerp(0.5f, 1f, extend);
		Projectile.Center = handPos;
	}

	// 第三击：把刀像回旋镖一样旋转扔出去，飞到一定距离后再飞回手中
	private void AttackThrow(float progress) {
		float armRot = ToArmRot(mouseRad);
		player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, armRot);
		handPos = player.GetBackHandPosition(Player.CompositeArmStretchAmount.Full, armRot);

		bool outbound = progress < 0.5f;
		if (!outbound && !throwReturning)
			throwReturning = true;

		float travel = outbound ? progress / 0.5f : 1f - (progress - 0.5f) / 0.5f;
		Vector2 origin = player.MountedCenter;
		Projectile.Center = origin + mouseRad.ToRotationVector2() * ThrowDistance * travel;

		throwSpin += 0.5f;
		Projectile.rotation = throwSpin;
	}

	public override bool PreDraw(ref Color lightColor) {
		Texture2D tex = TextureAssets.Projectile[Type].Value;
		if (projMode == ProjMode.Attack && comboStep == 2) {
			// 飞行中的回旋投掷：以刀身中心为锚点自由旋转
			Vector2 origin = tex.Size() / 2f;
			Vector2 drawPos = Projectile.Center - Main.screenPosition;
			Main.spriteBatch.Draw(tex, drawPos, null, lightColor, Projectile.rotation, origin, DrawScale, SpriteEffects.None, 0f);
		}
		else {
			// 持握状态：以刀柄(贴图左侧中心)为锚点，按 DrawScale 适当放大视觉表现
			Vector2 origin = new(0f, tex.Height / 2f);
			Vector2 drawPos = handPos - Main.screenPosition;
			Main.spriteBatch.Draw(tex, drawPos, null, lightColor, Projectile.rotation, origin, DrawScale, SpriteEffects.None, 0f);
		}
		return false;
	}
}
