using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Caster.Lava
{
	public class LavaHelmet : LavaSetHeadPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(LavaHead);
		protected override int VanityItemType => ModContent.ItemType<LavaHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
