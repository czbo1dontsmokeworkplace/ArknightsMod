using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.LaPluma
{
	public class LaPlumaChestplate : LaPlumaSetBodyPiece
	{
		public override int Rarity => 5;
		protected override string VanityItemName => nameof(LaPlumaBody);
		protected override int VanityItemType => ModContent.ItemType<LaPlumaBody>();

		protected override void SetSetDefaults() {
			Item.defense = 34;
		}
	}
}
