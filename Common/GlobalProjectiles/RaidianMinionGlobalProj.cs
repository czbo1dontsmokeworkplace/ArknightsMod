using ArknightsMod.Common.GlobalNPCs;
using ArknightsMod.Content.Items.Armor.Supporter.Radian;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Common.GlobalProjectiles
{
	public class RaidianMinionGlobalProj : GlobalProjectile
	{
		public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers) {
			if (!projectile.DamageType.CountsAsClass(DamageClass.Summon))
				return;
			if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
				return;

			Player owner = Main.player[projectile.owner];
			if (!owner.active || owner.dead)
				return;
			RaidianSetPlayer state = owner.GetModPlayer<RaidianSetPlayer>();

			// 命中时动态加入固定攻击力，不改弹幕基础伤害，避免反复叠加或子弹幕继承两次。
			if (state.RaidianHelmetActive)
				modifiers.SourceDamage.Flat += (int)(owner.statLifeMax2 * RaidianSetPlayer.InspireDamageRatio);

			if (!state.RaidianSetActive || !target.GetGlobalNPC<RaidianMarkGlobalNPC>().RaidianMarked)
				return;

			// 额外 8% 攻击力的法术伤害。做成平砍加值而不是独立弹幕：独立弹幕会多触发一次
			// 命中判定，和吸血/护甲穿透之类的效果叠起来容易出问题。
			modifiers.FlatBonusDamage += projectile.damage * RaidianSetPlayer.MarkedBonusDamageRatio;
		}
	}
}
