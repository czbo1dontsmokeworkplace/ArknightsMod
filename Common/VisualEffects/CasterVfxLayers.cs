using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics;
using Terraria.Graphics.Renderers;
using Terraria.Graphics.Shaders;

namespace ArknightsMod.Common.VisualEffects;

/// <summary>原版 PrettySparkleParticle 与本模组 Particle 分开注册。</summary>
internal static class CasterSparkles
{
    private static readonly ParticlePool<PrettySparkleParticle> Pool = new(256, () => new PrettySparkleParticle());

    internal static void Spawn(Vector2 position, Vector2 velocity, Color tint, float size,
        int lifetime = 16, float rotation = 0f)
    {
        if (Main.dedServ || Main.gameMenu || !float.IsFinite(position.X) || !float.IsFinite(position.Y)
            || position.X < Main.screenPosition.X - 120f
            || position.X > Main.screenPosition.X + Main.screenWidth + 120f
            || position.Y < Main.screenPosition.Y - 120f
            || position.Y > Main.screenPosition.Y + Main.screenHeight + 120f)
            return;
        lifetime = Math.Clamp(lifetime, 4, 36);
        size = MathHelper.Clamp(size, 0.35f, 1.8f);
        PrettySparkleParticle sparkle = Pool.RequestParticle();
        sparkle.LocalPosition = position;
        sparkle.Velocity = velocity;
        sparkle.ColorTint = tint;
        sparkle.Rotation = rotation;
        sparkle.Scale = new Vector2(size, size * .55f);
        sparkle.TimeToLive = lifetime;
        sparkle.FadeInNormalizedTime = .06f;
        sparkle.FadeOutNormalizedTime = .72f;
        sparkle.FadeInEnd = 2;
        sparkle.FadeOutStart = Math.Max(2, lifetime / 3);
        sparkle.FadeOutEnd = lifetime;
        sparkle.AdditiveAmount = .6f;
        Main.ParticleSystem_World_OverPlayers.Add(sparkle);
    }
}

/// <summary>仅沿弹幕保存的历史位置绘制着色器拖尾，长度由缓存点数限定。</summary>
internal static class CasterShaderTrail
{
    internal static void Draw(Projectile projectile, Color body, Color edge, float width)
    {
        if (Main.dedServ || projectile.oldPos.Length < 3)
            return;
        int count = 0;
        while (count < projectile.oldPos.Length && projectile.oldPos[count] != Vector2.Zero)
            count++;
        if (count < 3)
            return;

        Vector2[] positions = new Vector2[count];
        float[] rotations = new float[count];
        for (int i = 0; i < count; i++)
        {
            positions[i] = projectile.oldPos[i];
            rotations[i] = projectile.oldRot[i] - MathHelper.PiOver2;
        }

        Main.spriteBatch.End();
        try
        {
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null,
                Main.GameViewMatrix.TransformationMatrix);
            MiscShaderData shader = GameShaders.Misc["FlameLash"];
            shader.UseImage0("Images/Extra_194");
            shader.UseColor(body);
            shader.UseSecondaryColor(edge);
            shader.UseOpacity(.75f);
            shader.UseSaturation(0f);
            shader.Apply();
            VertexStrip strip = new();
            strip.PrepareStrip(positions, rotations,
                progress => Color.Lerp(edge, body, progress) * MathF.Pow(1f - progress, 1.5f),
                progress => width * MathF.Pow(1f - progress, .75f),
                -Main.screenPosition + projectile.Size * .5f);
            strip.DrawTrail();
        }
        finally
        {
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null,
                Main.GameViewMatrix.TransformationMatrix);
        }
    }
}
