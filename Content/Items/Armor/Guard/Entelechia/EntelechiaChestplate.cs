using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Entelechia
{
	public class EntelechiaChestplate : EntelechiaSetBodyPiece
	{
		public override int Rarity => 5;
		protected override string VanityItemName => nameof(EntelechiaBody);
		protected override int VanityItemType => ModContent.ItemType<EntelechiaBody>();

		protected override void SetSetDefaults() {
			Item.defense = 34;
		}
	}
}
