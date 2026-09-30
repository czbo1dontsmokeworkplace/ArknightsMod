using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Friendly
{
	/// <summary>玩家正在和友善阶段的坎诺特对话（逛商店）时播放的商店音乐。</summary>
	public class CannotShopSceneEffect : ModSceneEffect
	{
		public override int Music => MusicLoader.GetMusicSlot(Mod, "Sounds/Music/CannotShop");

		public override SceneEffectPriority Priority => SceneEffectPriority.Event;

		public override bool IsSceneEffectActive(Player player) {
			int talk = player.talkNPC;
			if (talk < 0 || talk >= Main.maxNPCs)
				return false;

			NPC npc = Main.npc[talk];
			return npc.active && npc.ModNPC is Cannot cannot && cannot.Phase == CannotPhase.Friendly;
		}
	}
}
