using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Crossbows;

internal static class CrossbowVisuals
{
    internal static Color ColorFor(CrossbowKind kind) => CrossbowSignatureVfx.Tint(kind);
    internal static void SpawnPulse(IEntitySource source, Vector2 position, Vector2 direction, CrossbowKind kind, bool muzzle)
    {
        // One owner creates the visual emitter; remote clients receive its ordinary spawn.
        if (source is not EntitySource_Parent parent || parent.Entity is not Projectile shot
            || shot.owner != Main.myPlayer) return;
        Projectile.NewProjectile(source, position, direction, ModContent.ProjectileType<CrossbowPulse>(),
            0, 0f, shot.owner, (int)kind, muzzle ? 1f : 0f);
    }
    internal static void DrawMuzzle(Vector2 center, Vector2 aim, CrossbowKind kind, float strength)
        => CrossbowSignatureVfx.DrawMuzzle(center, aim, kind, strength);
    internal static void DrawBolt(Projectile projectile, CrossbowKind kind, int age)
        => CrossbowFlightVfx.Draw(projectile, kind);
}

public sealed class CrossbowPulse : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.timeLeft = 20;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override void AI()
    {
        if (!Main.dedServ && Projectile.timeLeft == 20)
            CrossbowSignatureVfx.Pulse(Projectile, (CrossbowKind)(int)Projectile.ai[0], Projectile.ai[1] == 1f);
    }
    // Material particles carry impact feedback; the holdout draws the actual muzzle flash.
    public override bool PreDraw(ref Color lightColor) => false;
}
