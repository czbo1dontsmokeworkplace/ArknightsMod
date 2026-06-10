using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Oblivionis
{
	public class OblivionisHelmet : OblivionisSetHeadPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(OblivionisHead);
		protected override int VanityItemType => ModContent.ItemType<OblivionisHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
