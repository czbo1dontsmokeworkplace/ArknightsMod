using ArknightsMod.Content.Items.Weapons;
using ArknightsMod.Players;
using ArknightsMod.Systems.Gameplay.Skill;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Systems.Gameplay.Skill
{
	public class ArknightWeaponItem : GlobalItem
	{
		public override void HoldItem(Item item, Player player) {
			if (player.whoAmI != Main.myPlayer || item.ModItem is not UpgradeWeaponBase)
				return;

			var mp = player.GetModPlayer<WeaponPlayer>();
			var skill = mp.CurrentSkill;
			if (skill == null)
				return;

			mp.TryAutoCharge();
			if (skill.AutoUpdateActive)
				mp.UpdateActiveSkill();
		}
	}
}
