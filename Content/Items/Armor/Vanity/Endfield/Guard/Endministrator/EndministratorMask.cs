using ArknightsMod.Content.Items.Armor.Vanity;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Endfield.Guard.Endministrator
{
	public class EndministratorMask : ModItem
	{
		public override string Texture => "ArknightsMod/Content/Items/Armor/Vanity/Endfield/Guard/Endministrator/Endmin_Female_Mask_Item";
		public int Rarity => 6;
		public int Value => 560000;

		public override void Load()
		{
			if (Main.netMode == NetmodeID.Server)
				return;

			EquipLoader.AddEquipTexture(Mod, "ArknightsMod/Content/Items/Armor/Vanity/Endfield/Guard/Endministrator/Endmin_Female_Mask", EquipType.Face, this, Name);
		}

		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 1;
			if (Main.netMode == NetmodeID.Server)
				return;

			Item.faceSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Face);
		}

		public override void SetDefaults()
		{
			Item.width = 28;
			Item.height = 28;
			Item.rare = ArknightsVanityHead.GetRarity(Rarity);
			Item.value = Value;
			Item.accessory = true;
			Item.vanity = true;

			if (Main.netMode == NetmodeID.Server)
				return;

			if (Item.faceSlot < 0)
				Item.faceSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Face);
		}

		internal class EndministratorMaskLayer : PlayerDrawLayer
		{
			public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);
			public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
			{
				Player player = drawInfo.drawPlayer;
				if (player.dead || player.invis)
					return false;

				int type = ModContent.ItemType<EndministratorMask>();
				for (int i = 0; i < player.armor.Length; i++)
				{
					Item item = player.armor[i];
					if (item != null && !item.IsAir && item.type == type)
						return true;
				}

				return false;
			}

			protected override void Draw(ref PlayerDrawSet drawInfo)
			{
				Player player = drawInfo.drawPlayer;
				if (player.dead || player.invis)
					return;

				Texture2D texture = ModContent.Request<Texture2D>("ArknightsMod/Content/Items/Armor/Vanity/Endfield/Guard/Endministrator/Endmin_Female_Mask").Value;

				int frameIndex = player.bodyFrame.Y / player.bodyFrame.Height;
				Vector2 rawHeadgearOffset = Main.OffsetsPlayerHeadgear[frameIndex];
				Vector2 headgearOffset = rawHeadgearOffset * player.gravDir;
				Vector2 basePos = new Vector2(
					(int)(drawInfo.Position.X - Main.screenPosition.X - (float)(player.bodyFrame.Width / 2) + (float)(player.width / 2)),
					(int)(drawInfo.Position.Y - Main.screenPosition.Y + (float)player.height - (float)player.bodyFrame.Height + 4f));
				bool isHeadBobbingFrame = (frameIndex >= 7 && frameIndex <= 9) || (frameIndex >= 14 && frameIndex <= 16);
				float extraDownOnRaisedFrames = isHeadBobbingFrame ? 2f : 0f;
				Vector2 position = basePos + player.headPosition + drawInfo.headVect + drawInfo.helmetOffset + headgearOffset + new Vector2(0f, (-1f + extraDownOnRaisedFrames) * player.gravDir);
				Vector2 origin = drawInfo.headVect;

				Rectangle frame = texture.Frame(1, 20, 0, frameIndex);
				DrawData drawData = new(texture, position.Floor(), frame, drawInfo.colorArmorHead, player.headRotation, origin, 1f, drawInfo.playerEffect, 0)
				{
					shader = player.dye?[0].dye ?? 0
				};
				drawInfo.DrawDataCache.Add(drawData);
			}
		}
	}
}
