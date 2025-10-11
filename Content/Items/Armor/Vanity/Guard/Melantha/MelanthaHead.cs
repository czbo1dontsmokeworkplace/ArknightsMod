using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Guard.Melantha
{
	[AutoloadEquip(EquipType.Head)]
	public class MelanthaHead : ArknightsVanityHead
	{
		public override int Rarity => 3;
		public override void Load() {
			if (Main.netMode == NetmodeID.Server)
				return;

			EquipLoader.AddEquipTexture(Mod, $"{Texture}_{EquipType.Back}", EquipType.Back, this);
		}
		internal class MelanthaHeadLayer : PlayerDrawLayer
		{
			protected override void Draw(ref PlayerDrawSet drawInfo) {
				var drawPlayer = drawInfo.drawPlayer;
				var texture = ModContent.Request<Texture2D>("ArknightsMod/Content/Items/Armor/Vanity/Guard/Melantha/MelanthaHead_Back", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
				int dyeShader = drawPlayer.dye?[1].dye ?? 0;
				Vector2 drawPosition = drawInfo.Center - Main.screenPosition;
				drawPosition += new Vector2(0, drawPlayer.height - 48f);
				drawPosition = new Vector2((int)drawPosition.X, (int)drawPosition.Y);

				if (drawPlayer.armor[10].type == ModContent.ItemType<MelanthaHead>()) {
					var data = new DrawData(texture, drawPosition, null,
						drawInfo.colorArmorBody, drawPlayer.fullRotation, drawInfo.bodyVect, 1f, drawInfo.playerEffect, 0) {
						shader = dyeShader
					};
					drawInfo.DrawDataCache.Add(data);
				}
			}
			public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.BackAcc);
		}
	}
}
