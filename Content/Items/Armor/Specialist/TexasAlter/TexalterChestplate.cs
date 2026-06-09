using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Specialist.TexasAlter
{
	public class TexalterChestplate : TexalterSetBodyPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(TexalterBody);
		protected override int VanityItemType => ModContent.ItemType<TexalterBody>();

		protected override void SetSetDefaults() {
			Item.defense = 24;
		}
	}
}
