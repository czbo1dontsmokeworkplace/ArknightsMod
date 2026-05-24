using ArknightsMod.Content.Projectiles.Summoner;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Buffs.Summoner
{
	public class DeepcolorMinionBuff : ModBuff
	{
		// 暂用武器图标，避免缺少 Buff 贴图导致模组无法编译成功
		public override string Texture => $"{nameof(ArknightsMod)}/Content/Items/Weapons/Summoner/DeepcolorSketch";

		public override void SetStaticDefaults() {
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex) {
			if (player.ownedProjectileCounts[ModContent.ProjectileType<DeepcolorMinion>()] > 0)
				player.buffTime[buffIndex] = 2;
			else {
				player.DelBuff(buffIndex);
				buffIndex--;
			}
		}
	}
}
