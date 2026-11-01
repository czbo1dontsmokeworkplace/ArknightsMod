using ArknightsMod.Content.Items.Armor;
using ArknightsMod.Content.Items.Armor.Supporter.CivilightEterna;
using ArknightsMod.Content.Items.Weapons.Medic;
using ArknightsMod.Systems.Gameplay.OperatorTags;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Supporter.CivilightEterna
{
	public class CivilightDustMote : ModProjectile
	{
		public override string Texture => "Terraria/Images/Projectile_502";

		public override void SetDefaults() {
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = false;
			Projectile.hostile = false;
			Projectile.tileCollide = false;
			Projectile.timeLeft = 2;
			Projectile.penetrate = -1;
		}

		public override void AI() {
			Projectile.timeLeft = 2;
			Player owner = Main.player[Projectile.owner];
			if (!owner.active || owner.dead || !owner.GetModPlayer<CivilightEternaSetPlayer>().CivilightEternaSetActive) {
				Projectile.Kill();
				return;
			}

			int slot = (int)Projectile.ai[0];
			float angle = Main.GlobalTimeWrappedHourly * 2f + slot * MathHelper.TwoPi / 3f;
			Projectile.Center = owner.Center + angle.ToRotationVector2() * 56f;

			if (Projectile.ai[1] > 0f) {
				Projectile.ai[1]--;
				return;
			}

			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			for (int i = 0; i < Main.maxPlayers; i++) {
				Player ally = Main.player[i];
				if (!ally.active || ally.dead || ally.whoAmI == owner.whoAmI)
					continue;
				if (!MedicHealingNet.Allied(owner, ally))
					continue;

				if (!OperatorTagRegistry.TryGetFromHelmet(ally.armor[0].type, out _))
					continue;

				if (Vector2.Distance(ally.Center, Projectile.Center) > 24f)
					continue;

				OperatorSkillHealHelper.ApplyHeal(ally, owner.statLifeMax2 / 10, owner);
				Projectile.ai[1] = 6 * 60;
				Projectile.netUpdate = true;
				break;
			}
		}

		public override bool PreDraw(ref Color lightColor) => Projectile.ai[1] <= 0f;
	}
}
