using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Oblivionis
{
	public class OblivionisChestplate : OblivionisSetBodyPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(OblivionisBody);
		protected override int VanityItemType => ModContent.ItemType<OblivionisBody>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
