using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Defender.Nian
{
	public class NianGreaves : NianSetLegsPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(NianLegs);
		protected override int VanityItemType => ModContent.ItemType<NianLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
