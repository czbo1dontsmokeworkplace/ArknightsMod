using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Gitano;

public sealed class GitanoCrystal : ModProjectile
{
    public override string Texture => "ArknightsMod/Content/Projectiles/Sniper/Schwarz/TrailShard";

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 28;
        ProjectileID.Sets.TrailingMode[Type] = 2;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 20;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = 1;
        Projectile.timeLeft = 50;
        Projectile.extraUpdates = 2;
        Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }

    public override void AI()
    {
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        if (Main.dedServ) return;
        Lighting.AddLight(Projectile.Center, new Vector3(.31f, .13f, .48f));
        if (Projectile.numUpdates == 0) GitanoVisuals.FlightParticles(Projectile.Center, Projectile.velocity);
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        Projectile.Kill();
        return false;
    }

    public override void OnKill(int timeLeft)
    {
        if (Projectile.owner == Main.myPlayer)
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<GitanoBurst>(), Math.Max(1, (int)(Projectile.damage * .7f)),
                Projectile.knockBack, Projectile.owner);
        if (!Main.dedServ)
            SoundEngine.PlaySound(SoundID.Item27 with { Volume = .35f, Pitch = .35f }, Projectile.Center);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        GitanoVisuals.DrawCrystal(Projectile);
        return false;
    }
}
