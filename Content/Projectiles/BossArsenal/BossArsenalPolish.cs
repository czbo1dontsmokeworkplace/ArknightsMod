using System;
using System.Reflection;
using ArknightsMod.Content.Projectiles.Charged;
using ArknightsMod.Content.Projectiles.Guard.Reaper.Phoenix;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.BossArsenal;

public sealed class HarbingerSickle : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.DeathSickle;
    public override void SetStaticDefaults() => Main.projFrames[Type] = Math.Max(1, Main.projFrames[ProjectileID.DeathSickle]);
    public override void SetDefaults()
    {
        Projectile.CloneDefaults(ProjectileID.DeathSickle);
        Projectile.aiStyle = -1; Projectile.DamageType = DamageClass.Melee;
        Projectile.tileCollide = false; Projectile.penetrate = 4; Projectile.timeLeft = 95;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = 20;
    }
    public override void AI()
    {
        Projectile.localAI[0]++;
        if (++Projectile.frameCounter >= 4) { Projectile.frameCounter = 0; Projectile.frame = (Projectile.frame + 1) % Math.Max(1, Main.projFrames[Type]); }
        Projectile.rotation += .32f * (Projectile.velocity.X < 0 ? -1 : 1);
        Projectile.velocity *= Projectile.localAI[0] < 30 ? .965f : .91f;
        Projectile.Opacity = Math.Min(1, Projectile.timeLeft / 18f);
        if (!Main.dedServ) Lighting.AddLight(Projectile.Center, .55f, .05f, .16f);
    }
    public override Color? GetAlpha(Color lightColor) => new Color(255, 145, 175) * Projectile.Opacity;
}

// Darklight Greatsword: a slowly-launched rotating beam that accelerates,
// then slows and fades. Winter Scar keeps exactly two total hits.
public sealed class WinterScarBeam : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.IceSickle;
    public override void SetStaticDefaults() { ProjectileID.Sets.TrailCacheLength[Type] = 10; ProjectileID.Sets.TrailingMode[Type] = 2; }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 32; Projectile.friendly = true; Projectile.DamageType = DamageClass.Melee;
        Projectile.penetrate = 2; Projectile.timeLeft = 150; Projectile.tileCollide = true;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override void AI()
    {
        if (Projectile.timeLeft > 30) Projectile.velocity *= Projectile.velocity.Length() < 24 ? 1.055f : 1;
        else Projectile.velocity *= .925f;
        Projectile.rotation += .55f * Math.Max(.1f, Math.Min(1, Projectile.timeLeft / 30f));
        Projectile.Opacity = Math.Min(1, Projectile.timeLeft / 30f);
        if (!Main.dedServ) Lighting.AddLight(Projectile.Center, .12f, .5f, .75f);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(BuffID.Frostburn2, 120);
    public override bool PreDraw(ref Color lightColor)
    {
        Main.instance.LoadProjectile(ProjectileID.IceSickle); var tex = TextureAssets.Projectile[ProjectileID.IceSickle].Value;
        for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            if (Projectile.oldPos[i] != Vector2.Zero) Main.EntitySpriteDraw(tex, Projectile.oldPos[i] + Projectile.Size * .5f - Main.screenPosition,
                null, Color.Cyan * (.25f * (1 - i / (float)Projectile.oldPos.Length) * Projectile.Opacity), Projectile.rotation, tex.Size() * .5f, 1, SpriteEffects.None);
        Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Color.LightCyan * Projectile.Opacity,
            Projectile.rotation, tex.Size() * .5f, 1, SpriteEffects.None); return false;
    }
}

public sealed class BasaltSpark : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    public override void SetDefaults()
    {
        Projectile.width = 6; Projectile.height = 12; Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Melee; Projectile.penetrate = 3; Projectile.timeLeft = 60;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = 15;
    }
    public override void AI()
    {
        if (Projectile.localAI[0]++ > 5) { Projectile.velocity.Y = Math.Min(16, Projectile.velocity.Y + .2f); Projectile.velocity.X *= .99f; }
        if (!Main.dedServ) { var d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch, -Projectile.velocity * .15f, 0, Color.Orange, 1.2f); d.noGravity = true; }
    }
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (Projectile.velocity.X != oldVelocity.X) Projectile.velocity.X = -oldVelocity.X * .35f;
        if (Projectile.velocity.Y != oldVelocity.Y) Projectile.velocity.Y = -oldVelocity.Y * .45f;
        return false;
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(BuffID.OnFire, 120);
    public override bool PreDraw(ref Color lightColor)
    {
        using var pass = new ChargedOperatorVfx.Pass(true);
        ChargedOperatorVfx.Line(Projectile.Center, Projectile.Center - Projectile.velocity * 2, Color.Orange, 3);
        ChargedOperatorVfx.Line(Projectile.Center, Projectile.Center - Projectile.velocity, Color.LightYellow, 1);
        return false;
    }
}

