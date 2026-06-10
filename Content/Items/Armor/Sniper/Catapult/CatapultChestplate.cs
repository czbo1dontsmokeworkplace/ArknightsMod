using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Catapult
{
	public class CatapultChestplate : CatapultSetBodyPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(CatapultBody);
		protected override int VanityItemType => ModContent.ItemType<CatapultBody>();

		protected override void SetSetDefaults() {
			Item.defense = 7;
		}
	}
}
