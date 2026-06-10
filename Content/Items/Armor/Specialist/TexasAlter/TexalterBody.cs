using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Specialist.TexasAlter
{
	[AutoloadEquip(EquipType.Body)]
	public class TexalterBody : NeoArmorBody
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 80;

		public override void Load() {
			if (Main.netMode == NetmodeID.Server)
				return;

			EquipLoader.AddEquipTexture(Mod, $"{Texture}_{EquipType.Back}", EquipType.Back, this);
		}
		public override void SetStaticDefaultsNoServer()
		{
			if (Main.netMode == NetmodeID.Server)
				return;

			int cape = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Back);

			ArmorIDs.Body.Sets.HidesTopSkin[Item.bodySlot] = true;
			ArmorIDs.Body.Sets.HidesArms[Item.bodySlot] = true;
			ArmorIDs.Body.Sets.IncludedCapeBack[Item.bodySlot] = cape;
			ArmorIDs.Body.Sets.IncludedCapeBackFemale[Item.bodySlot] = cape;
		}

		public override void SetArmorDefaults() {
			Item.defense = 24;
		}
		
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<TexalterBody>(1)
			.AddIngredient<Orundum>(60)
			.AddIngredient<PolymerizationPreparation>(6)
			.AddIngredient<RefinedSolvent>(6)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
