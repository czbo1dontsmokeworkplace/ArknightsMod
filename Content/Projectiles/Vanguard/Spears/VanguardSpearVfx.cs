using System;
using System.Collections.Generic;
using ArknightsMod.Common.Particle;
using ArknightsMod.Common.Particle.T;
using ArknightsMod.Content.Items.Weapons.Vanguard;
using ArknightsMod.Content.Projectiles.Charged;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Vanguard.Spears;

internal static class VanguardSpearVfx
{
    private static MiscShaderData shader;
    private static readonly Dictionary<string, Texture2D> spearArt = new();
    internal static bool Visible(Vector2 point) => ChargedOperatorVfx.Visible(point);
    internal static void Unload()
    {
        Texture2D[] old = new Texture2D[spearArt.Count]; spearArt.Values.CopyTo(old, 0); spearArt.Clear(); shader = null;
        Main.QueueMainThreadAction(() => { foreach (Texture2D texture in old) texture.Dispose(); });
    }
    private static Texture2D Art(string path)
    {
        if (spearArt.TryGetValue(path, out Texture2D texture)) return texture;
        Texture2D source = ModContent.Request<Texture2D>(path).Value;
        Color[] data = new Color[source.Width * source.Height]; source.GetData(data);
        // The three source spear PNGs are inspected offline too. Normalize the loaded copy
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
    internal static void DrawSpear(Vector2 grip, Vector2 tip, VanguardSpearKind kind, Color light, bool accent)
    {
        if (!Visible(grip) || !float.IsFinite(tip.X) || !float.IsFinite(tip.Y)) return;
        string path = kind switch {
            VanguardSpearKind.Grani => "ArknightsMod/Content/Items/Weapons/Vanguard/Fang/FangSpear",
            VanguardSpearKind.Bagpipe => "ArknightsMod/Content/Projectiles/Vanguard/Bagpipe/BagpipeProj",
            _ => "ArknightsMod/Content/Items/Weapons/Guard/Hongdou/HongdouLance_protile"
        };
        Texture2D texture = Art(path);
        // Measured opaque endpoints, not PNG rectangle corners (Bagpipe has transparent padding).
        Vector2 tail = kind switch { VanguardSpearKind.Grani => new(0, 55), VanguardSpearKind.Bagpipe => new(0, 91), _ => new(0, 15) };
        Vector2 head = kind switch { VanguardSpearKind.Grani => new(55, 0), VanguardSpearKind.Bagpipe => new(81, 12), _ => new(111, 15) };
        Vector2 sourceGrip = Vector2.Lerp(tail, head, .22f);
        Vector2 localBlade = head - sourceGrip;
        Vector2 blade = tip - grip;
        float scale = Math.Clamp(blade.Length() / localBlade.Length(), .15f, 4);
        float rotation = blade.ToRotation() - localBlade.ToRotation();
        Color tint = VanguardSpearRules.Tint(kind);
        using (new ChargedOperatorVfx.Pass(false))
            Main.EntitySpriteDraw(texture, grip - Main.screenPosition, null, Color.Lerp(light, Color.White, .32f),
                rotation, sourceGrip, scale, SpriteEffects.None);
        using (new ChargedOperatorVfx.Pass(true))
        {
            // Keep the highlight on the actual head, not a white line along the entire shaft.
            ChargedOperatorVfx.Sprite("StarCore", tip, new Vector2(accent ? 24 : 15), tint * .7f, rotation);
            if (kind == VanguardSpearKind.WildMane)
                ChargedOperatorVfx.Sprite("LinearSpark", Vector2.Lerp(grip, tip, .7f), new Vector2(9, 28), tint * .6f, blade.ToRotation() + MathHelper.PiOver2);
        }
    }
    internal static void Ribbon(Vector2 head, Vector2[] history, VanguardSpearKind kind, float width)
    {
        if (!Visible(head)) return;
        List<Vector2> path = new() { head }; float length = 0;
        foreach (Vector2 point in history)
        {
            if (point == Vector2.Zero) break;
            float distance = Vector2.Distance(point, path[^1]);
            if (!float.IsFinite(distance) || distance > 96 || length + distance > 250) break;
            if (distance < .5f) continue;
            length += distance; path.Add(point);
        }
        if (path.Count < 3) return;
        Vector2[] positions = path.ToArray(); float[] rotations = new float[path.Count];
        for (int i = 0; i < rotations.Length; i++) rotations[i] = (positions[Math.Max(0, i - 1)] - positions[Math.Min(positions.Length - 1, i + 1)]).ToRotation();
        Color color = VanguardSpearRules.Tint(kind);
        Main.spriteBatch.End();
        try
        {
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
#pragma warning disable CS0618
            shader ??= new MiscShaderData(Main.VertexPixelShaderRef, "MagicMissile").UseProjectionMatrix(true)
                .UseImage0("Images/Extra_191").UseImage1("Images/Extra_189").UseImage2("Images/Extra_190");
#pragma warning restore CS0618
            shader.UseColor(color).UseSecondaryColor(Color.Lerp(color, Color.White, .12f)).UseOpacity(.58f).UseSaturation(0).Apply();
            VertexStrip strip = new();
            strip.PrepareStrip(positions, rotations, t => color * (1 - t), t => Math.Clamp(width, 2, 20) * (1 - t), -Main.screenPosition);
            strip.DrawTrail();
        }
        finally
        {
            Main.pixelShader.CurrentTechnique.Passes[0].Apply(); Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
    internal static void FlightRibbon(Projectile projectile, VanguardSpearKind kind)
        => ChargedOperatorVfx.Trail(projectile, VanguardSpearRules.Tint(kind), kind == VanguardSpearKind.Grani ? 4 : 7);
    private static void Fragment(Vector2 point, Vector2 velocity, VanguardSpearKind kind, int life = 20)
    {
        if (!Visible(point) || ParticleManager.activeParticles.Count >= 1200) return;
        new T_Vanguard_Fragment_Particle(point, velocity, kind, life).Spawn();
    }
    internal static void Sweep(Vector2 tip, Vector2 direction, VanguardSpearKind kind, int stage)
    {
        if (!Visible(tip)) return;
        Vector2 tangent = direction.RotatedBy((stage == 0 ? 1 : -1) * MathHelper.PiOver2);
        Fragment(tip, tangent * 1.4f - direction * .4f, kind, 14);
        if (kind == VanguardSpearKind.Grani)
            ChargedOperatorVfx.Sparkle(tip, -direction * .3f, VanguardSpearRules.Tint(kind), direction.ToRotation(), .38f);
    }
    internal static void Throw(Vector2 hand, Vector2 direction, VanguardSpearKind kind)
    {
        if (!Visible(hand)) return;
        for (int i = -1; i <= 1; i++) Fragment(hand, -direction.RotatedBy(i * .3f) * (2 + Math.Abs(i)), kind, 16);
    }
    internal static void Flight(Vector2 point, Vector2 direction, VanguardSpearKind kind, int tick, bool child)
    {
        if (!Visible(point)) return;
        Color color = VanguardSpearRules.Tint(kind);
        Lighting.AddLight(point, color.ToVector3() * .3f);
        if (tick % (child ? 4 : 2) == 0)
            Fragment(point - direction * 12, -direction * 1.4f, kind, 14);
        if (kind == VanguardSpearKind.Grani && tick % 5 == 0)
        {
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2) * 9;
            ChargedOperatorVfx.Sparkle(point + normal, -normal * .04f, color, direction.ToRotation(), .45f);
            ChargedOperatorVfx.Sparkle(point - normal, normal * .04f, color, direction.ToRotation(), .45f);
        }
        if (kind == VanguardSpearKind.Bagpipe && tick % 6 == 0)
        {
            Dust dust = Dust.NewDustPerfect(point - direction * 15, DustID.Smoke, -direction * .4f, 145, new Color(100, 85, 75), .65f);
            dust.noGravity = true;
        }
    }
    internal static void Impact(Vector2 point, Vector2 direction, VanguardSpearKind kind, bool strong)
    {
        if (!Visible(point)) return;
        int count = strong ? 12 : 6;
        Color color = VanguardSpearRules.Tint(kind);
        for (int i = 0; i < count; i++)
        {
            float angle = MathHelper.TwoPi * i / count;
            Vector2 ray = angle.ToRotationVector2();
            Fragment(point, ray * Main.rand.NextFloat(1.8f, strong ? 4.4f : 3), kind, 16 + i % 10);
            if (i % 3 == 0) ChargedOperatorVfx.Sparkle(point + ray * (strong ? 16 : 8), ray * .5f, color, angle, strong ? .8f : .5f);
        }
        for (int i = 0; i < (strong ? 3 : 1); i++)
        {
            Dust dust = Dust.NewDustPerfect(point, DustID.TintableDustLighted, direction.RotatedByRandom(.6f) * 2, 90, color, .65f);
            dust.noGravity = true;
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
    public override void Unload() => VanguardSpearVfx.Unload();
}
