using System;
using System.Collections.Generic;
using ArknightsMod.Common.Particle;
using ArknightsMod.Common.Particle.T;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Schwarz;

// Visual only. Activation uses the synchronized skill snapshot carried by the shot.
internal static class SchwarzTerminalFlight
{
    internal static void Emit(Projectile p)
    {
        if (Main.dedServ || ParticleManager.activeParticles.Count >= 1400) return;
        Vector2 aim = p.velocity.SafeNormalize(Vector2.UnitX);
        Vector2 side = aim.RotatedBy(MathHelper.PiOver2);
        float phase = (float)Main.GameUpdateCount * .38f + p.identity;
        for (int sign = -1; sign <= 1; sign += 2)
        {
            Vector2 at = p.Center - aim * 16f + side * sign * (9f + MathF.Sin(phase) * 4f);
            new T_Smear_Particle(at, -aim * Main.rand.NextFloat(2f, 5f) + side * sign * 1.1f,
                Main.rand.Next(10, 17), sign < 0 ? new Color(225, 208, 255) : new Color(137, 59, 235),
                Main.rand.NextFloat(16f, 27f), height: 2.5f, damping: .9f,
                fadeExponent: 1.7f, rotation: aim.ToRotation() + sign * .2f).Spawn();
        }
    }

    internal static void Draw(Projectile p)
    {
        if (Main.dedServ) return;
        // Use actual history, limiting world-space coverage even at high projectile speed.
        var path = new List<Vector2> { p.Center };
        float distance = 0f;
        foreach (Vector2 old in p.oldPos)
        {
            if (old == Vector2.Zero) break;
            Vector2 point = old + p.Size * .5f;
            float span = Vector2.Distance(path[^1], point);
            if (!float.IsFinite(span) || span > 180f) break;
            if (span < .5f) continue;
            if (distance + span >= 230f)
            {
                path.Add(Vector2.Lerp(path[^1], point, (230f - distance) / span));
                break;
            }
            path.Add(point);
            distance += span;
        }
        float time = (float)(Main.GameUpdateCount % 36000) / 60f;
        Effect effect = ModContent.Request<Effect>("ArknightsMod/Assets/Effects/SchwarzTerminalWake",
            AssetRequestMode.ImmediateLoad).Value;
        effect.Parameters["WorldView"].SetValue(Main.GameViewMatrix.TransformationMatrix
            * Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, 0, 1));
        effect.Parameters["FlowTime"].SetValue(time + p.identity * .37f);
        GraphicsDevice device = Main.spriteBatch.GraphicsDevice;
        Main.spriteBatch.End();
        try
        {
            device.BlendState = BlendState.AlphaBlend;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;
            if (path.Count >= 2)
            {
                var vertices = new VertexPositionColorTexture[path.Count * 2];
                for (int strand = -1; strand <= 1; strand++)
                {
                    effect.Parameters["Strand"].SetValue((float)strand);
                    float traveled = 0f;
                    float total = 0f;
                    for (int i = 1; i < path.Count; i++) total += Vector2.Distance(path[i - 1], path[i]);
                    for (int i = 0; i < path.Count; i++)
                    {
                        if (i > 0) traveled += Vector2.Distance(path[i - 1], path[i]);
                        float t = traveled / Math.Max(1f, total);
                        Vector2 tangent = (path[Math.Max(0, i - 1)] - path[Math.Min(path.Count - 1, i + 1)])
                            .SafeNormalize(Vector2.UnitX);
                        Vector2 normal = tangent.RotatedBy(MathHelper.PiOver2);
                        float taper = MathF.Pow(1f - t, .65f);
                        float width = (strand == 0 ? 18f : 4.5f) * taper + .2f;
                        float offset = strand * (12f + MathF.Sin(t * 12f - time * 13f + p.identity) * 6f) * taper;
                        Vector2 at = path[i] + normal * offset - Main.screenPosition;
                        vertices[i * 2] = new(new Vector3(at + normal * width, 0), Color.White, new(t, 0));
                        vertices[i * 2 + 1] = new(new Vector3(at - normal * width, 0), Color.White, new(t, 1));
                    }
                    effect.CurrentTechnique.Passes[0].Apply();
                    device.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices, 0, vertices.Length - 2);
                }
            }
        }
        finally
        {
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }
        // An opaque spearhead remains legible against both bright sky and the glowing wake.
        Texture2D arrow = ModContent.Request<Texture2D>("ArknightsMod/Content/Projectiles/Sniper/Schwarz/SchwarzArrow").Value;
        Vector2 center = p.Center - Main.screenPosition;
        float rotation = p.velocity.ToRotation() - MathHelper.PiOver2;
        Main.spriteBatch.Draw(arrow, center, null, new Color(178, 105, 255, 0), rotation,
            arrow.Size() * .5f, new Vector2(18f, 66f) / arrow.Size(), SpriteEffects.None, 0);
        Main.spriteBatch.Draw(arrow, center, null, new Color(6, 3, 12), rotation,
            arrow.Size() * .5f, new Vector2(12f, 58f) / arrow.Size(), SpriteEffects.None, 0);
    }
}
