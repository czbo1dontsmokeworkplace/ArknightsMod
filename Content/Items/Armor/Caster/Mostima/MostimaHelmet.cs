using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Caster.Mostima
{
	public class MostimaHelmet : MostimaSetHeadPiece
	{
		public override int Rarity => 5;
		protected override string VanityItemName => nameof(MostimaHead);
		protected override int VanityItemType => ModContent.ItemType<MostimaHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
