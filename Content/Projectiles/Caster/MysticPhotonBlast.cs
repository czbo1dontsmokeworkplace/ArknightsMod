using ArknightsMod.Content.Projectiles.Guard.Reaper.Phoenix;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster;

public sealed class MysticPhotonBlast : ModProjectile
{
    private bool visualStarted;
    public override string Texture => "Terraria/Images/Projectile_0";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.friendly = Projectile.hostile = false;
        Projectile.penetrate = -1; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.timeLeft = 2; Projectile.netImportant = true;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override void AI()
    {
        if (visualStarted || Main.dedServ) return;
        visualStarted = true;
        // Retain only the original Proteus impact sound graph. No added
        // particles, blast hitbox, screen shake, or secondary explosion damage.
        PhoenixFx.Burst("soundemitter.player.53.hit", Projectile.Center,
            Projectile.velocity.SafeNormalize(Vector2.UnitX));
    }
    public override bool PreDraw(ref Color lightColor) => false;
}
