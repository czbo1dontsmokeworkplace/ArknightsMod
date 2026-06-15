using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Specialist.Manticore
{
	public class ManticoreChestplate : ManticoreSetBodyPiece
	{
		public override int Rarity => 5;
		protected override string VanityItemName => nameof(ManticoreBody);
		protected override int VanityItemType => ModContent.ItemType<ManticoreBody>();

		protected override void SetSetDefaults() {
			Item.defense = 28;
		}
	}
}
