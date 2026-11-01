using ArknightsMod.Players;
using ArknightsMod.Content.Items.Weapons.Guard.Surtr;
using ArknightsMod.Content.Projectiles.Guard.Laevatain;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ArknightsMod.Content.Buffs
{
	public class SurtrLaevatainS3Player : ModPlayer
	{
		public const int MaxLifeBonus = 1200;

		// 持续掉血用的累加器
		public float S3DrainAccumulator;

		// 一次黄昏持续至死亡；切换装备、技能或重新进世界都不能重新回血、重置掉血。
		public bool S3HealDone;
		public bool S3HealPending;
		public int S3LastFinalMaxLife;
		public int S3ElapsedTicks;
		internal bool S3Committed;

		public override void SaveData(TagCompound tag)
		{
			if (Player.dead || !(S3Committed || Player.HasBuff<SurtrLaevatainS3Buff>()))
				return;
			tag["TwilightActive"] = true;
			tag["TwilightHealed"] = S3HealDone;
			tag["TwilightTicks"] = S3ElapsedTicks;
			tag["TwilightDrain"] = S3DrainAccumulator;
		}

		public override void LoadData(TagCompound tag)
		{
			S3Committed = tag.GetBool("TwilightActive");
			S3HealDone = tag.GetBool("TwilightHealed");
			S3ElapsedTicks = System.Math.Clamp(tag.GetInt("TwilightTicks"), 0, SurtrLaevatainS3Buff.RampTicks);
			float drain = tag.GetFloat("TwilightDrain");
			S3DrainAccumulator = float.IsFinite(drain) ? MathHelper.Clamp(drain, 0f, 1f) : 0f;
		}

		public override void UpdateDead()
		{
			// 死亡时移除 S3 buff，防止残留到复活后
			int s3BuffType = ModContent.BuffType<SurtrLaevatainS3Buff>();
			for (int i = 0; i < Player.MaxBuffs; i++)
			{
				if (Player.buffType[i] == s3BuffType)
				{
					Player.DelBuff(i);
					break;
				}
			}
			OnS3BuffRemoved();
		}

		public override void PostUpdate()
		{
			if (Player.dead)
				return;
			bool nowActive = Player.HasBuff<SurtrLaevatainS3Buff>();
			// Buff 只是显示/同步载体，不能通过移除图标或重进世界解除已开启的黄昏。
			if (S3Committed && !nowActive)
			{
				Player.AddBuff(ModContent.BuffType<SurtrLaevatainS3Buff>(), SurtrLaevatainS3Buff.InitialDuration);
				nowActive = true;
			}
			if (S3HealPending && !S3HealDone && nowActive)
			{
				S3HealPending = false;
				S3HealDone = true;
				Player.GetModPlayer<IncomingHealingPlayer>().RestoreLife(Player.statLifeMax2);
			}

			// 记录本帧最终生命上限，供 S3 掉血用（掉血在 UpdateBuffs 里算时，
			// 其他加成的上限还没叠上来）。PostUpdate 是帧内最早能拿到完整值的时机。
			S3LastFinalMaxLife = Player.statLifeMax2;
		}

		internal void OnS3BuffRemoved()
		{
			// 仅死亡结束本次黄昏，允许复活后重新施放。
			S3Committed = false;
			S3HealDone = false;
			S3HealPending = false;
			S3DrainAccumulator = 0f;
			S3ElapsedTicks = 0;
			S3LastFinalMaxLife = 0;
			Player.GetModPlayer<SurtrLaevatain_Player>().StopTransformationFire();

			// 同步清掉 WeaponPlayer 里的技能激活状态。不然死亡复活后 buff 已经没了，
			// 技能条却还亮着、攻击还保持 S3 强化形态。
			// 只清 S3 的状态：如果玩家已经切了武器或切了技能位，那是别的技能的状态，不能动。
			var wp = Player.GetModPlayer<WeaponPlayer>();
			if (wp.HoldSurtrLaevatain && wp.Skill == 2)
			{
				wp.SkillActive = false;
				wp.SkillTimer = 0;
			}
		}
	}

	/// <summary>
	/// S3「黄昏」的持续效果：生命上限+1200 并回满血（都在下面 Update 里做，原因见 Update 内注释），
	/// 并按持续时间线性爬坡扣血，90 秒爬满后稳定在每秒扣除最大生命 10%。
	/// 开启后持续至死亡，切换武器或技能不会结束，也不能主动取消。
	/// </summary>
	public class SurtrLaevatainS3Buff : ModBuff
	{
		public override string Texture => "ArknightsMod/Content/Buffs/SurtrLaevatain_3_buff";

		public const int InitialDuration = int.MaxValue;
		internal const int RampTicks = 90 * 60; // 90 秒内扣血速率线性爬满
		private const float MaxDrainPerSecond = 0.10f; // 爬满后每秒扣除的最大生命值比例

		public override void SetStaticDefaults()
		{
			Main.buffNoTimeDisplay[Type] = true;
			Main.debuff[Type] = false; // 主动技能带来的效果，不是敌对负面状态
			BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
		}

		public override bool RightClick(int buffIndex) => false;

		public override void Update(Player player, ref int buffIndex)
		{
			if (player.dead)
			{
				player.DelBuff(buffIndex);
				buffIndex--;
				player.GetModPlayer<SurtrLaevatainS3Player>().OnS3BuffRemoved();
				return;
			}

			var mp = player.GetModPlayer<SurtrLaevatainS3Player>();
			mp.S3Committed = true;
			if (player.whoAmI == Main.myPlayer)
				player.GetModPlayer<WeaponPlayer>().ResumeTwilight();
			player.statLifeMax2 += SurtrLaevatainS3Player.MaxLifeBonus;
			if (!mp.S3HealDone)
				mp.S3HealPending = true;

			float rampProgress = mp.S3ElapsedTicks / (float)RampTicks;
			mp.S3ElapsedTicks = System.Math.Min(mp.S3ElapsedTicks + 1, RampTicks);
			int maxLifeForDrain = mp.S3LastFinalMaxLife > 0 ? mp.S3LastFinalMaxLife : player.statLifeMax2;
			float drainPerTick = rampProgress * MaxDrainPerSecond * maxLifeForDrain / 60f;

			mp.S3DrainAccumulator += drainPerTick;
			if (mp.S3DrainAccumulator < 1f)
				return;

			int damage = (int)mp.S3DrainAccumulator;
			mp.S3DrainAccumulator -= damage;

			if (player.statLife - damage <= 0)
			{
				player.statLife = 0;
				player.KillMe(PlayerDeathReason.ByCustomReason(NetworkText.FromLiteral(player.name + " 被黄昏的力量吞噬了")), damage, 0);
			}
			else
			{
				player.statLife -= damage;
			}
		}
	}
}
