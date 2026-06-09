using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Specialist.Mizuki
{
	public class MizukiGreaves : MizukiSetLegsPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(MizukiLegs);
		protected override int VanityItemType => ModContent.ItemType<MizukiLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 9;
		}
	}
}
