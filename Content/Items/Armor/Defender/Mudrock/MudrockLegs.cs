using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Defender.Mudrock
{
	[AutoloadEquip(EquipType.Legs)]
	internal class MudrockLegs : NeoArmorLegs
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 222;

		public override void SetArmorDefaults() {
			Item.defense = 17;
		}

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

		public override string Texture => "ArknightsMod/Content/Items/Armor/Defender/Mudrock/MudrockGreaves";

		public override void ModifyVanityTooltips(List<TooltipLine> tooltips) {
			OperatorOutfitTooltipLayout.ApplyWrappedVanityLine(Mod, tooltips, "Mods.ArknightsMod.ArmorSets.Mudrock.ToggleHint");
		}
	}
}
