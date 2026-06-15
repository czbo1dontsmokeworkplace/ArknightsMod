using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Midnight
{
	public class MidnightChestplate : MidnightSetBodyPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(MidnightBody);
		protected override int VanityItemType => ModContent.ItemType<MidnightBody>();

		protected override void SetSetDefaults() {
			Item.defense = 21;
		}
	}
}
