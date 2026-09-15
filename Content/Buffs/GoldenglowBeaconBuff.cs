using ArknightsMod.Content.Projectiles.Caster.Goldenglow;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Buffs
{
	// 显示自动释放的浮游单元数量
	public class GoldenglowBeaconBuff : ModBuff
	{
		public override void SetStaticDefaults() {
			Main.buffNoTimeDisplay[Type] = true;
			Main.buffNoSave[Type] = true;
		}

		public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare) {
			int count = player_BeaconCount;
			tip = Language.GetTextValue("Mods.ArknightsMod.Buffs.GoldenglowBeaconBuff.Description", count);
		}

		private int player_BeaconCount => Main.LocalPlayer.ownedProjectileCounts[ModContent.ProjectileType<GoldenglowBeacon>()];

		public override bool RightClick(int buffIndex) => false;
	}
}
