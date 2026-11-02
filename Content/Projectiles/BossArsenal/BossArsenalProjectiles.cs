using System;
using ArknightsMod.Content.Items.BossArsenal;
using ArknightsMod.Content.Projectiles.Guard.Reaper.Phoenix;
using ArknightsMod.Content.Projectiles.Charged;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.BossArsenal;

internal static class BossArsenalFx
{
    internal static Color Tint(int kind) => (kind / 4) switch { 0 => new(255, 45, 60), 1 => new(150, 220, 255), 2 => new(255, 110, 25), _ => new(255, 110, 25) };
    internal static void Ring(Vector2 at, float radius, Color color, float fade)
    {
        using var pass = new ChargedOperatorVfx.Pass(true);
        ChargedOperatorVfx.Sprite("RuptureRing", at, new Vector2(radius * 2, radius * .65f), color * fade);
    }
}

public sealed class BossArsenalShot : ModProjectile
{
    private PhoenixMotionFx missile;
    private int Age => (int)Projectile.localAI[0];
    internal int Kind => (int)Projectile.ai[0];
    public override string Texture => "Terraria/Images/MagicPixel";
    public override void SetStaticDefaults() { ProjectileID.Sets.TrailCacheLength[Type] = 18; ProjectileID.Sets.TrailingMode[Type] = 2; }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 14; Projectile.friendly = true; Projectile.timeLeft = 240;
        Projectile.penetrate = 1; Projectile.tileCollide = true; Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
    {
        Projectile.DamageType = Kind == 6 ? DamageClass.Magic : DamageClass.Ranged;
        if (Kind == 6) { Projectile.Resize(80, 80); Projectile.tileCollide = false; Projectile.timeLeft = 180; }
        if (Kind == 5) Projectile.timeLeft = 100;
        if (Kind == 7) { Projectile.extraUpdates = 1; Projectile.timeLeft = 150; }
        if (Kind == 9) { Projectile.timeLeft = 32; Projectile.penetrate = -1; Projectile.localNPCHitCooldown = 10; }
        if (Kind is 3 or 7 or 11) Projectile.DamageType = DamageClass.Summon;
    }
    public override bool? CanDamage() => Kind == 6 && Age < 12 ? false : null;
    public override void AI()
    {
        Projectile.localAI[0]++;
        if (Kind == 1)
        {
            if (!Main.dedServ) { missile ??= new PhoenixMotionFx(36, Projectile); if (Age == 1) missile.Activate("actor.player.weapon.36.activation"); }
            float speed = Projectile.velocity.Length();
            if (speed > 0) Projectile.velocity *= Math.Min(32, speed + .24f) / speed;
        }
        else if (Kind == 7) {
            if (!Main.dedServ) missile ??= NoxPreludeVisuals.Create(Projectile, false);
            NPC target = Projectile.ai[1] > 0 && Main.npc.IndexInRange((int)Projectile.ai[1] - 1) ? Main.npc[(int)Projectile.ai[1] - 1] : null;
            if (target == null || !target.CanBeChasedBy(Projectile)) target = Projectile.FindTargetWithinRange(900, true);
            float speed = Math.Min(14, Projectile.velocity.Length() + .18f);
            float angle = Projectile.velocity.ToRotation();
            if (target != null && Age > 8) angle = angle.AngleTowards((target.Center - Projectile.Center).ToRotation(), .07f);
            Projectile.velocity = angle.ToRotationVector2() * speed;
        }
        else if (Kind == 3) Projectile.velocity.Y = Math.Min(18, Projectile.velocity.Y + .14f);
        else if (Kind == 5) { Projectile.velocity.Y = Math.Min(14, Projectile.velocity.Y + .22f); Projectile.rotation += Projectile.velocity.X * .025f; }
        else if (Kind == 6)
        {
            if (Age < 12) Projectile.velocity = Vector2.Zero;
            else {
                Projectile.velocity = new Vector2(0, Math.Min(64, 42 + (Age - 12) * 4));
                Projectile.tileCollide = Projectile.Bottom.Y >= Projectile.ai[1] - 96;
                if (Projectile.Bottom.Y >= Projectile.ai[1]) Projectile.Kill();
            }
        }
        else if (Kind == 9) { Projectile.velocity *= .96f; Projectile.scale = 1 + Age * .045f; Projectile.Resize((int)(22 * Projectile.scale), (int)(22 * Projectile.scale)); }
        else if (Kind == 11) Projectile.velocity.Y += .16f;
        Projectile.rotation = Kind is 5 or 6 ? Projectile.rotation : Projectile.velocity.ToRotation();
        if (!Main.dedServ) Lighting.AddLight(Projectile.Center, BossArsenalFx.Tint(Kind).ToVector3() * .4f);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Kind is 5 or 6 or 7) target.AddBuff(BuffID.Frostburn2, 180);
        if (Kind is 9 or 11) target.AddBuff(BuffID.OnFire, 180);
    }
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (Kind == 6) {
            if (oldVelocity.Y > 0 && Projectile.velocity.Y != oldVelocity.Y && Projectile.owner == Main.myPlayer)
                CoffinImpact.FireGround(Projectile);
            return true;
        }
        if (Kind != 5) return true;
        if (Projectile.velocity.X != oldVelocity.X) Projectile.velocity.X = -oldVelocity.X * .55f;
        if (Projectile.velocity.Y != oldVelocity.Y) Projectile.velocity.Y = -oldVelocity.Y * .45f;
        Projectile.netUpdate = true;
        return false;
    }
    public override void OnKill(int timeLeft)
    {
        missile?.Stop();
        if (Kind == 7 && !Main.dedServ)
            NoxPreludeVisuals.Impact(Projectile, false);
        if (Projectile.owner != Main.myPlayer || Kind == 9 || Kind == 7) return;
        float radius = Kind switch { 1 => 280, 5 => 110, 6 => 160, _ => 64 };
        var p = Projectile.NewProjectileDirect(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero,
            ModContent.ProjectileType<BossArsenalBurst>(), Projectile.damage, Projectile.knockBack, Projectile.owner, Kind, radius);
        p.DamageType = Projectile.DamageType; p.CritChance = Projectile.CritChance;
    }
    public override bool PreDraw(ref Color lightColor)
    {
        if (Kind == 1) { BossArsenalPolishVisuals.Missile(Projectile); return false; }
        if (Kind == 6) { BossArsenalPolishVisuals.Coffin(Projectile, Age); return false; }
        if (Kind is 9 or 11) { BossArsenalPolishVisuals.Flame(Projectile, Kind == 9 ? Age / 32f : Age / 65f); return false; }
        if (Kind == 7) { BossArsenalPolishVisuals.IceMissile(Projectile); return false; }
        int texture = Kind == 5 ? ProjectileID.Grenade : Kind is 9 or 11 ? ProjectileID.Flames : ProjectileID.IceBolt;
        Main.instance.LoadProjectile(texture); Texture2D body = TextureAssets.Projectile[texture].Value;
        Rectangle frame = body.Frame(1, Math.Max(1, Main.projFrames[texture]));
        Main.EntitySpriteDraw(body, Projectile.Center - Main.screenPosition, frame,
            Kind == 5 ? Color.LightCyan : BossArsenalFx.Tint(Kind), Projectile.rotation, frame.Size() * .5f, Projectile.scale, SpriteEffects.None);
        return false;
    }
}

