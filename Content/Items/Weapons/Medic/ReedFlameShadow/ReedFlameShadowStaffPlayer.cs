using ArknightsMod.Content.Buffs.Medic.ReedFlameShadow;
using ArknightsMod.Content.Projectiles.Medic.ReedFlameShadow;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Medic.ReedFlameShadow
{
	/// <summary>
	/// 焰影苇草 二/三技能的运行时状态（照 <c>ShiningStaffPlayer</c> / <c>WarfarinStaffPlayer</c>
	/// 的写法，武器专属 ModPlayer）：
	/// <br/>· 二技能「枯荣共息」：<see cref="S2OrbitPhase"/> —— 三颗环绕弹幕<b>共享</b>的旋转相位，
	/// 顺时针匀速递增；每 <see cref="TopUpInterval"/> 帧按存活弹幕总数补弹，补在空缺的 120° 槽位上。
	/// <br/>· 三技能「生命火种」：<see cref="S3RemainingTicks"/> 供弹幕携带，<see cref="UpdateSkill3Scar"/>
	/// 负责把灼痕续到技能结束、并在技能结束时立即清除（每秒流失与死亡爆炸见
	/// <see cref="ReedFlameShadowScarGlobalNPC"/>）。
	///
	/// <para>索敌不在这里：目标由<b>每颗弹幕自己</b>持有（<c>Projectile.ai[2]</c>），
	/// 见 <see cref="ReedFlameShadowOrb.Simulate"/>——共用一个全局目标会让"目标跑远之后
	/// 新补充的弹幕"继承陈旧目标，隔着大半张地图飞过去。</para>
	///
	/// <para>为什么全部判定都收敛在属主客户端（<c>whoAmI == Main.myPlayer</c>）：
	/// <see cref="WeaponPlayer.SkillActive"/> / <c>Skill</c> 是<b>客户端本地</b>状态，服务器与其它客户端
	/// 拿到的值不可靠，所以"生成 / 索敌 / 清除"只由本人判定；弹幕本体在其它端只跟随同步过来的
	/// 位置与速度，不自行 Kill（与 AstraSwordAura / ClosureShieldProjectile 同一套约定）。</para>
	/// </summary>
	public sealed class ReedFlameShadowStaffPlayer : ModPlayer
	{
		// ── 二技能可调参数（改这里就能调整整个技能的手感）──────────────────
		/// <summary>弹幕中心到玩家中心的距离（像素）。</summary>
		public const float OrbitRadius = 160f;
		/// <summary>顺时针角速度：120 帧（2 秒）转一圈。屏幕坐标系里角度递增即顺时针。</summary>
		public const float OrbitAngularSpeed = MathHelper.TwoPi / 120f;
		/// <summary>三颗彼此相隔 120°。</summary>
		public const float SlotSpacing = MathHelper.TwoPi / 3f;
		/// <summary>slot0 的起始角度：-90°，即玩家正上方。</summary>
		public const float Slot0Angle = -MathHelper.PiOver2;
		/// <summary>环绕弹幕的总槽位数。</summary>
		public const int OrbSlotCount = 3;
		/// <summary>每隔这么多帧检查一次环绕弹幕数量，不足则补一颗。</summary>
		public const int TopUpInterval = 20;
		/// <summary>索敌半径：只有待命中的弹幕会按这个范围找目标；已扑出去的会追到命中。</summary>
		public const float TargetSearchRange = 600f;
		/// <summary>弹幕击退（与普攻一致）。</summary>
		public const float OrbKnockback = 3.5f;

		/// <summary>
		/// 判定"三技能真的结束了"所需的连续失效帧数。给一两帧宽限是为了避免瞬时状态
		/// （切物品的那一帧、状态刷新）把全场灼痕误清掉——正常结束时也只晚约 1/30 秒。
		/// </summary>
		private const int S3EndGraceTicks = 2;

		/// <summary>当前共享旋转相位（弧度，已用 WrapAngle 收敛在 ±π）。</summary>
		public float S2OrbitPhase;
		/// <summary>补弹计时器。</summary>
		public int S2TopUpTimer;
		/// <summary>三技能生效期间置位：表示"还需要每帧扫场续期 / 结束后还要清扫一遍"。</summary>
		private bool s3ScarSweepArmed;
		/// <summary>三技能连续失效的帧数（配合 <see cref="S3EndGraceTicks"/> 确认结束）。</summary>
		private int s3InactiveTicks;

		public static int OrbType => ModContent.ProjectileType<ReedFlameShadowOrb>();

		/// <summary>某个槽位在当前相位下的角度。</summary>
		public static float SlotAngle(float phase, int slot) =>
			phase + Slot0Angle + slot * SlotSpacing;

		/// <summary>某个槽位在当前相位下的世界坐标。</summary>
		public static Vector2 SlotPosition(Player player, float phase, int slot) =>
			player.MountedCenter + SlotAngle(phase, slot).ToRotationVector2() * OrbitRadius;

		/// <summary>某个技能槽是否仍在生效：手持苇草法杖 + 该技能被选中 + 技能开启中。</summary>
		private bool SkillIsActive(int skill)
		{
			if (!Player.active || Player.dead)
				return false;
			if (Player.HeldItem?.ModItem is not ReedFlameShadowStaff)
				return false;

			WeaponPlayer weaponPlayer = Player.GetModPlayer<WeaponPlayer>();
			return weaponPlayer.SkillActive
				&& weaponPlayer.Skill == skill
				&& weaponPlayer.CurrentSkill?.Key.Item == nameof(ReedFlameShadowStaff);
		}

		/// <summary>二技能是否仍在生效。</summary>
		public bool S2IsActive() => SkillIsActive(1);

		/// <summary>三技能是否仍在生效。</summary>
		public bool S3IsActive() => SkillIsActive(2);

		/// <summary>
		/// 三技能剩余帧数（≤0 表示技能没在跑）。与 <c>WeaponPlayer.UpdateActiveSkill</c>
		/// 判断"技能是否该结束"用的是同一套阈值（含二倍速消耗配置的倍率），
		/// 所以算出来的剩余时间与技能真正结束的时刻严格对齐。
		/// </summary>
		public int S3RemainingTicks()
		{
			if (!S3IsActive())
				return 0;

			WeaponPlayer weaponPlayer = Player.GetModPlayer<WeaponPlayer>();
			float total = weaponPlayer.CurrentSkill.CurrentLevelData.ActiveTime * 60f * WeaponPlayer.ActiveDurationMultiplier;
			return Math.Max(0, (int)total - weaponPlayer.SkillTimer);
		}

		/// <summary>
		/// 二技能开启瞬间调用：相位归位，并在 160px 轨道上一次性召唤三颗
		/// （slot0 正上方，彼此 120°）。
		/// </summary>
		public void BeginS2()
		{
			if (Player.whoAmI != Main.myPlayer || !Player.active || Player.dead)
				return;

			S2OrbitPhase = 0f;
			S2TopUpTimer = 0;

			for (int slot = 0; slot < OrbSlotCount; slot++)
				SpawnOrb(slot);
		}

		public override void PostUpdate()
		{
			// 服务器与其它客户端的 WeaponPlayer 技能状态不可靠，两个技能的推进都只在本人端进行。
			if (Player.whoAmI != Main.myPlayer)
				return;

			// 三技能灼痕的寿命管理（续期 / 结束时清除）必须先跑：它不受二技能状态影响
			UpdateSkill3Scar();

			if (!S2IsActive()) {
				// 技能结束（20 秒到期 / 切走武器 / 切技能槽 / 死亡）：弹幕自身会在同一帧发现并销毁
				S2TopUpTimer = 0;
				return;
			}

			S2OrbitPhase = MathHelper.WrapAngle(S2OrbitPhase + OrbitAngularSpeed);

			if (++S2TopUpTimer >= TopUpInterval) {
				S2TopUpTimer = 0;
				TryTopUpOrbs();
			}
		}

		public override void UpdateDead()
		{
			S2OrbitPhase = 0f;
			S2TopUpTimer = 0;
			// 死亡同样算"技能结束"：三技能灼痕要跟着清掉
			UpdateSkill3Scar();
		}

		/// <summary>
		/// 三技能灼痕的寿命：技能生效期间，把本玩家施加（或接管）的灼痕一直续到"技能结束"；
		/// 技能一旦确认结束（30 秒到期 / 切走武器 / 切技能槽 / 死亡）就把它们连同标记一起清掉——
		/// 每秒流失与死亡爆炸都挂在灼痕上，灼痕没了它们自然停。
		///
		/// <para><b>为什么续期用 AddBuff 而不是直接写 buffTime</b>：某些敌人会自己清状态
		/// （假人重置、Boss 阶段转换），也可能因为 5 个状态栏被别的 debuff 挤爆而丢掉灼痕；
		/// 只改剩余时间救不回来，直接重新 AddBuff 才能把这些情况一并兜住（已有灼痕时
		/// AddBuff 只取较大值，不会缩短）。</para>
		/// </summary>
		private void UpdateSkill3Scar()
		{
			if (Player.whoAmI != Main.myPlayer)
				return;

			bool active = S3IsActive();

			if (active) {
				s3InactiveTicks = 0;
				s3ScarSweepArmed = true; // 技能生效 → 需要每帧扫场续期
			}
			else if (!s3ScarSweepArmed) {
				return; // 技能没开过、或结束后的清理已经做完：什么都不用干
			}
			else if (++s3InactiveTicks < S3EndGraceTicks) {
				return; // 宽限帧：先别急着清，可能只是瞬时状态（见下）
			}
			else {
				s3ScarSweepArmed = false; // 确认结束，下面这一遍清完就收工
			}

			int remaining = active ? Math.Max(1, S3RemainingTicks()) : 0;
			int scarType = ModContent.BuffType<BurnedScar>();

			foreach (NPC npc in Main.ActiveNPCs) {
				var scar = npc.GetGlobalNPC<ReedFlameShadowScarGlobalNPC>();
				if (!scar.ScarFromSkill3 || scar.ScarOwner != Player.whoAmI)
					continue;

				if (active) {
					// 自愈式续期：灼痕在就取较大值续期，被清掉了就重新挂回去
					npc.AddBuff(scarType, remaining);
				}
				else {
					int buffIndex = npc.FindBuffIndex(scarType);
					if (buffIndex >= 0)
						npc.DelBuff(buffIndex); // 技能结束 → 灼痕消失
					scar.ClearMark();
				}
			}
		}

		/// <summary>
		/// 每 <see cref="TopUpInterval"/> 帧检查一次：统计口径是<b>该技能全部存活弹幕</b>
		/// （含正在追踪/归位飞行的），总数不足 <see cref="OrbSlotCount"/> 时补一颗。
		/// </summary>
		private void TryTopUpOrbs()
		{
			int type = OrbType;
			if (Player.ownedProjectileCounts[type] >= OrbSlotCount)
				return;

			// 补在"当前空缺的槽位"上，三颗之间才始终是 120°。
			bool[] used = new bool[OrbSlotCount];
			for (int i = 0; i < Main.maxProjectiles; i++) {
				Projectile projectile = Main.projectile[i];
				if (!projectile.active || projectile.type != type || projectile.owner != Player.whoAmI)
					continue;

				int slot = (int)projectile.ai[1];
				if (slot >= 0 && slot < OrbSlotCount)
					used[slot] = true;
			}

			for (int slot = 0; slot < OrbSlotCount; slot++) {
				if (!used[slot]) {
					SpawnOrb(slot);
					return;
				}
			}
		}

		/// <summary>
		/// 直接在 160px 轨道上生成一颗（按需求不做入场动画），速度取顺时针切向速度
		/// （供拖尾朝向与其它端的插值使用；环绕状态下每帧会被弹幕自己修正回轨道）。
		/// <br/>ai0 = 环绕状态、ai1 = 槽位、ai2 = 目标（-1 = 无，必须显式写 -1：
		/// ai[2] 默认是 0，会被当成"锁定 0 号 NPC"）。
		/// </summary>
		private void SpawnOrb(int slot)
		{
			if (slot < 0 || slot >= OrbSlotCount)
				return;

			int damage = Math.Max(1, Player.GetWeaponDamage(Player.HeldItem));
			Vector2 position = SlotPosition(Player, S2OrbitPhase, slot);
			Vector2 tangent = (SlotAngle(S2OrbitPhase, slot) + MathHelper.PiOver2).ToRotationVector2();
			Vector2 velocity = tangent * (OrbitRadius * OrbitAngularSpeed);

			Projectile.NewProjectile(Player.GetSource_Misc("ReedFlameShadowS2"), position, velocity,
				OrbType, damage, OrbKnockback, Player.whoAmI,
				ReedFlameShadowOrb.StateOrbiting, slot, -1f);
		}
	}
}
