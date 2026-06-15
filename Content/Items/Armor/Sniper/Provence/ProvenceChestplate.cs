using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Provence
{
	public class ProvenceChestplate : ProvenceSetBodyPiece
	{
		public override int Rarity => 5;
		protected override string VanityItemName => nameof(ProvenceBody);
		protected override int VanityItemType => ModContent.ItemType<ProvenceBody>();

		protected override void SetSetDefaults() {
			Item.defense = 17;
		}
	}
}
