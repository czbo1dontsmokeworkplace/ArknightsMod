using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Common.Particle.T;

/// <summary>嘉维尔锯齿甩出的卷曲金属火花：短尾、缓转、冷却后下坠。</summary>
public sealed class T_Gavial_Saw_Spark_Particle : global::ArknightsMod.Common.Particle.Particle
{
    public override string TexturePath => "ArknightsMod/Content/Projectiles/Guard/Mousse/Assets/MousseLightSplinter";
    public override BlendState DrawBlendState => BlendState.Additive;

    private readonly Vector2 origin;
    private readonly Vector2[] trail = new Vector2[3];
    private readonly Color tint;
    private readonly float curl, length;

    public T_Gavial_Saw_Spark_Particle(Vector2 position, Vector2 velocity, Color tint,
        float length, float curl, int lifetime)
    {
        Position = origin = position;
        Velocity = velocity;
        this.tint = tint;
        this.length = MathHelper.Clamp(length, 8f, 24f);
        this.curl = MathHelper.Clamp(curl, -0.12f, 0.12f);
        Lifetime = Math.Clamp(lifetime, 12, 28);
        Rotation = velocity.ToRotation();
        Array.Fill(trail, position);
    }

    public override void Update()
    {
        for (int i = trail.Length - 1; i > 0; i--) trail[i] = trail[i - 1];
        trail[0] = Position;
        Velocity = Velocity.RotatedBy(curl * (1f - LifetimeRatio)) * 0.91f;
        Velocity.Y += 0.035f + LifetimeRatio * 0.07f;
        Rotation = Velocity.ToRotation();
        // 高频切割的火花始终留在接触点周围，避免连续命中铺满屏幕。
        if (Vector2.DistanceSquared(Position + Velocity, origin) > 80f * 80f) Kill();
    }

    public override void Draw()
    {
        if (Time >= Lifetime) return;
        float fade = 1f - LifetimeRatio;
        fade *= fade;
        float size = length * (0.45f + 0.55f * (1f - LifetimeRatio));
        Vector2 scale = new Vector2(size, size * 0.22f) / Texture.Size();
        Color cooled = Color.Lerp(tint, new Color(65, 102, 53), LifetimeRatio);
        for (int i = trail.Length - 1; i >= 0; i--)
        {
            Vector2 segment = (i == 0 ? Position : trail[i - 1]) - trail[i];
            float angle = segment.LengthSquared() > 0.01f ? segment.ToRotation() : Rotation;
            Main.spriteBatch.Draw(Texture, trail[i] - Main.screenPosition, null,
                cooled * (fade * 0.22f * (1f - i / 3f)), angle,
                Texture.Size() * 0.5f, scale * (1f - i * 0.18f), SpriteEffects.None, 0f);
        }
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, cooled * fade,
            Rotation, Texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null,
            new Color(239, 244, 179) * (fade * 0.7f), Rotation,
            Texture.Size() * 0.5f, scale * new Vector2(0.5f, 0.4f), SpriteEffects.None, 0f);
    }
}
