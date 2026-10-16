using System;
using System.IO;
using ArknightsMod.Content.Items.Weapons.Vanguard;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Vanguard.Spears;

// ai[0] operator, ai[1] final throw (1) / mother spear (0) / child spear (2), ai[2] real age.
public sealed class VanguardThrownSpear : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    public VanguardSpearKind Kind => (VanguardSpearKind)(int)Projectile.ai[0];
    private bool split, hitEnemy, finalImpact;
    private Vector2 start;
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 28;
        ProjectileID.Sets.TrailingMode[Type] = 2;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 14; Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Melee; Projectile.ignoreWater = true; Projectile.tileCollide = true;
        Projectile.extraUpdates = VanguardSpearRules.FlightUpdates - 1;
        Projectile.timeLeft = 80 * VanguardSpearRules.FlightUpdates;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override void OnSpawn(IEntitySource source)
    {
        start = Projectile.Center;
        Projectile.penetrate = Projectile.ai[1] == 2 ? 1 : VanguardSpearRules.Penetration(Kind);
    }
    public override void SendExtraAI(BinaryWriter writer)
    { writer.Write(split); writer.Write(finalImpact); writer.Write(start.X); writer.Write(start.Y); }
    public override void ReceiveExtraAI(BinaryReader reader)
    { split = reader.ReadBoolean(); finalImpact = reader.ReadBoolean(); start = new(reader.ReadSingle(), reader.ReadSingle()); }
    public override void AI()
    {
        if (!VanguardSpearRules.ValidKind((int)Kind) || !float.IsFinite(Projectile.velocity.X) || !float.IsFinite(Projectile.velocity.Y))
        { Projectile.Kill(); return; }
        Projectile.rotation = Projectile.velocity.ToRotation();
        if (Projectile.numUpdates != 0) return;
        Projectile.ai[2]++;
        if (Projectile.ai[2] >= 75 || Vector2.DistanceSquared(start, Projectile.Center) > 1400 * 1400)
        { Projectile.Kill(); return; }
        Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        VanguardSpearVfx.Flight(Projectile.Center, direction, Kind, (int)Projectile.ai[2], Projectile.ai[1] == 2);
        // Mother spear releases its internal child after 12 frames, or immediately on impact.
        // Children are explicitly tagged and can never split again.
        if (Kind == VanguardSpearKind.WildMane && Projectile.ai[1] != 2 && Projectile.ai[2] == 12)
            Split();
    }
    public override bool? CanHitNPC(NPC target) => target.friendly ? false : null;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        // Compact leading edge plus the sub-update swept segment: the decorative shaft is not
        // a giant damage rectangle, and high-speed throws cannot tunnel through thin enemies.
        float point = 0;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
            Projectile.Center - Projectile.velocity, Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX) * 7, 12, ref point);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        hitEnemy = true;
        VanguardSpearVfx.Impact(Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.UnitX), Kind, Projectile.ai[1] == 1);
        if (Projectile.owner != Main.myPlayer) return;
        if (Kind == VanguardSpearKind.WildMane && Projectile.ai[1] != 2) Split();
        if (!finalImpact && Projectile.ai[1] == 1)
        {
            finalImpact = true; Projectile.netUpdate = true;
            float radius = Kind == VanguardSpearKind.Bagpipe ? 76 : Kind == VanguardSpearKind.Vigna ? 40 : 52;
            int blastDamage = Kind == VanguardSpearKind.Bagpipe ? Projectile.damage : Kind == VanguardSpearKind.Vigna ? Projectile.damage / 2 : 0;
            VanguardSpearImpact.Spawn(Projectile, Projectile.Center, Kind, blastDamage, radius);
        }
        // Deliberately no Reserve.FillFromMelee here, on children, or on impact projectiles.
    }
    private void Split()
    {
        if (split || Projectile.owner != Main.myPlayer) return;
        split = true; Projectile.netUpdate = true;
        Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        for (int i = -1; i <= 1; i += 2)
        {
            Projectile child = Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center,
                direction.RotatedBy(i * .11f) * VanguardSpearRules.FlightSpeed(Kind) * 1.12f / VanguardSpearRules.FlightUpdates,
                Type, Math.Max(1, Projectile.damage / 2), Projectile.knockBack * .6f, Projectile.owner, (int)Kind, 2);
            child.CritChance = Projectile.CritChance;
        }
        VanguardSpearVfx.Impact(Projectile.Center, direction, Kind, false);
    }
    public override void OnKill(int timeLeft)
    {
        // Wall/lifetime expiry is only a quiet visual fizzle, never an explosion or a split.
        if (!hitEnemy) VanguardSpearVfx.Fizzle(Projectile.Center, Kind);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        VanguardSpearVfx.FlightRibbon(Projectile, Kind);
        Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        float length = Projectile.ai[1] == 2 ? 42 : 62 + (int)Kind * 5;
        VanguardSpearVfx.DrawSpear(Projectile.Center - direction * length * .78f, Projectile.Center, Kind, lightColor, Projectile.ai[1] == 1);
        return false;
    }
}
