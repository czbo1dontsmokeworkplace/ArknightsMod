using ArknightsMod.Content.Items.Weapons.BlastCasters;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.BlastCasters;

public sealed class AphrissaRelay : ModProjectile
{
    public override string Texture => "ArknightsMod/Common/Particle/DefaultParticle";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 28; Projectile.tileCollide = false;
        Projectile.ignoreWater = true; Projectile.penetrate = -1;
        Projectile.timeLeft = BlastCasterRules.RelayLife; Projectile.netImportant = true;
    }
    public override bool? CanDamage() => false;
    public override bool ShouldUpdatePosition() => false;
    internal static Projectile Find(Player owner)
    {
        foreach (Projectile relay in Main.ActiveProjectiles)
            if (relay.owner == owner.whoAmI && relay.ModProjectile is AphrissaRelay
                && Vector2.DistanceSquared(relay.Center, owner.MountedCenter) <= BlastCasterRules.RelayTetherRange * BlastCasterRules.RelayTetherRange) return relay;
        return null;
    }
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead || owner.HeldItem.ModItem is not AphrissaProjector
            || Vector2.DistanceSquared(Projectile.Center, owner.MountedCenter) > BlastCasterRules.RelayTetherRange * BlastCasterRules.RelayTetherRange
            || Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height))
        { Projectile.Kill(); return; }
        if (Projectile.owner == Main.myPlayer)
        {
            Vector2 next = (Main.MouseWorld - Projectile.Center).SafeNormalize(Vector2.UnitX * owner.direction);
            float rotation = next.ToRotation();
            if (System.Math.Abs(MathHelper.WrapAngle(rotation - Projectile.ai[0])) > .03f)
            { Projectile.ai[0] = rotation; Projectile.netUpdate = true; }
        }
        Projectile.rotation = Projectile.ai[0];
        if (Projectile.timeLeft % 10 == 0) BlastCasterVisuals.RelayIdle(Projectile.Center, Projectile.rotation);
        Lighting.AddLight(Projectile.Center, .06f, .22f, .20f);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        BlastCasterVisuals.DrawRelay(Projectile.Center, Projectile.rotation,
            System.Math.Min(1f, Projectile.timeLeft / 30f));
        return false;
    }
    public override void OnKill(int timeLeft) => BlastCasterVisuals.Skill(Projectile.Center, BlastCasterKind.Aphrissa);
}
