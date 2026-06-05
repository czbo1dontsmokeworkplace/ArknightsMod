using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Mlynar
{
	public class MlynarHelmet : MlynarSetHeadPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(MlynarHead);
		protected override int VanityItemType => ModContent.ItemType<MlynarHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