internal static class CoffinImpact
{
    internal static void FireGround(Projectile coffin)
    {
        // Surface-following placement, not arbitrary air patches or fires inside tiles.
        for (int offset = -4; offset <= 4; offset++)
        {
            Vector2 point = coffin.Bottom + new Vector2(offset * 28, -48);
            bool found = false;
            for (int y = 0; y < 13; y++, point.Y += 8)
                if (Collision.SolidCollision(point, 4, 4)) { found = true; break; }
            if (!found) continue;
            var fire = Projectile.NewProjectileDirect(coffin.GetSource_FromThis(), point - new Vector2(0, 18), Vector2.Zero,
                ModContent.ProjectileType<BlackIceGroundFire>(), Math.Max(1, coffin.damage / 3), 1, coffin.owner);
            fire.CritChance = coffin.CritChance;
        }
    }
}

public sealed class BlackIceGroundFire : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    public override void SetDefaults()
    {
        Projectile.width = 30; Projectile.height = 42; Projectile.friendly = true; Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = -1; Projectile.timeLeft = 60; Projectile.tileCollide = false;
        Projectile.usesIDStaticNPCImmunity = true; Projectile.idStaticNPCHitCooldown = 10;
    }
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        Projectile.localAI[0]++;
        if (!Main.dedServ && Projectile.timeLeft % 3 == 0)
        {
            var dust = Dust.NewDustPerfect(Projectile.Bottom + new Vector2(Main.rand.NextFloat(-12, 12), -4), DustID.Shadowflame,
                new Vector2(0, -2), 100, new Color(70, 50, 110), 1.2f); dust.noGravity = true;
        }
    }
    public override bool PreDraw(ref Color lightColor) { BossArsenalPolishVisuals.GroundFire(Projectile); return false; }
}

public sealed class YetiCrownBeam : ModProjectile
{
    private PhoenixBeamFx visual;
    private float length;
    internal bool IsFiring => Projectile.timeLeft <= 42;
    public override string Texture => "Terraria/Images/MagicPixel";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 6; Projectile.friendly = true; Projectile.DamageType = DamageClass.Summon;
        Projectile.timeLeft = 54; Projectile.tileCollide = false; Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = 10; Projectile.netImportant = true;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => IsFiring ? null : false;
    public override void AI()
    {
        int index = (int)Projectile.ai[1];
        if (!Main.projectile.IndexInRange(index)) { Projectile.Kill(); return; }
        var parent = Main.projectile[index];
        if (!parent.active || parent.owner != Projectile.owner || parent.identity != (int)Projectile.ai[2]
            || parent.ModProjectile is not BossArsenalMinion || parent.ai[0] != 7
            || !Main.player[Projectile.owner].HasBuff<YetiChimeBuff>()) { Projectile.Kill(); return; }
        Projectile.Center = parent.Center;
        Projectile.damage = parent.damage;
        if (Projectile.owner == Main.myPlayer && Main.npc.IndexInRange((int)Projectile.ai[0]))
        {
            NPC target = Main.npc[(int)Projectile.ai[0]];
            if (target.CanBeChasedBy(Projectile))
            {
                float angle = Projectile.velocity.ToRotation().AngleTowards((target.Center - Projectile.Center).ToRotation(), .05f);
                Projectile.velocity = angle.ToRotationVector2(); Projectile.netUpdate = true;
            }
        }
        length = 0;
        for (int d = 8; d <= 960; d += 8)
        { if (Collision.SolidCollision(Projectile.Center + Projectile.velocity * d, 4, 4)) break; length = d; }
        if (!Main.dedServ && IsFiring)
        {
            visual ??= new PhoenixBeamFx("projectile.player.95.beam", () => Projectile.Center,
                () => Projectile.Center + Projectile.velocity * length, () => Projectile.active && IsFiring, 960,
                PhoenixFx.Colored(new Color(90, 190, 255), Color.White, .8f));
            visual.Extend();
        }
    }
    public override bool? Colliding(Rectangle a, Rectangle b)
    {
        float at = 0; return Collision.CheckAABBvLineCollision(b.TopLeft(), b.Size(), Projectile.Center,
            Projectile.Center + Projectile.velocity * length, 14, ref at);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        => visual?.HitAlong(Math.Clamp(Vector2.Dot(target.Center - Projectile.Center, Projectile.velocity), 0, length));
    public override void OnKill(int timeLeft) => visual?.Stop();
    public override bool PreDraw(ref Color lightColor)
    {
        if (!IsFiring) { using var pass = new ChargedOperatorVfx.Pass(true); ChargedOperatorVfx.Line(Projectile.Center,
            Projectile.Center + Projectile.velocity * length, Color.LightCyan * .3f, 1); } return false;
    }
}
