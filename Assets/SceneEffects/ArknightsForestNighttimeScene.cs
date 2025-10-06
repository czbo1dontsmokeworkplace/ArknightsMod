using ArknightsMod.Common.Configs;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Assets.SceneEffects
{
	internal class ArknightsForestNighttimeScene : ModSceneEffect
	{
		public override int Music => MusicLoader.GetMusicSlot(Mod, "Music/asisaw");
		public override SceneEffectPriority Priority => SceneEffectPriority.BiomeLow;
		public override bool IsLoadingEnabled(Mod mod) {
			return ModContent.GetInstance<MusicConfig>().EnableArknightsForestNighttime;
		}
		public override bool IsSceneEffectActive(Player player) {
			return player.active
				&& player.ZoneOverworldHeight
				&& player.ZoneForest
				&& !Main.dayTime
				&& !player.ZoneDesert
				&& !player.ZoneBeach
				&& !player.ZoneHallow;
		}
	}
}