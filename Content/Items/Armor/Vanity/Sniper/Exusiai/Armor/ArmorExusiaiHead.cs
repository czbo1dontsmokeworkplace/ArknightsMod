using ArknightsMod.Common;
using ArknightsMod.Content.Items.Armor.Vanity.Guard.Melantha.Armor;
using ArknightsMod.Content.Items.Armor.Vanity.Guard.Utage;
using ArknightsMod.Content.Tiles.Infrastructure;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Content.Items.Armor.Vanity.Sniper.Exusiai.Armor
{
	[AutoloadEquip(EquipType.Head)]
	internal class ArmorExusiaiHead:ArknightsArmorHead
	{
		public override int Rarity => 6;
		public override int LifeBonus => 168;
		public override void SetArmorDefaults() {
			Item.defense = 0;
		}
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient(ModContent.ItemType<UtageBody>(), 1)
			.AddIngredient(ModContent.ItemType<Orundum>(), 30)
			.AddTile(ModContent.TileType<FactoryTile>())
			.Register();
		}
		public override bool IsArmorSet(Item head, Item body, Item legs) {
			return body.type == ModContent.ItemType<ArmorExusiaiBody>() &&
				legs.type == ModContent.ItemType<ArmorExusiaiLegs>();
		}
		public override void UpdateArmorSet(Player player) {
			player.setBonus = "";
			player.GetModPlayer<ExusiaiSetPlayer>().ExusiaiSetActive = true;
		}
		public override void UpdateArmorEquip(Player player) {
			Lighting.AddLight(player.Center, new Vector3(1f, 1f, 1f));
		}
		public override void UpdateVanity(Player player) {
			Lighting.AddLight(player.Center, new Vector3(1f, 1f, 1f));
		}
	}

	internal class ArmorExusiaiHeadLayer : PlayerDrawLayer
	{
		public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
			Item head = new(ModContent.ItemType<ArmorExusiaiHead>());
			return drawInfo.drawPlayer.head == head.headSlot && !drawInfo.drawPlayer.dead;
		}
		protected override void Draw(ref PlayerDrawSet drawInfo) {
			Texture2D texture = ModContent.Request<Texture2D>("ArknightsMod/Content/Items/Armor/Vanity/Sniper/Exusiai/ExusiaiHead_Ring").Value;

			var offset = new Vector2(1, -3) + new Vector2(0, -26);
			PlayerLayerHelper.AddPlayerDrawLayer(ref drawInfo, texture, 0, offset);
		}
		public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.BackAcc);
	}
}
