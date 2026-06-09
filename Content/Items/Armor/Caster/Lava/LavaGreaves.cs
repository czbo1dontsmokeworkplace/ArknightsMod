using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Caster.Lava
{
	public class LavaGreaves : LavaSetLegsPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(LavaLegs);
		protected override int VanityItemType => ModContent.ItemType<LavaLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 3;
		}
	}
}
