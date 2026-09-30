using System;
using ArknightsMod.Content.NPCs.Enemy.Evolution;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Evolution;

// Player projectiles reuse the visual vocabulary, never the hostile encounter ownership/lifecycle.
public class EvolutionWeaponShot : ModProjectile
{
    private int Age => (int)Projectile.ai[1];
    private int Kind => (int)Projectile.ai[0];
    protected virtual DamageClass Class => DamageClass.Magic;
    public override string Texture => EvolutionVisuals.Root + "BloodClot";
    public override void SetStaticDefaults()
    { ProjectileID.Sets.TrailCacheLength[Type] = 16; ProjectileID.Sets.TrailingMode[Type] = 2; }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 18;
        Projectile.friendly = true; Projectile.hostile = false; Projectile.DamageType = Class;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.penetrate = 3; Projectile.timeLeft = 180;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = 18;
    }
    internal static NPC FindTarget(Vector2 center, float range, Player owner)
    {
        if (owner.HasMinionAttackTargetNPC && Main.npc.IndexInRange(owner.MinionAttackTargetNPC))
        {
            NPC designated = Main.npc[owner.MinionAttackTargetNPC];
            if (designated.CanBeChasedBy() && Vector2.Distance(center, designated.Center) < range) return designated;
        }
        NPC best = null;
        foreach (NPC npc in Main.ActiveNPCs)
            if (npc.CanBeChasedBy() && Vector2.Distance(center, npc.Center) < range && Collision.CanHitLine(center, 1, 1, npc.position, npc.width, npc.height))
            { best = npc; range = Vector2.Distance(center, npc.Center); }
        return best;
    }
    public override bool ShouldUpdatePosition() => Kind != 4 && Kind != 2;
    public override bool? CanCutTiles() => false;
    public override bool CanHitPvp(Player target) => false;
    public override bool? CanDamage() => Kind == 2 && Age < 18 ? false : null;
    public override void AI()
    {
        Projectile.ai[1]++;
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead) { Projectile.Kill(); return; }
        if (Kind == 4)
        {
            Projectile.Center = owner.MountedCenter;
            if (Age >= 28) { Projectile.Kill(); return; }
            owner.heldProj = Projectile.whoAmI;
            Projectile.rotation = Projectile.velocity.ToRotation() + (Age / 28f - .5f) * 1.8f * owner.direction;
            if (Age == 15 && Projectile.owner == Main.myPlayer)
                for (int i = -1; i <= 1; i++) Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + Projectile.rotation.ToRotationVector2() * 100,
                    (Projectile.rotation + i * .12f).ToRotationVector2() * 10, Type, Projectile.damage / 3, 2, Projectile.owner, 0);
            return;
        }
        if (Kind == 2) { if (Age >= 32) Projectile.Kill(); return; }
        if (Kind == 1)
        {
            if (Age > 18 && FindTarget(Projectile.Center, 750, owner) is NPC target)
                Projectile.velocity = Projectile.velocity.ToRotation().AngleTowards((target.Center - Projectile.Center).ToRotation(), .065f).ToRotationVector2() * 13;
        }
        else
        {
            Projectile.velocity.Y = Math.Min(17, Projectile.velocity.Y + (Kind == 3 ? .28f : .2f));
            Projectile.tileCollide = Age > 10 && !Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height);
        }
        Projectile.rotation = Kind == 3 ? Projectile.rotation + Projectile.velocity.X * .04f : Projectile.velocity.ToRotation();
        if (!Main.dedServ) Lighting.AddLight(Projectile.Center, .45f, .015f, .04f);
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle box)
    {
        if (Kind != 4 && Kind != 2) return null;
        float length = Kind == 4 ? MathF.Sin(Age / 28f * MathHelper.Pi) * 230 : 750;
        float collision = 0;
        Vector2 direction = Kind == 4 ? Projectile.rotation.ToRotationVector2() : Projectile.velocity.SafeNormalize(Vector2.UnitY);
        return Collision.CheckAABBvLineCollision(box.TopLeft(), box.Size(), Projectile.Center, Projectile.Center + direction * length, Kind == 4 ? 24 : 10, ref collision);
    }
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (Kind != 3) return true;
        Projectile.ai[2]++;
        if (Projectile.ai[2] > 2) return true;
        if (Projectile.velocity.Y != oldVelocity.Y) Projectile.velocity.Y = -Math.Abs(oldVelocity.Y) * .55f;
        if (Projectile.velocity.X != oldVelocity.X) Projectile.velocity.X = -oldVelocity.X;
        EvolutionVisuals.Burst(Projectile.Center, .5f, false);
        return false;
    }
    public override void OnKill(int timeLeft)
    {
        EvolutionVisuals.Burst(Projectile.Center, .4f, false);
        if (Kind == 3 && Projectile.owner == Main.myPlayer)
            for (int i = 0; i < 4; i++) Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center,
                new Vector2((i - 1.5f) * 3, -6), Type, Projectile.damage / 3, 1, Projectile.owner, 0);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        if (Kind == 4)
        {
            Vector2 end = Projectile.Center + Projectile.rotation.ToRotationVector2() * (MathF.Sin(Age / 28f * MathHelper.Pi) * 230);
            EvolutionVisuals.Tendril(Projectile.Center, end, 0, Age, 24);
            EvolutionVisuals.Glow(end, EvolutionVisuals.Core * .7f, new Vector2(30));
        }
        else if (Kind == 2)
        {
            Vector2 end = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * 750;
            EvolutionVisuals.Line(Projectile.Center, end, EvolutionVisuals.Blood * (Age < 18 ? .4f : .8f), Age < 18 ? 1 : 12);
            if (Age >= 18) EvolutionVisuals.Line(Projectile.Center, end, EvolutionVisuals.Core * .9f, 3);
        }
        else
        {
            EvolutionVisuals.Trail(Projectile, 10, EvolutionVisuals.Blood);
            EvolutionVisuals.Glow(Projectile.Center, EvolutionVisuals.Blood * .8f, new Vector2(45));
            if (Kind == 1) EvolutionVisuals.Glow(Projectile.Center, EvolutionVisuals.Core, new Vector2(18, 9), Projectile.rotation);
            else
            {
                Texture2D t = EvolutionVisuals.Asset(Kind == 3 ? "BloodRock" : "BloodClot");
                Main.spriteBatch.Draw(t, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, t.Size() * .5f,
                    (Kind == 3 ? 42f : 20f) / Math.Max(t.Width, t.Height), SpriteEffects.None, 0);
            }
        }
        return false;
    }
}
public sealed class EvolutionMeleeShot : EvolutionWeaponShot { protected override DamageClass Class => DamageClass.Melee; }
public sealed class EvolutionRangedShot : EvolutionWeaponShot { protected override DamageClass Class => DamageClass.Ranged; }
public sealed class EvolutionSummonShot : EvolutionWeaponShot
{
    protected override DamageClass Class => DamageClass.Summon;
    public override void SetStaticDefaults() { base.SetStaticDefaults(); ProjectileID.Sets.MinionShot[Type] = true; }
}
