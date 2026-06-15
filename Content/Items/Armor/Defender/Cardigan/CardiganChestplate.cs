using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Defender.Cardigan
{
	public class CardiganChestplate : CardiganSetBodyPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(CardiganBody);
		protected override int VanityItemType => ModContent.ItemType<CardiganBody>();

		protected override void SetSetDefaults() {
			Item.defense = 36;
		}
	}
}
