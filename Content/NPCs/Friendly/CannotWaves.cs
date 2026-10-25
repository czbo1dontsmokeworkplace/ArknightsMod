using System;
using System.Collections.Generic;
using System.IO;
using ArknightsMod.Systems;
using ArknightsMod.Content.NPCs.Enemy.Chapter6;
using ArknightsMod.Content.NPCs.Enemy.ThroughChapter4;
using ArknightsMod.Content.NPCs.Enemy.TillChapter7;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Raptor = ArknightsMod.Content.NPCs.Enemy.ThroughChapter4.RaptorDrone.RaptorDrone;

namespace ArknightsMod.Content.NPCs.Friendly
{
	/// <summary>
	/// 坎诺特战斗阶段里，某个"游戏阶段"对应的整套波次阵容。
	/// </summary>
	public sealed class CannotWaveStage
	{
		/// <summary>仅用于日志/调试。</summary>
		public string Name;
		public int Tier;

		/// <summary>这个阶段是否已经解锁。多个阶段同时满足时取列表里靠后的那个。</summary>
		public Func<bool> Condition;

		/// <summary>
		/// 每一波要刷出的怪物类型。用 <see cref="Func{T}"/> 延迟求值，是因为 NPCType&lt;T&gt;()
		/// 只有内容加载完成后才拿得到，不能在静态字段初始化时直接调用。
		/// 数组长度必须等于 <see cref="CannotWaves.WaveCount"/>。
		/// </summary>
		public Func<int[]>[] Waves;

		public int[] GetWave(int oneBasedWaveIndex) {
			int i = Math.Clamp(oneBasedWaveIndex - 1, 0, Waves.Length - 1);
			return Waves[i]();
		}
	}

	/// <summary>
	/// 坎诺特「请坎诺特降价」战斗的波次阵容表。
	///
	/// ── 以后怎么按游戏阶段更新阵容 ──
	/// 在 <see cref="BuildStages"/> 里往列表末尾追加一个 <see cref="CannotWaveStage"/>：
	/// Condition 写解锁条件（例如 () => NPC.downedMechBossAny），Waves 写这个阶段的五波阵容。
	/// 战斗开始那一刻会从后往前找第一个 Condition 满足的阶段，整场战斗锁定用它
	/// （打到一半击败了新 Boss 也不会中途换阵容）。第一个阶段的 Condition 必须恒为 true，
	/// 保证任何时候都至少有一套阵容可用。
	/// </summary>
	public static class CannotWaves
	{
		public const int WaveCount = 5;

		private static List<CannotWaveStage> _stages;

		public static CannotWaveStage GetCurrentStage() {
			_stages ??= BuildStages();
			for (int i = _stages.Count - 1; i >= 0; i--) {
				if (_stages[i].Condition())
					return _stages[i];
			}
			return _stages[0];
		}

		internal static void Unload() => _stages = null;

		private static List<CannotWaveStage> BuildStages() => [
            new() {
                Name = "一阶", Tier = 1, Condition = () => true,
                // TODO: 暴徒、鸡尾酒投掷者没有可用 NPC 帧图，暂不加入。
                Waves = [
                    () => [ModContent.NPCType<RockCrab>()],
                    () => [ModContent.NPCType<Drone>()],
                    () => [ModContent.NPCType<RockCrab>(), ModContent.NPCType<Drone>()],
                    () => [ModContent.NPCType<RockCrab>(), ModContent.NPCType<RockCrab>(), ModContent.NPCType<Drone>()],
                    () => [ModContent.NPCType<RockCrab>(), ModContent.NPCType<Drone>(), ModContent.NPCType<Drone>()]
                ]
            },
            new() {
                Name = "二阶", Tier = 2, Condition = () => CannotProgressionSystem.CurrentTier >= 2,
                Waves = [
                    () => [ModContent.NPCType<LightShield>(), ModContent.NPCType<Crossbowman>()],
                    () => [ModContent.NPCType<LightShield>(), ModContent.NPCType<Caster>()],
                    () => [ModContent.NPCType<Crossbowman>(), ModContent.NPCType<ArtsMasterA1>()],
                    () => [ModContent.NPCType<ShieldGuard>(), ModContent.NPCType<Caster>()],
                    () => [ModContent.NPCType<ShieldGuard>(), ModContent.NPCType<Crossbowman>(), ModContent.NPCType<ArtsMasterA1>()]
                ]
            },
            new() {
                Name = "三阶", Tier = 3, Condition = () => CannotProgressionSystem.CurrentTier >= 3,
                Waves = [
                    () => [ModContent.NPCType<CannotOriginiutant>(), ModContent.NPCType<MortarGunner>()],
                    () => [ModContent.NPCType<CannotOriginiutant>(), ModContent.NPCType<Seniorcaster>()],
                    () => [ModContent.NPCType<MortarGunner>(), ModContent.NPCType<ArtsMasterA2>()],
                    () => [ModContent.NPCType<IceCleaver>(), ModContent.NPCType<Seniorcaster>()],
                    () => [ModContent.NPCType<IceCleaver>(), ModContent.NPCType<CannotOriginiutant>(), ModContent.NPCType<ArtsMasterA2>()]
                ]
            },
            new() {
                Name = "四阶", Tier = 4, Condition = () => CannotProgressionSystem.CurrentTier >= 4,
                // TODO: 红标渗透者、红标根除者、泥岩巨像缺少可用 NPC 帧图。
                Waves = [
                    () => [ModContent.NPCType<Raptor>()],
                    () => [ModContent.NPCType<InsaneZombieL>()],
                    () => [ModContent.NPCType<InsaneZombieL>(), ModContent.NPCType<Raptor>()],
                    () => [ModContent.NPCType<InsaneZombieL>(), ModContent.NPCType<InsaneZombieL>()],
                    () => [ModContent.NPCType<InsaneZombieL>(), ModContent.NPCType<Raptor>(), ModContent.NPCType<Raptor>()]
                ]
            }
            // TODO: 五阶枯朽萃聚使徒缺少帧图；未完成前沿用四阶，绝不生成空波次。
        ];
	}

	/// <summary>
	/// 标记"这只怪是坎诺特某场战斗里召唤出来的"，以及是被哪只坎诺特召唤的。
	/// 同步归属，让客户端也能识别本场支援效果以及被支援敌人发出的弹幕。
	/// </summary>
	public class CannotSummonedTag : GlobalNPC
	{
		public override bool InstancePerEntity => true;

		public int OwnerCannot = -1;

        public override void SendExtraAI(NPC npc, BitWriter bits, BinaryWriter writer) => writer.Write((short)OwnerCannot);
        public override void ReceiveExtraAI(NPC npc, BitReader bits, BinaryReader reader) => OwnerCannot = reader.ReadInt16();

		public static int CountAlive(int cannotWhoAmI) {
			int count = 0;
			foreach (NPC npc in Main.ActiveNPCs) {
				if (npc.GetGlobalNPC<CannotSummonedTag>().OwnerCannot == cannotWhoAmI)
					count++;
			}
			return count;
		}
	}

	public class CannotWavesSystem : ModSystem
	{
		public override void Unload() => CannotWaves.Unload();
	}
}
