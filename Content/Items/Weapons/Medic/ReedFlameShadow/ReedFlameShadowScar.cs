using ArknightsMod.Content.Buffs.Medic.ReedFlameShadow;
using ArknightsMod.Content.Projectiles.Medic.ReedFlameShadow;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ArknightsMod.Content.Items.Weapons.Medic.ReedFlameShadow
{
	/// <summary>
	/// 「生命火种」（三技能）给灼痕附加的运行时数据 + 三技能独有的两件事：
	/// 每秒流失、死亡爆炸。
	///
	/// <para>为什么需要这份数据：<see cref="BurnedScar"/> 是一个普通 debuff，本身分不出来源，
	/// 而三技能要区分"这份灼痕是不是本技能施加的"——只有本技能施加（或接管）的灼痕才
	/// 每秒烧血、死亡爆炸，并且寿命被续到技能结束。</para>
	///
	/// <para>每秒流失用原版 debuff 掉血机制（<c>npc.lifeRegen</c>，与现有"流血"同一个写法）：
	/// 换算关系是 lifeRegen 每 2 点 = 每秒 1 点生命，而且这条掉血不走防御，天然就是
	/// "无视防御的真实伤害"。爆炸则用只写 <c>HitInfo.Damage</c> 的 <c>StrikeNPC</c>：
	/// 减防发生在 <c>CalculateHitInfo</c> 里，跳过它就等于无视防御（维什戴尔余震同款写法）。</para>
	/// </summary>
	public sealed class ReedFlameShadowScarGlobalNPC : GlobalNPC
	{
		/// <summary>死亡爆炸半径（像素）。</summary>
		public const float BlastRadius = 320f;
		/// <summary>每秒流失 = 攻击力 × 这个比例。</summary>
		public const float DrainRatioPerSecond = 0.6f;
		/// <summary>爆炸伤害 = 攻击力 × 这个比例。</summary>
		public const float BlastDamageRatio = 1.4f;

		public override bool InstancePerEntity => true;

		/// <summary>施加者（player whoAmI）；-1 = 这份灼痕与三技能无关。</summary>
		public int ScarOwner = -1;
		/// <summary>这份灼痕是否由三技能施加/接管（决定是否享受每秒流失与死亡爆炸）。</summary>
		public bool ScarFromSkill3;
		/// <summary>施加时的攻击力快照。</summary>
		public int ScarPower;

		/// <summary>
		/// 施加/接管一份"技能三灼痕"。
		/// <br/>已灼痕的敌人<b>不会</b>被叠成第二份，只会被接管：改记为本玩家施加，并把剩余时间续到本次技能结束。
		/// </summary>
		/// <param name="owner">施加者（持杖玩家）。</param>
		/// <param name="npc">目标敌怪。</param>
		/// <param name="ticks">灼痕持续帧数（= 本技能剩余时间）。</param>
		/// <param name="power">施加时的攻击力快照，供服务器在读不到技能状态时使用。</param>
		public static bool ApplyFromSkill3(Player owner, NPC npc, int ticks, int power)
		{
			if (owner == null || !owner.active || owner.dead)
				return false;
			if (npc == null || !npc.active || npc.friendly || npc.townNPC || npc.dontTakeDamage)
				return false;

			int buffType = ModContent.BuffType<BurnedScar>();
			int buffIndex = npc.FindBuffIndex(buffType);
			int duration = Math.Max(1, ticks);

			if (buffIndex < 0)
				npc.AddBuff(buffType, duration);
			else
				npc.buffTime[buffIndex] = duration; // 接管：不新增一份，直接把剩余时间续到技能结束

			var scar = npc.GetGlobalNPC<ReedFlameShadowScarGlobalNPC>();
			scar.ScarOwner = owner.whoAmI;
			scar.ScarFromSkill3 = true;
			scar.ScarPower = Math.Max(1, power);

			npc.netUpdate = true; // 让附加数据（SendExtraAI）一起同步出去
			return true;
		}

		/// <summary>清掉本怪身上的"技能三灼痕"标记（不动 debuff 本身）。</summary>
		public void ClearMark()
		{
			ScarOwner = -1;
			ScarFromSkill3 = false;
			ScarPower = 0;
		}

		/// <summary>这只怪身上还有多少帧灼痕。</summary>
		public static int RemainingScarTicks(NPC npc)
		{
			int buffIndex = npc.FindBuffIndex(ModContent.BuffType<BurnedScar>());
			return buffIndex >= 0 ? npc.buffTime[buffIndex] : 0;
		}

		/// <summary>
		/// 攻击力口径：单机/主机上施加者就在本端，直接读实时面板（含三技能的 +60% 与各种增益）；
		/// 服务器在多人下读不到客户端的技能状态，退回发射时的快照。
		/// </summary>
		private int CurrentPower()
		{
			if (!ScarFromSkill3 || ScarOwner < 0 || ScarOwner >= Main.maxPlayers)
				return Math.Max(1, ScarPower);

			Player owner = Main.player[ScarOwner];
			if (!owner.active || owner.dead)
				return Math.Max(1, ScarPower);

			if (ScarOwner == Main.myPlayer && owner.HeldItem?.ModItem is ReedFlameShadowStaff)
				return Math.Max(1, owner.GetWeaponDamage(owner.HeldItem));

			return Math.Max(1, ScarPower);
		}

		// ── 每秒流失 ────────────────────────────────────────
		public override void UpdateLifeRegen(NPC npc, ref int damage)
		{
			if (!ScarFromSkill3)
				return;

			if (ScarOwner < 0)
				return;

			if (!npc.HasBuff(ModContent.BuffType<BurnedScar>())) {
				// 灼痕这一帧不在（敌人自己清了状态、或 5 个状态栏被别的 debuff 挤爆）：
				// 本帧不烧血，但**不清标记**——施加者每帧都在续期，下一帧就会被挂回来。
				// 这里若 ClearMark，就会把"被清掉的状态"变成永久丢失，
				// 那正是部分敌人身上灼痕提前结束的原因。
				return;
			}

			int perSecond = Math.Max(1, (int)MathF.Round(CurrentPower() * DrainRatioPerSecond));
			npc.lifeRegen = Math.Min(0, npc.lifeRegen) - perSecond * 2; // ×2：lifeRegen 2 点 = 每秒 1 点生命
			damage = Math.Max(damage, 2);
		}

		// ── 死亡爆炸 ────────────────────────────────────────
		public override void OnKill(NPC npc)
		{
			if (!ScarFromSkill3 || ScarOwner < 0)
				return;

			int owner = ScarOwner;
			int power = CurrentPower();
			// 爆炸会给范围内的敌人挂上同样的灼痕，它们死亡时再炸 → 连锁；
			// 这里沿用"本次灼痕剩余多少帧"，就等于"本次技能还剩多少帧"。
			int scarTicks = RemainingScarTicks(npc);
			Vector2 center = npc.Center;

			ClearMark();

			// 伤害与新弹幕都交给服务器/单机决定，客户端只接收同步结果（避免多端各炸一次）
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			ApplyBlast(center, owner, power, scarTicks);
		}

		private static void ApplyBlast(Vector2 center, int ownerWhoAmI, int power, int scarTicks)
		{
			Player owner = ownerWhoAmI >= 0 && ownerWhoAmI < Main.maxPlayers ? Main.player[ownerWhoAmI] : null;

			int damage = Math.Max(1, (int)MathF.Round(power * BlastDamageRatio));
			float radiusSquared = BlastRadius * BlastRadius;

			foreach (NPC npc in Main.ActiveNPCs) {
				if (!npc.CanBeChasedBy(owner) || npc.DistanceSQ(center) > radiusSquared)
					continue;

				// 先挂灼痕再结算伤害：被这一炸直接打死的敌人，死亡时身上已经带着灼痕，
				// 于是它自己的 OnKill 会再炸一次——这就是"连锁"能成立的前提。
				if (owner != null)
					ApplyFromSkill3(owner, npc, scarTicks, power);

				// 只写 Damage、不经过 CalculateHitInfo：StrikeNPC 会把 hit.Damage 当作最终伤害直接用，
				// 所以这就是"无视防御"的爆炸伤害。
				NPC.HitInfo hit = new() {
					Damage = damage,
					Knockback = 0f,
					HitDirection = npc.Center.X >= center.X ? 1 : -1,
					Crit = false,
					DamageType = DamageClass.Magic,
				};
				npc.StrikeNPC(hit);
				if (Main.netMode != NetmodeID.SinglePlayer)
					NetMessage.SendStrikeNPC(npc, hit);
			}

			ReedFlameShadowScarBlast.Spawn(center, ownerWhoAmI);
		}

		// ── 灼痕期间的火星 ──────────────────────────────────
		public override void DrawEffects(NPC npc, ref Color drawColor)
		{
			if (Main.dedServ || !ScarFromSkill3)
				return;

			if (drawColor.R < 255)
				drawColor.R = (byte)Math.Min(255, drawColor.R + 30);

			if (Main.rand.NextBool(6)) {
				Dust dust = Dust.NewDustDirect(npc.position, npc.width, npc.height,
					Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.Torch,
					0f, -0.6f, 100, default, Main.rand.NextFloat(0.8f, 1.4f));
				dust.noGravity = true;
				dust.velocity += npc.velocity * 0.2f;
			}
		}

		public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
		{
			bitWriter.WriteBit(ScarFromSkill3);
			if (!ScarFromSkill3)
				return;

			binaryWriter.Write((short)ScarOwner);
			binaryWriter.Write(ScarPower);
		}

		public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
		{
			ScarFromSkill3 = bitReader.ReadBit();
			if (!ScarFromSkill3) {
				ScarOwner = -1;
				ScarPower = 0;
				return;
			}

			ScarOwner = binaryReader.ReadInt16();
			ScarPower = binaryReader.ReadInt32();
		}
	}
}
