using ArknightsMod.Content.Items.Armor.Vanity.Defender.Beagle.Armor;
using ArknightsMod.Content.Tiles.Infrastructure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Guard.Melantha.Armor
{
	[AutoloadEquip(EquipType.Head)]
	public class ArmorMelanthaHead : ArknightsArmorHead
	{
		public override int Rarity => 3;
		public override void SetArmorDefaults() {
			Item.defense = 0;
		}
		public override int LifeBonus => 140;
		public override void Load() {
			if (Main.netMode == NetmodeID.Server)
				return;

			EquipLoader.AddEquipTexture(Mod, $"{Texture}_{EquipType.Back}", EquipType.Back, this);
		}
		public override bool IsArmorSet(Item head, Item body, Item legs) {
			return body.type == ModContent.ItemType<ArmorMelanthaBody>() &&
				legs.type == ModContent.ItemType<ArmorMelanthaLegs>();
		}
		public override void UpdateArmorSet(Player player) {
			player.setBonus = "";
			player.GetModPlayer<MelanthaSetPlayer>().MelanthaSetActive = true;
		}
		public override void AddRecipes()
			{
			CreateRecipe()
			.AddIngredient(ModContent.ItemType<MelanthaHead>(), 1)
			.AddIngredient(ModContent.ItemType<Orundum>(), 30)
			.AddTile(ModContent.TileType<FactoryTile>())
			.Register();
		}

		public override void UpdateArmorEquip(Player Player) {
				Player.GetModPlayer<ArknightsArmorPlayer>().extraDefenseBonus-=0.2f ;
		}
		}
	}
	
