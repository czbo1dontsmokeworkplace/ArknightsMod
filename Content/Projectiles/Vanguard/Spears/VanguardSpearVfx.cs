using System;
using System.Collections.Generic;
using ArknightsMod.Common.Particle;
using ArknightsMod.Common.Particle.T;
using ArknightsMod.Content.Items.Weapons.Vanguard;
using ArknightsMod.Content.Projectiles.Charged;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Vanguard.Spears;

internal static class VanguardSpearVfx
{
    private static BasicEffect shader;
    private static readonly Dictionary<string, Texture2D> spearArt = new();
    internal static bool Visible(Vector2 point) => ChargedOperatorVfx.Visible(point);
    internal static Texture2D SmokeTexture => Art("ArknightsMod/Common/Particle/DefaultParticle");
    internal static void Unload()
    {
        Texture2D[] old = new Texture2D[spearArt.Count]; spearArt.Values.CopyTo(old, 0); spearArt.Clear();
        BasicEffect oldShader = shader; shader = null;
        Main.QueueMainThreadAction(() => { oldShader?.Dispose(); });
        Main.QueueMainThreadAction(() => { foreach (Texture2D texture in old) texture.Dispose(); });
    }
    private static Texture2D Art(string path)
    {
        if (spearArt.TryGetValue(path, out Texture2D texture)) return texture;
        // Opaque weapon art stays in the native asset lifecycle. Only soft smoke needs
        // a private straight-alpha copy; never read back and cache a second spear body.
        if (path != "ArknightsMod/Common/Particle/DefaultParticle")
            return ModContent.Request<Texture2D>(path, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        Texture2D source = ModContent.Request<Texture2D>(path).Value;
        Color[] data = new Color[source.Width * source.Height]; source.GetData(data);
        // The source spear PNGs are inspected offline too. Normalize the loaded copy
        // to straight RGBA and use NonPremultiplied for its body, true Additive for glow.
        bool straight = false;
        foreach (Color c in data) if (c.A > 0 && c.A < 255 && Math.Max(c.R, Math.Max(c.G, c.B)) > c.A + 4) { straight = true; break; }
        if (!straight)
            for (int i = 0; i < data.Length; i++)
            {
                Color c = data[i];
                data[i] = c.A == 0 ? Color.Transparent : new Color(Math.Min(255, c.R * 255 / c.A),
                    Math.Min(255, c.G * 255 / c.A), Math.Min(255, c.B * 255 / c.A), c.A);
            }
        texture = new Texture2D(Main.graphics.GraphicsDevice, source.Width, source.Height);
        texture.SetData(data); spearArt.Add(path, texture); return texture;
    }
    internal static void DrawSpear(Vector2 grip, Vector2 tip, VanguardSpearKind kind, Color light, bool accent, bool charging = false, float age = 0)
    {
        if (!Visible(grip) || !float.IsFinite(tip.X) || !float.IsFinite(tip.Y)) return;
        string path = kind switch {
            VanguardSpearKind.Vigna => "ArknightsMod/Content/Projectiles/Vanguard/Spears/VignaLongLance",
            VanguardSpearKind.Grani => "ArknightsMod/Content/Items/Weapons/Vanguard/Fang/FangSpear",
            VanguardSpearKind.Bagpipe => "ArknightsMod/Content/Projectiles/Vanguard/Bagpipe/BagpipeProj",
            _ => "ArknightsMod/Content/Items/Weapons/Guard/Hongdou/HongdouLance"
        };
        Texture2D texture = Art(path);
        // Measured opaque endpoints, not PNG rectangle corners (Bagpipe has transparent padding).
        Vector2 tail = kind switch { VanguardSpearKind.Vigna => new(1, 102), VanguardSpearKind.Grani => new(0, 55), VanguardSpearKind.Bagpipe => new(0, 91), _ => new(1, 81) };
        Vector2 head = kind switch { VanguardSpearKind.Vigna => new(98, 1), VanguardSpearKind.Grani => new(55, 0), VanguardSpearKind.Bagpipe => new(81, 12), _ => new(79, 2) };
        Vector2 sourceGrip = Vector2.Lerp(tail, head, .22f);
        Vector2 localBlade = head - sourceGrip;
        Vector2 blade = tip - grip;
        float scale = Math.Clamp(blade.Length() / localBlade.Length(), .15f, 4);
        float rotation = blade.ToRotation() - localBlade.ToRotation();
        Color tint = VanguardSpearRules.Tint(kind);
        // Submit the body directly within the held projectile's own render pass.
        using (new ChargedOperatorVfx.Pass(false))
            Main.spriteBatch.Draw(texture, grip - Main.screenPosition, null, Color.Lerp(light, Color.White, .32f),
                rotation, sourceGrip, scale, SpriteEffects.None, 0f);
        if (charging)
        {
            DrawSpearRim(texture, path, grip, sourceGrip, scale, rotation, kind, age);
            using (new ChargedOperatorVfx.Pass(true))
                ChargedOperatorVfx.Sprite("StarCore", tip, new Vector2(11, 8), tint * .65f, blade.ToRotation());
        }
        else using (new ChargedOperatorVfx.Pass(true))
        {
            ChargedOperatorVfx.Sprite("StarCore", tip, new Vector2(accent ? 24 : 15), tint * .7f, rotation);
            if (kind == VanguardSpearKind.WildMane)
                ChargedOperatorVfx.Sprite("LinearSpark", Vector2.Lerp(grip, tip, .7f), new Vector2(9, 28), tint * .6f, blade.ToRotation() + MathHelper.PiOver2);
        }
    }
    private static void DrawSpearRim(Texture2D source, string key, Vector2 grip, Vector2 sourceGrip,
        float scale, float rotation, VanguardSpearKind kind, float age)
    {
        key += "#rim";
        const int pad = 2;
        if (!spearArt.TryGetValue(key, out Texture2D rim))
        {
            int w = source.Width + pad * 2, h = source.Height + pad * 2;
            var body = new Color[source.Width * source.Height]; source.GetData(body);
            var edge = new Color[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                int sx = x - pad, sy = y - pad, center = 0, outer = 0;
                if (sx >= 0 && sy >= 0 && sx < source.Width && sy < source.Height) center = body[sy * source.Width + sx].A;
                for (int dy = -pad; dy <= pad; dy++) for (int dx = -pad; dx <= pad; dx++)
                {
                    if (dx * dx + dy * dy > pad * pad) continue;
                    int nx = sx + dx, ny = sy + dy;
                    if (nx >= 0 && ny >= 0 && nx < source.Width && ny < source.Height)
                        outer = Math.Max(outer, body[ny * source.Width + nx].A);
                }
                edge[y * w + x] = new Color(255, 255, 255, Math.Max(0, outer - center));
            }
            rim = new Texture2D(Main.graphics.GraphicsDevice, w, h); rim.SetData(edge); spearArt.Add(key, rim);
        }
        // Actual alpha silhouette, expanded by only two source texels. Animated vertex colors
        // and the texture shader flow across this narrow outline, never filling a flame column.
        var vertices = new VertexPositionColorTexture[18];
        for (int i = 0; i <= 8; i++) for (int side = 0; side <= 1; side++)
        {
            float u = i / 8f;
            Vector2 local = new Vector2(u * rim.Width, side * rim.Height) - sourceGrip - new Vector2(pad);
            Vector2 point = grip + (local * scale).RotatedBy(rotation) - Main.screenPosition;
            float flow = .5f + .5f * MathF.Sin(u * 15 - age * .9f);
            Color tint = Color.Lerp(VanguardSpearRules.Tint(kind), Accent(kind), flow) * (.35f + .35f * flow);
            vertices[i * 2 + side] = new(new Vector3(point, 0), tint, new(u, side));
        }
        DrawGeometry(vertices, rim, false);
    }
    private static void DrawGeometry(VertexPositionColorTexture[] vertices, Texture2D texture, bool wrap)
    {
        GraphicsDevice device = Main.instance.GraphicsDevice;
        shader ??= new BasicEffect(device) { TextureEnabled = true, VertexColorEnabled = true };
        Main.spriteBatch.End();
        try
        {
            device.BlendState = BlendState.Additive;
            device.SamplerStates[0] = wrap ? SamplerState.LinearWrap : SamplerState.LinearClamp;
            device.DepthStencilState = DepthStencilState.None; device.RasterizerState = RasterizerState.CullNone;
            shader.World = Matrix.Identity; shader.View = Main.GameViewMatrix.TransformationMatrix;
            shader.Projection = Matrix.CreateOrthographicOffCenter(0, device.Viewport.Width, device.Viewport.Height, 0, -1, 1);
            shader.Texture = texture;
            foreach (EffectPass pass in shader.CurrentTechnique.Passes)
            {
                pass.Apply(); device.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices, 0, vertices.Length - 2);
            }
        }
        finally
        {
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
    internal static Color Accent(VanguardSpearKind kind) => kind switch {
        VanguardSpearKind.Vigna => new(174, 75, 224), VanguardSpearKind.Grani => new(168, 244, 232),
        VanguardSpearKind.WildMane => new(255, 203, 88), _ => new(255, 107, 48)
    };
    private static void Atmosphere(Vector2 point, Vector2 velocity, VanguardSpearKind kind, int mode, float radius, int life)
    {
        if (Visible(point) && ParticleManager.activeParticles.Count < 1400)
            new T_Vanguard_Atmosphere_Particle(point, velocity, kind, mode, radius, life).Spawn();
    }
    private static void Motif(Vector2 point, Vector2 velocity, VanguardSpearKind kind, float size, int life)
    {
        if (Visible(point) && ParticleManager.activeParticles.Count < 1400)
            new T_Vanguard_Motif_Particle(point, velocity, kind, size, life).Spawn();
    }
    internal static void Spark(Vector2 point, Vector2 velocity, VanguardSpearKind kind, int age)
    {
        if (!Visible(point)) return;
        Color tint = VanguardSpearRules.Tint(kind);
        if (age % 2 == 0 && ParticleManager.activeParticles.Count < 1400)
            new T_Vanguard_HitFlare(point, -velocity * .08f, tint, Main.rand.NextFloat(7, 12), 10).Spawn();
        if (age % 3 == 0) Motif(point, -velocity * .12f, kind, 5, 10);
        if (age % 4 == 0)
            ChargedOperatorVfx.Sparkle(point, -velocity * .15f, VanguardSpearRules.Tint(kind), velocity.ToRotation(), .32f);
        if (age % 3 == 0)
        {
            Dust d = Dust.NewDustPerfect(point, DustID.TintableDustLighted, -velocity * .12f, 110, VanguardSpearRules.Tint(kind), .55f);
            d.noGravity = true;
        }
    }
    internal static IEnumerable<global::ArknightsMod.Common.Particle.Particle> HitParticles(Vector2 point, Vector2 direction, VanguardSpearKind kind, bool strong)
    {
        direction = direction.SafeNormalize(Vector2.UnitX);
        Color color = VanguardSpearRules.Tint(kind);
        yield return new T_Vanguard_HitFlare(point, direction * .4f, color, strong ? 44 : 26, strong ? 12 : 8, true);
        int count = strong ? 12 : 5;
        for (int i = 0; i < count; i++)
        {
            // A forward fan with a few backward ricochets, rather than a uniform circle.
            Vector2 speed = direction.RotatedBy(Main.rand.NextFloat(-.85f, .85f))
                * Main.rand.NextFloat(strong ? 4 : 2, strong ? 10 : 6);
            if (i % 4 == 0) speed *= -.6f;
            yield return new T_Vanguard_HitFlare(point, speed, i % 2 == 0 ? color : Accent(kind),
                Main.rand.NextFloat(8, strong ? 22 : 14), Main.rand.Next(10, 19));
        }
    }
    // Kept separate from registration so the exact moving burst can also be raster-checked offline.
    internal static IEnumerable<global::ArknightsMod.Common.Particle.Particle> BurstParticles(Vector2 point, Vector2 aim, VanguardSpearKind kind, bool hit)
    {
        int count = hit ? (22 + (int)kind * 2) * 3 : 36;
        for (int i = 0; i < count; i++)
        {
            float a = MathHelper.TwoPi * (i + Main.rand.NextFloat(-.35f, .35f)) / count;
            Vector2 ray = a.ToRotationVector2();
            Vector2 speed = ray * (hit ? Main.rand.NextFloat(9, 18) : Main.rand.NextFloat(3, 6)) + aim * (hit ? 1.2f : -2.6f);
            yield return new T_Vanguard_Motif_Particle(point + ray * (hit ? 10 : 4), speed, kind,
                hit ? Main.rand.NextFloat(6, 13) : 8, hit ? 18 + i % 10 : 20 + i % 12);
            if (i % 3 == 0) yield return new T_Vanguard_Atmosphere_Particle(point + ray * 16, speed * .75f, kind, 2, 18 + i % 18, 18 + i % 10);
            if (i % 6 == 0) yield return new T_Vanguard_Atmosphere_Particle(point + ray * 24, speed * .6f, kind, 1, hit ? 24 : 18, 24);
            if (i % 9 == 0) yield return new T_Vanguard_Atmosphere_Particle(point + ray * 16, speed * .5f, kind, 0, hit ? 28 : 22, 28);
        }
    }
    internal static void SignatureBurst(Vector2 point, Vector2 aim, VanguardSpearKind kind, bool hit)
    {
        if (!Visible(point)) return;
        foreach (var particle in BurstParticles(point, aim, kind, hit))
        {
            if (ParticleManager.activeParticles.Count >= 1400) break;
            particle.Spawn();
        }
        int count = hit ? (22 + (int)kind * 2) * 3 : 36;
        for (int i = 0; i < count; i++)
        {
            float a = MathHelper.TwoPi * (i + Main.rand.NextFloat(-.3f, .3f)) / count;
            Vector2 ray = a.ToRotationVector2();
            Vector2 speed = ray * (hit ? Main.rand.NextFloat(9, 18) : Main.rand.NextFloat(3, 6));
            if (i % 4 == 0)
                ChargedOperatorVfx.Sparkle(point + ray * (hit ? 26 : 10), speed * .6f, VanguardSpearRules.Tint(kind), a, hit ? .75f : .45f);
            if (i % 5 == 0)
            {
                Dust d = Dust.NewDustPerfect(point, kind == VanguardSpearKind.Bagpipe ? DustID.Smoke : DustID.TintableDustLighted,
                    speed * .65f, 120, VanguardSpearRules.Tint(kind), .85f);
                d.noGravity = kind == VanguardSpearKind.Grani;
            }
        }
    }
    internal static void DrawEmblem(Vector2 center, float rotation, float radius, Color color, VanguardSpearKind kind, float phase)
    {
        radius = Math.Clamp(radius, 6, 64);
        Vector2 aim = rotation.ToRotationVector2(), side = aim.RotatedBy(MathHelper.PiOver2);
        if (kind == VanguardSpearKind.Grani)
        {
            // Shield-shaped five-point perimeter, breaking into five crystal corners.
            Vector2[] shield = { new(-.5f, -.8f), new(.5f, -.8f), new(.65f, .2f), new(0, 1), new(-.65f, .2f) };
            for (int i = 0; i < shield.Length; i++)
            {
                Vector2 a = center + (aim * shield[i].X + side * shield[i].Y) * radius;
                Vector2 b = center + (aim * shield[(i + 1) % 5].X + side * shield[(i + 1) % 5].Y) * radius;
                ChargedOperatorVfx.Line(a, b, color * .7f, 1.6f);
                ChargedOperatorVfx.Sprite("StarCore", a, new Vector2(9), color, phase);
            }
            return;
        }
        int lobes = kind == VanguardSpearKind.Vigna ? 3 : kind == VanguardSpearKind.WildMane ? 4 : 6;
        for (int i = 0; i < lobes; i++)
        {
            float a = rotation + i * MathHelper.TwoPi / lobes + phase * (kind == VanguardSpearKind.WildMane ? 1.4f : .3f);
            Vector2 ray = a.ToRotationVector2();
            string mask = kind == VanguardSpearKind.Vigna ? "OrbitSmear" : kind == VanguardSpearKind.WildMane ? "ForwardFlame" : "LinearSpark";
            Vector2 size = kind == VanguardSpearKind.Vigna ? new(radius, radius * .7f) : new(radius * .45f, radius * 1.1f);
            ChargedOperatorVfx.Sprite(mask, center + ray * radius * .6f, size, color * .65f, a + MathHelper.PiOver2);
        }
    }
    private static void Fragment(Vector2 point, Vector2 velocity, VanguardSpearKind kind, int life = 20)
    {
        if (!Visible(point) || ParticleManager.activeParticles.Count >= 1200) return;
        new T_Vanguard_Fragment_Particle(point, velocity, kind, life).Spawn();
    }
    internal static void ChargeStart(Vector2 hand, Vector2 direction, VanguardSpearKind kind)
    {
        if (!Visible(hand)) return;
        for (int i = -2; i <= 2; i++) Motif(hand, -direction.RotatedBy(i * .22f) * (3 + Math.Abs(i)), kind, 8, 20);
    }
    internal static void ChargeWake(Vector2 point, Vector2 direction, VanguardSpearKind kind, int tick)
    {
        if (!Visible(point)) return;
        Color color = VanguardSpearRules.Tint(kind);
        Vector2 side = direction.RotatedBy(MathHelper.PiOver2);
        Lighting.AddLight(point, color.ToVector3() * .55f);
        // The wake opens to both sides rather than coating the spear in a flame sprite.
        float phase = tick * .62f;
        for (int sign = -1; sign <= 1; sign += 2)
        {
            float offset = 10 + Main.rand.NextFloat(0, 8);
            Vector2 wing = side * sign;
            Motif(point - direction * 12 + wing * offset,
                -direction * Main.rand.NextFloat(1.2f, 2.8f) + wing * Main.rand.NextFloat(4.5f, 8), kind, 6 + (int)kind, 20);
            if (ParticleManager.activeParticles.Count < 1400)
                new T_Vanguard_Helix_Particle(point, direction, kind, phase + (sign < 0 ? MathHelper.Pi : 0)).Spawn();
            if (tick % 2 == 0)
                Atmosphere(point - direction * 20 + wing * 12, -direction * .8f + wing * 4,
                    kind, 2, 14 + tick % 10, 18);
        }
        if (tick % 3 == 0)
            Atmosphere(point - direction * 24, -direction * 1.2f, kind, 0, 14, 18);
        if (tick % 2 == 0)
        {
            float offset = MathF.Sin(phase) * (26 + (int)kind * 2);
            ChargedOperatorVfx.Sparkle(point + side * offset, side * MathF.Cos(phase) * 2.2f - direction * .6f,
                Accent(kind), direction.ToRotation() + .4f, .5f);
        }
        if (tick % 3 == 0)
        {
            Dust dust = Dust.NewDustPerfect(point - direction * 12, DustID.TintableDustLighted,
                -direction + side * Main.rand.NextFloat(-4, 4), 120, color, .7f);
            dust.noGravity = true;
        }
    }
    internal static void Impact(Vector2 point, Vector2 direction, VanguardSpearKind kind, bool strong)
    {
        if (!Visible(point)) return;
        foreach (var particle in HitParticles(point, direction, kind, strong))
        {
            if (ParticleManager.activeParticles.Count >= 1400) break;
            particle.Spawn();
        }
        Lighting.AddLight(point, VanguardSpearRules.Tint(kind).ToVector3() * (strong ? .65f : .3f));
        int count = strong ? 36 : 6;
        Color color = VanguardSpearRules.Tint(kind);
        for (int i = 0; i < count; i++)
        {
            float angle = MathHelper.TwoPi * i / count;
            Vector2 ray = angle.ToRotationVector2();
            Motif(point, ray * (strong ? Main.rand.NextFloat(8, 14) : Main.rand.NextFloat(1.8f, 3)), kind, strong ? 10 : 5, 16 + i % 10);
            if (i % 3 == 0) ChargedOperatorVfx.Sparkle(point + ray * (strong ? 16 : 8), ray * .5f, color, angle, strong ? .8f : .5f);
        }
        for (int i = 0; i < (strong ? 10 : 4); i++)
        {
            Dust dust = Dust.NewDustPerfect(point, DustID.TintableDustLighted,
                direction.RotatedByRandom(1.1f) * Main.rand.NextFloat(1.5f, strong ? 7 : 4), 90, color, Main.rand.NextFloat(.5f, .9f));
            dust.noGravity = true;
            if (i % 3 == 0)
            {
                Dust debris = Dust.NewDustPerfect(point, DustID.Smoke, Main.rand.NextVector2Circular(2, 2),
                    170, Color.Lerp(color, Color.Gray, .65f), .45f);
                debris.noGravity = false;
            }
        }
    }
    internal static void Fizzle(Vector2 point, VanguardSpearKind kind)
    { for (int i = 0; i < 3; i++) Fragment(point, Main.rand.NextVector2Circular(1.5f, 1.5f), kind, 12); }
    internal static void Ready(Player player, VanguardSpearKind kind)
    {
        if (!Visible(player.Center)) return;
        Vector2 head = player.MountedCenter - new Vector2(0, 40 * player.gravDir);
        for (int i = -1; i <= 1; i++)
            ChargedOperatorVfx.Sparkle(head + new Vector2(i * 12, 0), new Vector2(0, -.3f * player.gravDir), VanguardSpearRules.Tint(kind), MathHelper.PiOver2, .45f);
    }
}

public sealed class VanguardSpearVfxSystem : ModSystem
{
    public override void Unload() { VanguardSpearVfx.Unload(); VanguardChargeTrail.Unload(); }
}
