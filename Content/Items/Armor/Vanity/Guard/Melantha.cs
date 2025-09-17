using ArknightsMod.Content.Items.Armor.Vanity.Sniper;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Guard
{
	[AutoloadEquip(EquipType.Head)]
	public class MelanthaHead : ArknightsVanityHead
	{
		public override void Load() {
			if (Main.netMode == NetmodeID.Server)
				return;
			EquipLoader.AddEquipTexture(Mod, $"{Texture}_{EquipType.Back}", EquipType.Back, this);
		}
		public override int Rarity => ItemRarityID.Orange;
	}

	[AutoloadEquip(EquipType.Body)]
	public class MelanthaBody : ArknightsVanityBody
	{
		public override int Rarity => ItemRarityID.Orange;
	}

	[AutoloadEquip(EquipType.Legs)]
	public class MelanthaLegs : ArknightsVanityLegs
	{
		public override int Rarity => ItemRarityID.Orange;
	}

	public class MelanthaHeadLayer : PlayerDrawLayer
	{
		public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
			Item head = new(ModContent.ItemType<MelanthaHead>());
			return drawInfo.drawPlayer.head == head.headSlot && !drawInfo.drawPlayer.dead;
		}
		protected override void Draw(ref PlayerDrawSet drawInfo) {
			var drawPlayer = drawInfo.drawPlayer;
			var texture = ModContent.Request<Texture2D>("ArknightsMod/Content/Items/Armor/Vanity/Guard/MelanthaHead_Back", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
			int dyeShader = drawPlayer.dye?[1].dye ?? 0;
			Vector2 drawPosition = drawInfo.Center - Main.screenPosition;
			drawPosition += new Vector2(0, drawPlayer.height - 48f);
			drawPosition = new Vector2((int)drawPosition.X, (int)drawPosition.Y);

			var data = new DrawData(texture, drawPosition, null,
					drawInfo.colorArmorBody, drawPlayer.fullRotation, drawInfo.bodyVect, 1f, drawInfo.playerEffect, 0) {
				shader = dyeShader
			};
			drawInfo.DrawDataCache.Add(data);
		}
		public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.BackAcc);
	}
}
