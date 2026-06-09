using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Medic.Warfarin
{
	public class WarfarinChestplate : WarfarinSetBodyPiece
	{
		public override int Rarity => 5;
		protected override string VanityItemName => nameof(WarfarinBody);
		protected override int VanityItemType => ModContent.ItemType<WarfarinBody>();

		protected override void SetSetDefaults() {
			Item.defense = 10;
		}
	}
}
