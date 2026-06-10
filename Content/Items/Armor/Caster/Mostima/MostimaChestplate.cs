using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Caster.Mostima
{
	public class MostimaChestplate : MostimaSetBodyPiece
	{
		public override int Rarity => 5;
		protected override string VanityItemName => nameof(MostimaBody);
		protected override int VanityItemType => ModContent.ItemType<MostimaBody>();

		protected override void SetSetDefaults() {
			Item.defense = 10;
		}
	}
}
