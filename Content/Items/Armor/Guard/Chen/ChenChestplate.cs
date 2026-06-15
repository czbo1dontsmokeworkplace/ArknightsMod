using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Chen
{
	public class ChenChestplate : ChenSetBodyPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(ChenBody);
		protected override int VanityItemType => ModContent.ItemType<ChenBody>();

		protected override void SetSetDefaults() {
			Item.defense = 30;
		}
	}
}
