using System;
using ArknightsMod.Content.Items.Weapons.Vanguard;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Vanguard.Spears;

// Invisible damage carrier, rendered entirely with particles. Does not spawn more sparks.
public sealed class VanguardSpearSpark : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    public static void Release(Projectile source, Vector2 point, VanguardSpearKind kind, bool heavy)
    {
        if (source.owner != Main.myPlayer) return;
        // Replace the entire spark spray with one native grenade already in
        // its explosion phase. Retain the spray's combined base damage budget.
        int count = heavy ? 6 + (int)kind : Main.rand.Next(1, 4);
        int damage = Math.Max(1, (int)(source.damage * (heavy ? .1f : .18f))) * count;
        Projectile grenade = Projectile.NewProjectileDirect(source.GetSource_FromThis(), point, Vector2.Zero,
            ProjectileID.Grenade, damage, source.knockBack * .15f, source.owner);
        grenade.DamageType = DamageClass.Melee; grenade.CritChance = source.CritChance;
        grenade.usesLocalNPCImmunity = true; grenade.localNPCHitCooldown = -1;
        grenade.timeLeft = 1;
        grenade.PrepareBombToBlow();
        grenade.netUpdate = true;
    }
    public override void SetDefaults()
    {
        Projectile.width = 8; Projectile.height = 12;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Melee;
        Projectile.penetrate = 5; Projectile.timeLeft = 90;
        Projectile.ignoreWater = true; Projectile.tileCollide = true;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = 15;
    }
    public override bool? CanDamage() => Projectile.ai[1] >= 5 ? null : false;
    public override void AI()
    {
        if (!VanguardSpearRules.ValidKind((int)Projectile.ai[0]) || !float.IsFinite(Projectile.velocity.X) || !float.IsFinite(Projectile.velocity.Y))
        { Projectile.Kill(); return; }
        Projectile.ai[1]++;
        if (Projectile.ai[1] > 5)
        {
            Projectile.velocity.X *= .985f;
            Projectile.velocity.Y = Math.Min(10, Projectile.velocity.Y + .2f);
        }
        VanguardSpearVfx.Spark(Projectile.Center, Projectile.velocity, (VanguardSpearKind)(int)Projectile.ai[0], (int)Projectile.ai[1]);
    }
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        // Two weak ricochets, then expire. Never persist inside terrain.
        if (++Projectile.ai[2] > 2) return true;
        if (oldVelocity.X != Projectile.velocity.X) Projectile.velocity.X = -oldVelocity.X * .4f;
        if (oldVelocity.Y != Projectile.velocity.Y) Projectile.velocity.Y = -oldVelocity.Y * .3f;
        Projectile.netUpdate = true;
        return false;
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        => VanguardSpearVfx.Spark(Projectile.Center, -Projectile.velocity * .3f, (VanguardSpearKind)(int)Projectile.ai[0], 0);
    public override bool PreDraw(ref Color lightColor) => false;
}
