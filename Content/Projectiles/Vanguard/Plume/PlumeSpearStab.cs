using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Vanguard.Plume
{
	public class PlumeSpearStab : ModProjectile
	{
		public override string Texture =>
			"ArknightsMod/Content/Items/Weapons/Vanguard/Plume/PlumePike_protile";

		protected virtual float HoldoutRangeMin => 20f;
		protected virtual float HoldoutRangeMax => 56f;

		public override void SetDefaults() {
			Projectile.CloneDefaults(ProjectileID.Spear);
			Projectile.width  = 70;
			Projectile.height = 70;
			Projectile.usesLocalNPCImmunity  = true;
			Projectile.localNPCHitCooldown   = -1;
		}

		public override bool PreAI() {
			Player player   = Main.player[Projectile.owner];
			int    duration = player.itemAnimationMax;

			player.heldProj = Projectile.whoAmI;

			if (Projectile.timeLeft > duration)
				Projectile.timeLeft = duration;

			Projectile.velocity = Vector2.Normalize(Projectile.velocity);

			float returnDuration = duration * 0.8f;
			float progress = Projectile.timeLeft < returnDuration
				? Projectile.timeLeft / returnDuration
				: 1 - (Projectile.timeLeft - returnDuration) / (duration - returnDuration);

			Projectile.Center = player.MountedCenter + Vector2.SmoothStep(
				Projectile.velocity * HoldoutRangeMin,
				Projectile.velocity * HoldoutRangeMax,
				progress);

			Projectile.rotation = Projectile.velocity.ToRotation();

			return false;
		}

		public override bool PreDraw(ref Color lightColor) {
			Texture2D tex     = ModContent.Request<Texture2D>(Texture).Value;
			float     ang     = Projectile.velocity.ToRotation();
			Vector2   origin  = tex.Size() / 2f;
			Vector2   drawPos = Projectile.Center - Main.screenPosition;
			Main.spriteBatch.Draw(tex, drawPos, null, lightColor, ang, origin, Projectile.scale, SpriteEffects.None, 0f);
			return false;
		}
	}
}
