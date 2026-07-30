using ArknightsMod.Content.Items.Armor.Reforge;
using ArknightsMod.Content.Items.Armor;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Caster.Mornia
{
	// NeoArmor Reforge：头盔效果不再靠 UpdateEquip 里的一堆 if(hasUpgraded) 判断，
	// 而是 MorniaHead 的 SetProfile.OnHelmetActive 在套装头盔实际装备时直接调用
	// OnHelmetActive(player)——这个类现在只负责"记住这一帧是否生效"和真正的
	// 玩法效果（ModifyWeaponDamage），检测逻辑完全交给 ReforgeSetPiece。
	internal class MorniaSetPlayer : ModPlayer
	{
		public bool HelmetActive;

		public override void ResetEffects() {
			HelmetActive = false;
		}

		public static void OnHelmetActive(Player player) {
			player.GetModPlayer<MorniaSetPlayer>().HelmetActive = true;
		}

		public override void ModifyWeaponDamage(Item item, ref StatModifier damage) {
			if (HelmetActive && item.DamageType.CountsAsClass(DamageClass.Magic))
				damage *= 1.06f;
		}
	}
}
