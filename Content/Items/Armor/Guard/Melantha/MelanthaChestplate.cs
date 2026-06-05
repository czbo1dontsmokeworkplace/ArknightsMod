using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Melantha
{
	public class MelanthaChestplate : MelanthaSetBodyPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(MelanthaBody);
		protected override int VanityItemType => ModContent.ItemType<MelanthaBody>();

		protected override void SetSetDefaults() {
			Item.defense = 12;
		}
	}
}
