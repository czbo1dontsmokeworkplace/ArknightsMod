using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Caster.Mostima
{
	public class MostimaGreaves : MostimaSetLegsPiece
	{
		public override int Rarity => 5;
		protected override string VanityItemName => nameof(MostimaLegs);
		protected override int VanityItemType => ModContent.ItemType<MostimaLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 3;
		}
	}
}
