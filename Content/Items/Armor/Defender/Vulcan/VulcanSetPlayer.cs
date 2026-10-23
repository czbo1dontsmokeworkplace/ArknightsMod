using ArknightsMod.Content.Items.Armor;
using ArknightsMod.Content.Items.Armor.NeoArmorReforge;
using ArknightsMod.Players;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Defender.Vulcan
{
	internal class VulcanSetPlayer : ArknightsArmorPlayer, global::ArknightsMod.Players.IProbabilityDodgeSource
	{
    public string DodgeName => "火神头盔（仅NPC碰撞）";
    public double DodgeChance => VulcanHelmetActive && SkillActive ? .25 : 0;
    public bool CanDodge(Player.HurtInfo info) => info.DamageSource != null
        && info.DamageSource.SourceNPCIndex >= 0
        && info.DamageSource.SourceProjectileLocalIndex < 0;

		public bool VulcanHelmetActive;
		public bool VulcanSetActive;

		public override void ResetEffects() {
			VulcanHelmetActive = false;
			VulcanSetActive = false;
		}

		// NeoArmor Reforge：套装件是独立 ItemID，穿上它本身就代表"已经是套装形态"，
		// 不需要再查 hasUpgraded；player.setBonus 文本交给 VulcanHead 的
		// SetProfile.SetBonusKey 统一设置，这里不再重复设置一遍。
		public override void PostUpdateEquips() {
			VulcanHelmetActive = Player.armor[0].type == NeoArmorReforgeSetLoader.GetSetType<VulcanHead>();
			VulcanSetActive = VulcanHelmetActive
				&& Player.armor[1].type == NeoArmorReforgeSetLoader.GetSetType<VulcanBody>()
				&& Player.armor[2].type == NeoArmorReforgeSetLoader.GetSetType<VulcanLegs>();
		}

		private bool SkillActive => Player.GetModPlayer<WeaponPlayer>().SkillActive;



		public override void UpdateLifeRegen() {
			if (VulcanSetActive && SkillActive)
				// Two lifeRegen units restore one HP per second: actual regen is 2% max life/s.
				Player.lifeRegen += (int)(Player.statLifeMax2 * 0.04f);
		}
	}
}
