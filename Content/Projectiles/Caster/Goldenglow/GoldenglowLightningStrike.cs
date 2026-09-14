using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Goldenglow;

public sealed class GoldenglowLightningStrike : ModProjectile
{
    private const int Lifetime = 30;
    private GoldenglowLightningGeometry geometry;
    private GoldenglowLightningRenderer.Ribbon ribbon;
    private bool initialized;
    // ai: seed, mode (normal/overdrive/sky/drone/water visual), age.
    private int Mode => (int)Projectile.ai[1];
    private bool Strong => Mode is 1 or 2;
    private Vector2 End => Projectile.Center + Projectile.velocity;
    private Color Tint => Strong ? new Color(100, 165, 255) : new Color(60, 120, 255);
    public override string Texture => "ArknightsMod/Assets/Effects/Goldenglow/LightningImpact";

    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2500;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.friendly = true;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = Lifetime;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => Mode == 4 || Projectile.ai[2] < 2 || Projectile.ai[2] > 5 ? false : null;
    public override void Unload() => GoldenglowLightningRenderer.Unload();

    internal static void Spawn(IEntitySource source, Vector2 start, Vector2 end, int owner,
        int damage, float knockback, int mode)
    {
        float length = Vector2.Distance(start, end);
        if (owner != Main.myPlayer || !float.IsFinite(length) || length > 2400f)
            return;
        if (length < 8f)
            end = start + Vector2.UnitY * -8f;
        Projectile.NewProjectile(source, start, end - start, ModContent.ProjectileType<GoldenglowLightningStrike>(),
            damage, knockback, owner, Main.rand.Next(1, 1 << 24), mode);
    }

    public override void SendExtraAI(BinaryWriter writer) => writer.Write((byte)Projectile.timeLeft);
    public override void ReceiveExtraAI(BinaryReader reader) => Projectile.timeLeft = Math.Clamp((int)reader.ReadByte(), 1, Lifetime);

    private void EnsureGeometry() => geometry ??= new GoldenglowLightningGeometry(
        (uint)Projectile.ai[0], Vector2.Zero, Projectile.velocity);

    public override void AI()
    {
        if (!float.IsFinite(Projectile.velocity.X) || !float.IsFinite(Projectile.velocity.Y) ||
            Projectile.velocity.LengthSquared() > 2400f * 2400f || Projectile.velocity.LengthSquared() < 1f)
        {
            Projectile.Kill();
            return;
        }
        EnsureGeometry();
        if (geometry.Bolts.Count == 0)
        {
            Projectile.Kill();
            return;
        }
        if (!initialized)
        {
            initialized = true;
            if (!Main.dedServ && Projectile.ai[2] < 10)
                SpawnImpact();
            if (Projectile.owner == Main.myPlayer && Mode < 3)
            {
                Vector2[] path = geometry.Bolts[^1].Points;
                // Check the actual strike path too: a bolt can cross a pool before reaching the cursor.
                Vector2? water = GoldenglowConductiveWater.IsWater(End) ? End : null;
                for (int i = path.Length - 1; !water.HasValue && i >= 0; i--)
                    if (GoldenglowConductiveWater.IsWater(Projectile.Center + path[i]))
                        water = Projectile.Center + path[i];
                if (water.HasValue)
                    GoldenglowConductiveWater.Energize(Projectile, water.Value, Strong);
            }
        }
        if (!Main.dedServ)
        {
            float light = MathHelper.Clamp(1f - Projectile.ai[2] / 20f, 0f, 1f);
            var points = geometry.Bolts[^1].Points;
            for (int i = 0; i < points.Length; i += 12)
                Lighting.AddLight(Projectile.Center + points[i], Tint.ToVector3() * light);
        }
        Projectile.ai[2]++;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        EnsureGeometry();
        if (geometry.Bolts.Count == 0)
            return false;
        // Same path as the visible main bolt; fork branches remain decorative like LightningStrike.
        var points = geometry.Bolts[^1].Points;
        float collision = 0f;
        for (int i = 1; i < points.Length; i++)
            if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                Projectile.Center + points[i - 1], Projectile.Center + points[i], Strong ? 22f : 12f,
                ref collision))
                return true;
        float radius = Strong ? 76f : 38f;
        return Vector2.DistanceSquared(targetHitbox.ClosestPointInRect(End), End) <= radius * radius;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(BuffID.Electrified, Strong ? 240 : 120);

    private void SpawnImpact()
    {
        if (Mode < 3)
            SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Assets/Effects/Goldenglow/LightningStrikeHit")
                { Volume = Strong ? 0.7f : 0.34f, PitchVariance = 0.16f, MaxInstances = 4 }, End);
        if (Mode == 1)
            Main.instance.CameraModifiers.Add(new PunchCameraModifier(End, Vector2.UnitY,
                3.5f, 6f, 10, 1100f, "GoldenglowOverdrive"));
        int count = Mode == 4 ? 3 : Strong ? 24 : Mode == 3 ? 5 : 12;
        for (int i = 0; i < count; i++)
        {
            Vector2 velocity = Main.rand.NextVector2Circular(Strong ? 10f : 6f, Strong ? 8f : 4f);
            Dust dust = Dust.NewDustPerfect(End, DustID.Electric, velocity, 100, Tint, Strong ? 1.15f : 0.75f);
            dust.noGravity = true;
            if (i % 3 == 0)
            {
                Dust spark = Dust.NewDustPerfect(End, DustID.FireworkFountain_Blue, velocity * 1.4f,
                    100, Color.White, 0.75f);
                spark.noGravity = true;
            }
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        EnsureGeometry();
        ribbon ??= new GoldenglowLightningRenderer.Ribbon(geometry);
        float progress = MathHelper.Clamp(Projectile.ai[2] / Lifetime, 0f, 1f);
        ribbon.Draw(Projectile.Center, Strong ? 22f : Mode >= 3 ? 7f : 12f,
            Tint, progress, Strong ? 1.35f : 1f);
        if (Mode < 4)
            GoldenglowLightningRenderer.DrawFlare(End, Tint, Strong ? 1.3f : Mode == 3 ? 0.35f : 0.65f,
                MathHelper.Clamp(1f - progress * 1.5f, 0f, 1f));
        return false;
    }
}
