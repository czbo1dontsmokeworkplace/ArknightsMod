using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanguard.Fang
{
	public class FangHelmet : FangSetHeadPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(FangHead);
		protected override int VanityItemType => ModContent.ItemType<FangHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
