using ArknightsMod.Common.GlobalNPCs;
using ArknightsMod.Systems;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Players
{
	public class BloodMushroomPlayer : ModPlayer
	{
		public const int BaseValue = 2;            // 血蕈基础估价（铜币）
		public const int ValueStepPerKill = 2;      // 每击杀一只红雾，估价 +2

		private const int SpawnCheckInterval = 300; // 约5秒判定一次是否刷新红雾
		private const float SpawnRollChance = 0.10f;

		private const int GrowthCheckInterval = 60;  // 约1秒判定一次饱食蔓延
		private const float GrowthRollChance = 0.003f;
		private const int GrowthRadius = 25;

		public bool HasBloodMushroom;
		public int ComboLevel; // 0 => 下一只红雾 x2 倍率，之后每次击杀 +1 级

		private int spawnTimer;
		private int growthTimer;

		public override void ResetEffects() {
			HasBloodMushroom = false;
		}

		public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource) {
			ComboLevel = 0;
		}

		public override void PostUpdate() {
			// 生成怪物/放置瓦片是世界权威操作，联机客户端不能自己决定，只在单人或服务器上跑，交给同步机制通知其他客户端。
			if (Main.netMode == NetmodeID.MultiplayerClient || Player.dead) {
				spawnTimer = 0;
				growthTimer = 0;
				return;
			}

			if (HasBloodMushroom) {
				spawnTimer++;
				if (spawnTimer >= SpawnCheckInterval) {
					spawnTimer = 0;
					if (!RedMistGlobalNPC.AnyActive() && Main.rand.NextFloat() < SpawnRollChance) {
						RedMistGlobalNPC.SpawnNear(Player, ComboLevel);
					}
				}
			}
			else {
				spawnTimer = 0;
			}

			bool wellFed = Player.HasBuff(BuffID.WellFed) || Player.HasBuff(BuffID.WellFed2) || Player.HasBuff(BuffID.WellFed3);
			if (wellFed) {
				growthTimer++;
				if (growthTimer >= GrowthCheckInterval) {
					growthTimer = 0;
					if (Main.rand.NextFloat() < GrowthRollChance) {
						NaturalGrowthSystem.TryGrowRandomNear(Player, GrowthRadius);
					}
				}
			}
			else {
				growthTimer = 0;
			}
		}
	}
}
