using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Defender.Mudrock
{
	[AutoloadEquip(EquipType.Body)]
	internal class MudrockBody : NeoArmorBody
	{
		private const string ChestplateSlotName = "MudrockChestplate";

		public override int Rarity => 6;
		public override int ArmorLifeBonus => 222;

		public override void SetArmorDefaults() {
			Item.defense = 50;
		}

		protected override string ArmorIconTexture => "ArknightsMod/Content/Items/Armor/Defender/Mudrock/MudrockChestplate";

		public override void SetVanityDefaults() {
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.useTime = 20;
			Item.useAnimation = 20;
			Item.UseSound = SoundID.Item4;
		}

		public override bool AltFunctionUse(Player player) => true;

		public override bool CanUseItem(Player player) => player.altFunctionUse == 2;

		public override bool? UseItem(Player player) {
			MudrockToggle.Toggle(this);
			return true;
		}

		public override void Load() {
			int chestplateSlot = EquipLoader.AddEquipTexture(Mod, "ArknightsMod/Content/Items/Armor/Defender/Mudrock/MudrockChestplate_Body", EquipType.Body, null, ChestplateSlotName);
			if (chestplateSlot >= 0 && chestplateSlot < ArmorIDs.Body.Sets.HidesArms.Length) {
				ArmorIDs.Body.Sets.HidesTopSkin[chestplateSlot] = true;
				ArmorIDs.Body.Sets.HidesArms[chestplateSlot] = true;
			}
		}

		public override void UpdateVanityEquip(Player player) {
			// 帧图（穿戴贴图）跟随形态切换：升级形态用胸甲贴图，否则用默认躯干贴图。
			Item.bodySlot = Item.neoarmor().helmetForm
				? EquipLoader.GetEquipSlot(Mod, ChestplateSlotName, EquipType.Body)
				: EquipLoader.GetEquipSlot(Mod, Name, EquipType.Body);
		}

		public override void ModifyVanityTooltips(List<TooltipLine> tooltips) {
			OperatorOutfitTooltipLayout.ApplyWrappedVanityLine(Mod, tooltips, "Mods.ArknightsMod.ArmorSets.Mudrock.ToggleHint");
		}
	}

	internal class MudrockBodyDrawLayer : PlayerDrawLayer
	{
		private Asset<Texture2D> texture;

		public override void Load() {
			texture = ModContent.Request<Texture2D>("ArknightsMod/Content/Items/Armor/Defender/Mudrock/MudrockChestplate_Body_EX");
		}

		public override void Unload() {
			texture = null;
		}

		public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
			Player player = drawInfo.drawPlayer;
			// 实际显示的躯干 = 社交/时装栏(armor[11]) 优先，否则盔甲栏(armor[1])；
			// 只要显示的是「已升级」的泥岩躯干（无论盔甲栏还是时装栏），就绘制额外胸甲层。
			Item shown = !player.armor[11].IsAir ? player.armor[11] : player.armor[1];
			return shown.type == ModContent.ItemType<MudrockBody>() && shown.neoarmor().helmetForm;
		}

		public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);

		protected override void Draw(ref PlayerDrawSet drawInfo) {
			Player player = drawInfo.drawPlayer;
			if (player.dead || player.invis)
				return;

			int bodyFrameIndex = player.bodyFrame.Y / player.bodyFrame.Height;
			Vector2 headgearOffset = Main.OffsetsPlayerHeadgear[bodyFrameIndex];
			Texture2D tex = texture.Value;
			Vector2 position = drawInfo.Position - Main.screenPosition
				+ new Vector2(player.width / 2 - player.bodyFrame.Width / 2, player.height - player.bodyFrame.Height + 4f)
				+ player.bodyPosition;
			Vector2 origin = drawInfo.bodyVect;

			DrawData drawData = new(tex, position.Floor() + origin + headgearOffset + new Vector2(0, -2),
				tex.Frame(9, 4, 0, 1), drawInfo.colorArmorBody, player.bodyRotation, origin, 1f, drawInfo.playerEffect, 0) {
				shader = player.cBody
			};
			drawInfo.DrawDataCache.Add(drawData);
		}
	}
}
