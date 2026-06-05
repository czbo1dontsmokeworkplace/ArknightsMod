using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Medic.Ansel
{
	public class AnselChestplate : AnselSetBodyPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(AnselBody);
		protected override int VanityItemType => ModContent.ItemType<AnselBody>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
