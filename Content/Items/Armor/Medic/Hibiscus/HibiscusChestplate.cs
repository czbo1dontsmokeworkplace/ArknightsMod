using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Medic.Hibiscus
{
	public class HibiscusChestplate : HibiscusSetBodyPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(HibiscusBody);
		protected override int VanityItemType => ModContent.ItemType<HibiscusBody>();

		protected override void SetSetDefaults() {
			Item.defense = 8;
		}
	}
}
