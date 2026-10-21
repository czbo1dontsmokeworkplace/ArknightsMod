using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Buffs.ArmorSets
{
	public class HazeMagicFragileDebuff : ModBuff
	{
		public const int DurationTicks = 2 * 60;
		public const float ResistanceMultiplier = 0.8f;

		public override void SetStaticDefaults() {
			Main.debuff[Type] = true;
			Main.pvpBuff[Type] = true;
		}
	}
}
