using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Common.GlobalNPCs
{
	// 血蕈的被动效果不生成全新的怪物种类，而是随机挑一只"已有"的怪物类型现身，
	// 由这个 GlobalNPC 给挑中的那一只叠加红雾特效（染色+粒子）和三维强化（生命/伤害/移速统一乘倍率）。
	public class RedMistGlobalNPC : GlobalNPC
	{
		public override bool InstancePerEntity => true;

		// 候选怪物池：挑中谁就给谁套红雾效果。想扩充就往这里加（含你们自己模组的怪物类型）。
		public static readonly int[] CandidateTypes = [
			NPCID.Zombie,
			NPCID.DemonEye,
			NPCID.GreenSlime,
			NPCID.Pinky,
		];

		public bool IsRedMist;
		public float StatMultiplier = 1f;

		// comboLevel: 0 => x2 倍率，之后每 +1 级倍率再 +1。
		public static NPC SpawnNear(Player player, int comboLevel) {
			if (Main.netMode == NetmodeID.MultiplayerClient) return null;

			int type = CandidateTypes[Main.rand.Next(CandidateTypes.Length)];
			float multiplier = 2f + comboLevel;

			Vector2 offset = Main.rand.NextVector2CircularEdge(200f, 200f);
			Vector2 spawnPos = player.Center + offset;

			int index = NPC.NewNPC(Entity.GetSource_NaturalSpawn(), (int)spawnPos.X, (int)spawnPos.Y, type);
			if (index >= Main.maxNPCs) return null;

			NPC npc = Main.npc[index];
			var effect = npc.GetGlobalNPC<RedMistGlobalNPC>();
			effect.IsRedMist = true;
			effect.StatMultiplier = multiplier;

			npc.lifeMax = (int)(npc.lifeMax * multiplier);
			npc.life = npc.lifeMax;
			npc.damage = (int)(npc.damage * multiplier);
			npc.netUpdate = true;

			return npc;
		}

		public static bool AnyActive() {
			for (int i = 0; i < Main.maxNPCs; i++) {
				NPC npc = Main.npc[i];
				if (npc.active && npc.GetGlobalNPC<RedMistGlobalNPC>().IsRedMist)
					return true;
			}
			return false;
		}

		public override void PostAI(NPC npc) {
			if (!IsRedMist) return;

			// 不直接改 npc.velocity——那样会被喂回下一帧的 AI 计算，
			// 如果宿主怪物的 AI 没有每帧夹限速度（比如恶魔之眼的飞行AI），会像滚雪球一样越乘越快，
			// 最后飞出屏幕。改成只叠加一段额外位移，怪物自己的速度状态完全不受影响，不会累积。
			npc.position += npc.velocity * (StatMultiplier - 1f);

			if (Main.rand.NextBool(4))
				Dust.NewDust(npc.position, npc.width, npc.height, DustID.Blood, 0f, 0f, 150, default, 1.1f);
		}

		public override void DrawEffects(NPC npc, ref Color drawColor) {
			if (!IsRedMist) return;
			drawColor = Color.Lerp(drawColor, Color.Red, 0.55f);
		}

		public override void OnKill(NPC npc) {
			if (!IsRedMist) return;

			int killer = npc.lastInteraction;
			if (killer < 0 || killer >= Main.maxPlayers) return;

			Player player = Main.player[killer];
			if (!player.active) return;

			player.GetModPlayer<BloodMushroomPlayer>().ComboLevel++;
		}
	}
}
