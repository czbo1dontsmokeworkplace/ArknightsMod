using System;
using ArknightsMod.Content.Items.Weapons.Vanguard;
using ArknightsMod.Content.Projectiles.Charged;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Vanguard.Spears;

// Finite, non-damaging release/impact geometry. All sizes are world-space pixels.
public sealed class VanguardThrowPulse : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    public static void Spawn(Projectile source, Vector2 point, bool impact)
    {
        if (source.owner != Main.myPlayer) return;
        Projectile.NewProjectile(source.GetSource_FromThis(), point, Vector2.Zero,
            ModContent.ProjectileType<VanguardThrowPulse>(), 0, 0, source.owner,
            source.ai[0], impact ? 1 : 0, source.velocity.ToRotation());
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2; Projectile.timeLeft = VanguardSpearRules.PulseLifetime;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override void AI()
    {
        if (Projectile.localAI[0]++ == 0)
        {
            var kind = (VanguardSpearKind)(int)Projectile.ai[0];
            VanguardSpearVfx.SignatureBurst(Projectile.Center, Projectile.ai[2].ToRotationVector2(), kind, Projectile.ai[1] == 1);
            if (!Main.dedServ) SoundEngine.PlaySound((kind == VanguardSpearKind.Bagpipe ? SoundID.Item14 : SoundID.Item70)
                with { Volume = Projectile.ai[1] == 1 ? .7f : .4f, Pitch = -.25f + (int)kind * .08f }, Projectile.Center);
        }
    }
    public override bool PreDraw(ref Color lightColor)
    {
        if (!VanguardSpearVfx.Visible(Projectile.Center)) return false;
        var kind = (VanguardSpearKind)(int)Projectile.ai[0];
        bool hit = Projectile.ai[1] == 1;
        float p = Math.Clamp(1 - Projectile.timeLeft / (float)VanguardSpearRules.PulseLifetime, 0, 1);
        if (hit) { DrawExpandingImpact(kind, p); return false; }
        float fade = MathF.Pow(1 - p, 1.1f);
        float radius = (hit ? 72 + (int)kind * 18 : 48 + (int)kind * 10) * (.4f + .6f * MathF.Sqrt(p));
        Color color = VanguardSpearRules.Tint(kind), accent = VanguardSpearVfx.Accent(kind);
        float rotation = Projectile.ai[2];
        Vector2 center = Projectile.Center;
        using (new ChargedOperatorVfx.Pass(false))
        {
            // Dark, moving pressure pocket separates the luminous geometry from a bright background.
            ChargedOperatorVfx.Sprite("ImpactPetals", center, new Vector2(radius * 2.35f),
                Color.Lerp(new Color(29, 19, 39), color, .23f) * (.6f * fade), rotation - p * .35f);
            string mask = kind == VanguardSpearKind.Grani ? "CrystalVortex" : kind == VanguardSpearKind.Bagpipe ? "ForwardFlame" : "ImpactPetals";
            ChargedOperatorVfx.Sprite(mask, center, new Vector2(radius * 2), color * (.92f * fade), rotation + p * .6f);
        }
        using (new ChargedOperatorVfx.Pass(true))
        {
            // Initial compression, operator crest, outward colored jets, delayed pressure front,
            // optical fragments and a short hot core have different sizes and ending times.
            float compression = Math.Max(0, 1 - p / .4f);
            ChargedOperatorVfx.Sprite("CrystalVortex", center, new Vector2(radius * 1.7f) * (1 - p * .3f),
                accent * (compression * .85f), rotation - p * 1.2f);
            VanguardSpearVfx.DrawEmblem(center, rotation, radius * .9f, color * (fade * 1.25f), kind, p * 1.5f);
            int jets = kind == VanguardSpearKind.Vigna ? 6 : kind == VanguardSpearKind.Grani ? 5 : kind == VanguardSpearKind.WildMane ? 8 : 10;
            for (int i = 0; i < jets; i++)
            {
                float angle = rotation + i * MathHelper.TwoPi / jets + p * .45f;
                Vector2 radial = angle.ToRotationVector2();
                string jet = kind == VanguardSpearKind.Grani ? "LinearSpark" : kind == VanguardSpearKind.Vigna ? "OrbitSmear" : "ForwardFlame";
                Vector2 size = kind == VanguardSpearKind.Vigna ? new(radius * .75f, radius * .55f) : new(radius * .32f, radius * .95f);
                ChargedOperatorVfx.Sprite(jet, center + radial * radius * .6f, size,
                    (i % 2 == 0 ? color : accent) * (fade * .8f), angle + MathHelper.PiOver2);
                Vector2 ember = center + radial * radius * (1.05f + p * .2f);
                ChargedOperatorVfx.Sprite("StarCore", ember, new Vector2(7 + (i % 3) * 3) * (1 - p * .4f), accent * fade, angle + p);
            }
            float delay = Math.Clamp((p - .18f) / .82f, 0, 1);
            if (p > .18f)
                ChargedOperatorVfx.Sprite("RuptureRing", center,
                    new Vector2(radius * (1.5f + delay * 1.1f), radius * (hit ? 1.9f : .8f)),
                    accent * ((1 - delay) * .85f), rotation + MathHelper.PiOver2);
            ChargedOperatorVfx.Sprite("StarCore", center, new Vector2(radius * .7f) * (1 - p * .6f),
                Color.Lerp(color, Color.White, .2f) * (fade * .95f), rotation);
        }
        return false;
    }
    private void DrawExpandingImpact(VanguardSpearKind kind, float progress)
    {
        Color color = VanguardSpearRules.Tint(kind), accent = VanguardSpearVfx.Accent(kind);
        Vector2 center = Projectile.Center;
        float angle = Projectile.ai[2];
        float spread = 1 - MathF.Pow(1 - progress, 3);
        float radius = (150 + (int)kind * 24) * (.1f + .9f * spread);
        float fade = MathF.Pow(1 - progress, 1.1f);
        float flash = Math.Max(0, 1 - progress / .18f);
        using (new ChargedOperatorVfx.Pass(false))
            ChargedOperatorVfx.Sprite("ImpactPetals", center, new Vector2(62 + (int)kind * 6),
                color * (flash * .6f), angle);
        using (new ChargedOperatorVfx.Pass(true))
        {
            // Only a brief core. The pressure front and detached fragments immediately leave it.
            if (flash > 0)
            {
                VanguardSpearVfx.DrawEmblem(center, angle, 28 + progress * 50, color * flash, kind, progress);
                ChargedOperatorVfx.Sprite("StarCore", center, new Vector2(36) * flash,
                    Color.Lerp(color, Color.White, .15f) * flash, angle);
            }
            ChargedOperatorVfx.Sprite("RuptureRing", center, new Vector2(radius * 2, radius * 1.8f), accent * (fade * .65f), angle);
            int rays = kind == VanguardSpearKind.Grani ? 10 : 14;
            for (int i = 0; i < rays; i++)
            {
                float direction = angle + i * MathHelper.TwoPi / rays;
                Vector2 ray = direction.ToRotationVector2();
                float stagger = .8f + .2f * MathF.Sin(i * 2.3f);
                Vector2 front = center + ray * radius * stagger;
                ChargedOperatorVfx.Sprite("LinearSpark", front, new Vector2(6 + (int)kind, 18 + 30 * (1 - progress)),
                    (i % 2 == 0 ? color : accent) * fade, direction + MathHelper.PiOver2);
                ChargedOperatorVfx.Sprite("StarCore", front + ray * 12, new Vector2(6) * (1 - progress * .5f), accent * fade, direction);
                if (i % 2 == 0)
                    ChargedOperatorVfx.Sprite("OrbitSmear", center + ray * radius * 1.03f, new Vector2(24, 13),
                        color * (fade * .4f), direction + MathHelper.PiOver2);
            }
        }
    }
}
