using System;
using ArknightsMod.Common.Particle;
using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Projectiles.Caster.Pudding;

namespace ArknightsMod.Common.Particle.T;

public sealed class T_Electric_Link_Particle : global::ArknightsMod.Common.Particle.Particle
{
    private readonly Vector2 end;
    private readonly float intensity;
    private readonly int seed;
    public override string TexturePath => "ArknightsMod/Content/Textures/spark_07";
    public override BlendState DrawBlendState => BlendState.Additive;

    internal T_Electric_Link_Particle(Vector2 start, Vector2 end, float intensity, int seed)
    {
        Position = start;
        this.end = end;
        this.intensity = MathHelper.Clamp(intensity, .2f, 1.4f);
        this.seed = seed;
        Lifetime = 12;
    }

    public override void Draw()
    {
        Vector2 difference = end - Position;
        float distance = difference.Length();
        if (distance < 2f || distance > 1500f) return;
        Vector2 normal = difference.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
        int segments = Math.Clamp((int)MathF.Ceiling(distance / 25f), 2, 36);
        float fade = MathF.Pow(1f - LifetimeRatio, 1.3f);
        Vector2 previous = Position;
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            // 两端扰动归零；每段严格落在真实传导路径附近。
            float wobble = MathF.Sin(t * MathHelper.Pi) *
                (MathF.Sin(t * 17f + seed * .019f + Time * .5f) * 5f
                + MathF.Sin(t * 37f + seed * .043f) * 2.5f) * intensity;
            Vector2 next = Vector2.Lerp(Position, end, t) + normal * wobble;
            Vector2 segment = next - previous;
            float length = segment.Length();
            if (length > .1f)
            {
                Vector2 midpoint = (previous + next) * .5f - Main.screenPosition;
                Main.spriteBatch.Draw(Texture, midpoint, null, PuddingVisuals.Cyan * (fade * .75f),
                    segment.ToRotation(), Texture.Size() * .5f,
                    new Vector2(MathHelper.Clamp(length + 5f, 3f, 55f) / Texture.Width,
                        9f * intensity / Texture.Height), SpriteEffects.None, 0f);
            }
            previous = next;
        }
    }
}
