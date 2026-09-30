using System;
using System.Collections.Generic;
using ArknightsMod.Content.NPCs.Enemy.Chapter6;
using ArknightsMod.Content.NPCs.Enemy.ThroughChapter4;
using ArknightsMod.Content.NPCs.Enemy.TillChapter7;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Friendly
{
	/// <summary>
	/// 坎诺特战斗阶段里，某个"游戏阶段"对应的整套波次阵容。
	/// </summary>
	public sealed class CannotWaveStage
	{
		/// <summary>仅用于日志/调试。</summary>
		public string Name;

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

		private static List<CannotWaveStage> BuildStages() {
			return [
				// ── 阶段 0：默认阵容（目前唯一一套，沿用之前"坎诺特精英怪"的五种怪）──
				// 五种怪的强度：奥内罗斯/高阶术师/盾卫 ≈ 350 血的小怪，冰裂者 1600 血，
				// 狂躁僵尸 2250 血，所以越往后越重，最后一波压轴。
				new CannotWaveStage {
					Name = "默认",
					Condition = () => true,
					Waves = [
						() => [ModContent.NPCType<Seniorcaster>()],
						() => [ModContent.NPCType<Oneiros>(), ModContent.NPCType<ShieldGuard>()],
						() => [ModContent.NPCType<Seniorcaster>(), ModContent.NPCType<ShieldGuard>(), ModContent.NPCType<Oneiros>()],
						() => [ModContent.NPCType<IceCleaver>(), ModContent.NPCType<Seniorcaster>()],
						() => [ModContent.NPCType<InsaneZombieL>(), ModContent.NPCType<ShieldGuard>()],
					],
				},
			];
		}
	}

	/// <summary>
	/// 标记"这只怪是坎诺特某场战斗里召唤出来的"，以及是被哪只坎诺特召唤的。
	/// 只在服务器/单人侧使用（判断一波有没有打完），不需要同步给客户端。
	/// </summary>
	public class CannotSummonedTag : GlobalNPC
	{
		public override bool InstancePerEntity => true;

		public int OwnerCannot = -1;

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