public sealed class BossArsenalBurst : ModProjectile
{
    private int Kind => (int)Projectile.ai[0];
    public override string Texture => "Terraria/Images/MagicPixel";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 4; Projectile.friendly = true; Projectile.penetrate = -1;
        Projectile.tileCollide = false; Projectile.timeLeft = 24;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
    {
        Vector2 center = Projectile.Center; Projectile.width = Projectile.height = (int)(Projectile.ai[1] * 2); Projectile.Center = center;
        if (Kind == 1) Projectile.timeLeft = 36;
        Projectile.DamageType = Kind % 4 == 3 ? DamageClass.Summon : Kind == 6 ? DamageClass.Magic : Kind % 4 == 0 ? DamageClass.Melee : DamageClass.Ranged;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => Projectile.localAI[0] <= 1 ? null : false;
    public override bool? Colliding(Rectangle a, Rectangle b) => Vector2.DistanceSquared(Projectile.Center, b.ClosestPointInRect(Projectile.Center)) <= Projectile.ai[1] * Projectile.ai[1];
    public override void AI()
    {
        if (Projectile.localAI[0]++ != 0 || Main.dedServ) return;
        SoundEngine.PlaySound(SoundID.Item14 with { Volume = Kind == 1 ? 1 : .7f, Pitch = Kind == 6 ? -.5f : 0 }, Projectile.Center);
        if (Kind == 5) BossArsenalPolishVisuals.SnowBurst(Projectile.Center);
        if (Kind == 1) { SalvoExplosionVisuals.Burst(Projectile.Center); return; }
        for (int i = 0; i < (Kind == 1 ? 60 : 28); i++)
        {
            Vector2 ray = (i * 2.399963f).ToRotationVector2();
            Dust d = Dust.NewDustPerfect(Projectile.Center + ray * Main.rand.NextFloat(5, 30), Kind is 5 or 6 ? DustID.IceTorch : Kind / 4 == 2 ? DustID.Torch : DustID.FireworksRGB,
                ray * Main.rand.NextFloat(3, Kind == 1 ? 17 : 10), 0, BossArsenalFx.Tint(Kind), Kind == 1 ? 2 : 1.3f); d.noGravity = true;
        }
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Kind is 4 or 5 or 6) target.AddBuff(BuffID.Frostburn2, 180);
        if (Kind / 4 == 2) target.AddBuff(BuffID.OnFire, 180);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        if (Kind == 1) { SalvoExplosionVisuals.Draw(Projectile.Center, Projectile.localAI[0] / 36f, Projectile.ai[1], Projectile.identity); return false; }
        float t = Projectile.localAI[0] / 24f;
        BossArsenalFx.Ring(Projectile.Center, Projectile.ai[1] * MathF.Sqrt(Math.Min(1, t * 2)), BossArsenalFx.Tint(Kind), (1 - t) * .8f);
        if (Kind == 5) BossArsenalPolishVisuals.DrawSnowBurst(Projectile.Center, t);
        if (Kind == 6)
        {
            BossArsenalFx.Ring(Projectile.Center, Projectile.ai[1] * t, Color.LightCyan, 1 - t);
            using var pass = new ChargedOperatorVfx.Pass(true);
            ChargedOperatorVfx.Sprite("LinearSpark", Projectile.Center, new Vector2(24, 230) * (1 - t), Color.LightCyan * (1 - t));
            for (int i = 0; i < 7; i++)
            {
                Vector2 ray = (-MathHelper.Pi + i * MathHelper.Pi / 6).ToRotationVector2();
                ChargedOperatorVfx.Sprite("LinearSpark", Projectile.Center + ray * (20 + t * 130),
                    new Vector2(5, 50 * (1 - t)), new Color(150, 190, 255) * (1 - t), ray.ToRotation());
            }
        }
        return false;
    }
}
