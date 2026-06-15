using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Endfield.Guard.Endministrator
{
	// 旧存档仍引用 Endministrator* 名称，映射到现行 EndminFemale* 物品
	public class EndministratorHead : EndminFemaleHead
	{
		public override string Texture => ModContent.GetInstance<EndminFemaleHead>().Texture;

		public override void SetVanityDefaults() {
			Item.headSlot = ModContent.GetInstance<EndminFemaleHead>().Item.headSlot; // 复用现行头部装备槽
		}
	}

	public class EndministratorBody : EndminFemaleBody
	{
		public override string Texture => ModContent.GetInstance<EndminFemaleBody>().Texture;

		public override void SetVanityDefaults() {
			Item.bodySlot = ModContent.GetInstance<EndminFemaleBody>().Item.bodySlot; // 复用现行身体装备槽
		}
	}

	public class EndministratorLegs : EndminFemaleLegs
	{
		public override string Texture => ModContent.GetInstance<EndminFemaleLegs>().Texture;

		public override void SetVanityDefaults() {
			Item.legSlot = ModContent.GetInstance<EndminFemaleLegs>().Item.legSlot; // 复用现行腿部装备槽
		}
	}
}
