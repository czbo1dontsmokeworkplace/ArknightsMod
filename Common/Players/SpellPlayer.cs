using ArknightsMod.Common.Damageclasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace ArknightsMod.Common.Players
{

	public class SpellPlayer : ModPlayer
	{
		public float PlayerSpellResist = 0; // 免疫倍数，默认为1
										   // 免疫玩家类，用于处理玩家的免疫状态
										   // 可以在这里添加属性和方法来管理玩家的免疫状态

		public override void ModifyHitByProjectile(Projectile projectile, ref Player.HurtModifiers modifiers) {
			if (SpellDamageConfig.SpellProjectiles.Contains(projectile.type)) {
				// 法术伤害无视护甲
				modifiers.ScalingArmorPenetration += 1f;
				// 法术抗性
				modifiers.FinalDamage *= 1f - (PlayerSpellResist / 100);
			}
		}

		public override void ResetEffects() 
		{
			PlayerSpellResist = 0;
			for(int i = 0;i<10;i++) {
				if (SpellDamageConfig.SpellDefense.ContainsKey(Player.armor[i].type)) {
					PlayerSpellResist += SpellDamageConfig.SpellDefense.TryGetValue(Player.armor[i].type, out int value) ? value : 0;

				}
			}

		}
	}


}
