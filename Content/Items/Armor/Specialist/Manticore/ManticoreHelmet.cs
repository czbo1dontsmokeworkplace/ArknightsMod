using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Specialist.Manticore
{
	public class ManticoreHelmet : ManticoreSetHeadPiece
	{
		public override int Rarity => 5;
		protected override string VanityItemName => nameof(ManticoreHead);
		protected override int VanityItemType => ModContent.ItemType<ManticoreHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
