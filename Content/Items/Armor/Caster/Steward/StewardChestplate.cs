using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Caster.Steward
{
	public class StewardChestplate : StewardSetBodyPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(StewardBody);
		protected override int VanityItemType => ModContent.ItemType<StewardBody>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
