using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Defender.Mudrock
{
	[AutoloadEquip(EquipType.Head)]
	internal class MudrockHead : NeoArmorHead
	{
		private const string HelmetSlotName = "MudrockHelmet";

		public override int Rarity => 6;
		public override int ArmorLifeBonus => 443;

		public override void SetArmorDefaults() {
			Item.defense = 0;
		}

		protected override string ArmorIconTexture => "ArknightsMod/Content/Items/Armor/Defender/Mudrock/MudrockHelmet";

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
			int helmetSlot = EquipLoader.AddEquipTexture(Mod, "ArknightsMod/Content/Items/Armor/Defender/Mudrock/MudrockHelmet_Head", EquipType.Head, null, HelmetSlotName);
			if (helmetSlot >= 0 && helmetSlot < ArmorIDs.Head.Sets.DrawHead.Length)
				ArmorIDs.Head.Sets.DrawHead[helmetSlot] = false;
		}

		public override void UpdateVanityEquip(Player player) {
			// 帧图（穿戴贴图）跟随形态切换：升级形态用头盔贴图，否则用默认头部贴图。
			// 时装/社交栏也走此方法，故任意穿戴方式下切换形态都能改变帧图。
			Item.headSlot = Item.neoarmor().helmetForm
				? EquipLoader.GetEquipSlot(Mod, HelmetSlotName, EquipType.Head)
				: EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);
		}

		public override void ModifyVanityTooltips(List<TooltipLine> tooltips) {
			OperatorOutfitTooltipLayout.ApplyWrappedVanityLine(Mod, tooltips, "Mods.ArknightsMod.ArmorSets.Mudrock.ToggleHint");
		}

		public override bool IsArmorSet(Item head, Item body, Item legs) {
			return body.type == ModContent.ItemType<MudrockBody>() && body.neoarmor().hasUpgraded &&
				legs.type == ModContent.ItemType<MudrockLegs>() && legs.neoarmor().hasUpgraded;
		}

		public override void UpdateArmorSet(Player player) {
			OperatorSetEquipHelper.ApplySetBonusText(player, true, "Mods.ArknightsMod.ArmorSets.Mudrock.SetBonus");
		}
	}
}
