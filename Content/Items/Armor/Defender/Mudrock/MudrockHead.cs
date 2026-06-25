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
			Item.headSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);
		}

		public override void UpdateArmorEquip(Player player) {
			Item.headSlot = EquipLoader.GetEquipSlot(Mod, HelmetSlotName, EquipType.Head);
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
