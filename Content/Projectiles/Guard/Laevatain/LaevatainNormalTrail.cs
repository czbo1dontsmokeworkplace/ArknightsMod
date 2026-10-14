using System;
using System.Collections.Generic;
using ArknightsMod.Content.SwingHelper;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace ArknightsMod.Content.Projectiles.Guard.Laevatain;

public partial class Laevatain_SwingHelper
{
    private readonly record struct TrailPose(Vector2 Pivot, Vector2 Inner, Vector2 Outer, float Angle);
    private readonly List<TrailPose> normalTrail = new(16);

    private void RecordNormalTrail(bool active)
    {
        if (!active) { normalTrail.Clear(); return; }
        normalTrail.Insert(0, new(handlePos, swordEnd - handlePos, swordHead - handlePos, swordRad));
        if (normalTrail.Count > 16) normalTrail.RemoveAt(16);
    }

    // Same circular subdivision principle as FranSpinningDisc: <= .04 rad per segment.
    // Keep frame history separate, so adding vertices never shortens the trail's lifetime.
    private void BuildNormalTrail(Color color)
    {
        trip.Clear();
        for (int i = 0; i < normalTrail.Count - 1; i++)
        {
            TrailPose a = normalTrail[i], b = normalTrail[i + 1];
            float angle = a.Outer.ToRotation();
            // Keep unwrapped swing travel: a fast cut can cross more than PI in one frame.
            float delta = b.Angle - a.Angle;
            int steps = Math.Clamp((int)MathF.Ceiling(MathF.Abs(delta) / .04f), 1, 160);
            for (int j = 0; j <= steps; j++)
            {
                if (i > 0 && j == 0) continue;
                float t = j / (float)steps;
                Vector2 pivot = Vector2.Lerp(a.Pivot, b.Pivot, t) - Main.screenPosition;
                Vector2 outer = (angle + delta * t).ToRotationVector2() * MathHelper.Lerp(a.Outer.Length(), b.Outer.Length(), t);
                float innerAngle = a.Inner.ToRotation();
                Vector2 inner = (innerAngle + delta * t).ToRotationVector2()
                    * MathHelper.Lerp(a.Inner.Length(), b.Inner.Length(), t);
                float u = (i + t) / Math.Max(1f, normalTrail.Count - 1f);
                trip.Add(new Vertex(pivot + inner, new Vector3(u, 0, 0), color));
                trip.Add(new Vertex(pivot + outer, new Vector3(u, 1, 0), color));
            }
        }
    }

    public override void DrawTrip(SwingEffect en, Color color, SpriteBatch sb, TripTex tex = TripTex.Streamline)
        => DrawNormalTrail(en, color, sb, null, tex, BlendState.Additive);

    public override void DrawTrip(SwingEffect en, Color color, SpriteBatch sb, Texture2D tex, BlendState blendState = null)
        => DrawNormalTrail(en, color, sb, tex, TripTex.Streamline, blendState ?? BlendState.Additive);

    private void DrawNormalTrail(SwingEffect effect, Color color, SpriteBatch sb, Texture2D texture, TripTex fallback, BlendState blend)
    {
        BuildNormalTrail(color);
        if (trip.Count < 4) return;
        sb.End();
        GraphicsDevice device = Main.instance.GraphicsDevice;
        Texture oldTexture1 = device.Textures[1];
        SamplerState oldSampler1 = device.SamplerStates[1];
        sb.Begin(SpriteSortMode.Immediate, blend, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        try
        {
            // Prime the ordinary sprite shader's view/projection before the unshaded layer.
            sb.Draw(TextureAssets.MagicPixel.Value, Vector2.Zero, new Rectangle(0, 0, 1, 1), Color.Transparent);
            ApplyShader(effect);
            if (texture != null) device.Textures[0] = texture;
            else TexChance(fallback);
            device.DrawUserPrimitives(PrimitiveType.TriangleStrip, trip.ToArray(), 0, trip.Count - 2);
        }
        finally
        {
            sb.End();
            device.Textures[1] = oldTexture1;
            device.SamplerStates[1] = oldSampler1;
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
}
