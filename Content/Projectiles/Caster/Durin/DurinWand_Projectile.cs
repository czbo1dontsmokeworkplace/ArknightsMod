using Microsoft.Xna.Framework;
using Terraria;
using System.IO;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Projectiles.Caster.CoreCasters;

namespace ArknightsMod.Content.Projectiles.Caster.Durin
{
	public class DurinWand_Projectile : ModProjectile, ICoreCasterGuidance
	{
		private float effectDistance;
		private CoreCasterFlight flight;
		public bool GuidanceReady => flight?.CanGuide(CoreCasterKind.Durin) == true;
		public override void SendExtraAI(BinaryWriter writer) => CoreCasterGuidance.Write(writer, flight);
		public override void ReceiveExtraAI(BinaryReader reader) => flight = CoreCasterGuidance.Read(reader);
		public override void SetStaticDefaults() {
			ProjectileID.Sets.TrailCacheLength[Type] = 16;
			ProjectileID.Sets.TrailingMode[Type] = 0;
		}

		public override void SetDefaults() {
			Projectile.width = 10;
			Projectile.height = 10;

			Projectile.aiStyle = 0;
			Projectile.friendly = true;
			Projectile.penetrate = 1; // How many monsters the projectile can penetrate.
			Projectile.tileCollide = true;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.ownerHitCheck = true; // Prevents hits through tiles. Most melee weapons that use projectiles have this

			AIType = ProjectileID.EnchantedBoomerang;
		}

		public override Color? GetAlpha(Color lightColor) {
			// return Color.White;
			return new Color(255, 255, 255, 0) * Projectile.Opacity;
		}

		public override void AI() {
			flight ??= new CoreCasterFlight(Projectile.Center, Projectile.velocity, Projectile.MaxUpdates, Projectile.identity);
			flight.Step(CoreCasterKind.Durin, 0, Projectile.Center, ref Projectile.velocity, Projectile.MaxUpdates,
				GuidanceReady ? CoreCasterGuidance.Target(Projectile, 380) : null);
			effectDistance += Projectile.velocity.Length();
			if (effectDistance >= 32) {
				effectDistance %= 32;
				CoreCasterVisuals.Trail(Projectile, CoreCasterKind.Durin);
			}
			Projectile.rotation = Projectile.velocity.ToRotation();
			Projectile.ai[0] += 1f;

			if (Projectile.ai[0] >= 30f)
				Projectile.Kill();
		}
		public override void OnKill(int timeLeft) => CoreCasterVisuals.Impact(Projectile.Center, Projectile.velocity, CoreCasterKind.Durin);
		public override bool PreDraw(ref Color lightColor) {
			CoreCasterVisuals.Bolt(Projectile, CoreCasterKind.Durin);
			return false;
		}

	}


}
