using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Friendly
{
	/// <summary>坎诺特进入敌对阶段时 为附近玩家播放战斗音乐。</summary>
	public class CannotBattleSceneEffect : ModSceneEffect
	{
		// 与 Cannot 的战斗目标范围一致 120 格，1 格为 16 像素。
		private const float BattleMusicDistancePixels = 120f * 16f;

		public override int Music => MusicLoader.GetMusicSlot(Mod, "Sounds/Music/CannotBattle");

		public override SceneEffectPriority Priority => SceneEffectPriority.BossLow;

		public override bool IsSceneEffectActive(Player player) {
			foreach (NPC npc in Main.ActiveNPCs) {
				if (npc.ModNPC is Cannot cannot && cannot.Phase == CannotPhase.Hostile &&
					Vector2.DistanceSquared(player.Center, npc.Center) <=
					BattleMusicDistancePixels * BattleMusicDistancePixels)
					return true;
			}

			return false;
		}
	}
}
