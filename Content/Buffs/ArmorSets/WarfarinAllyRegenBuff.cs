using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Buffs.ArmorSets
{
	public class WarfarinAllyRegenBuff : ModBuff
	{
		// Legacy registration only; the helmet now modifies regeneration directly.
		public override void SetStaticDefaults() {
			Main.buffNoSave[Type] = true;
		}
	}
}
