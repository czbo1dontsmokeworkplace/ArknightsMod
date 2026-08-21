using ArknightsMod.Content.Tiles.Natural;
using ArknightsMod.Systems;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Players
{
	public class FrostCrystalTreePlayer : ModPlayer
	{
		public const int BaseValue = 6;
		private const int SmallKillGain = 1;
		private const int BossKillGain = 10;
		private const int HurtLoss = 5;

		private const int GrowthCheckInterval = 60; // 约1秒判定一次低概率生长
		private const float GrowthRollChance = 0.001f;
		private const int GrowthRadius = 30;

		public bool HasFrostCrystalTree;
		public int Value = BaseValue;
		public bool Broken;

		private int growthTimer;

		public override void ResetEffects() {
			HasFrostCrystalTree = false;
		}

		public void RegisterKill(bool isBoss) {
			if (Broken) return;
			Value += isBoss ? BossKillGain : SmallKillGain;
		}

		private void RegisterHurt() {
			if (Broken) return;
			Value -= HurtLoss;
			if (Value <= 0) Break();
		}

		private void Break() {
			if (Broken) return;
			Value = System.Math.Max(0, Value / 2);
			Broken = true;
		}

		public override void PostHurt(Player.HurtInfo info) {
			RegisterHurt();
		}

		public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource) {
			Break();
		}

		public override void PostUpdate() {
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				growthTimer = 0;
				return;
			}

			growthTimer++;
			if (growthTimer >= GrowthCheckInterval) {
				growthTimer = 0;
				if (Main.rand.NextFloat() < GrowthRollChance) {
					NaturalGrowthSystem.TryGrowRandomNear(Player, GrowthRadius, ModContent.TileType<FrostCrystalTree>());
				}
			}
		}
	}
}
