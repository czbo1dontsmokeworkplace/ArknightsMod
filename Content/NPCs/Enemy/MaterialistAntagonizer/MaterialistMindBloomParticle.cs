using System;
using ArknightsMod.Common.Particle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

// Adapts the user's FranMindCoreBloomParticle light pair; no Fran gameplay or other core ornaments.
public sealed class MaterialistMindBloomParticle : Particle
{
    public override string TexturePath => MaterialistAntagonizer.AssetRoot + "MindCoreBloom";
    public override BlendState DrawBlendState => BlendState.Additive;
    private Vector2 initialSize;
    public MaterialistMindBloomParticle() { Lifetime = 7; }
    internal static void Emit(Vector2 point, Vector2 velocity, float age, float opacity)
    {
        float appearance = Math.Clamp(age / 5f, 0, 1);
        Color purple = Color.Lerp(new Color(107, 38, 235), new Color(177, 101, 255), appearance);
        Vector2 drift = -velocity * .05f;
        SpawnLight(point, drift, new Vector2(1.1f - .4f * appearance, .8f + .6f * appearance) *
            MathHelper.Lerp(130, 70, appearance) * 1.1f, purple * (.8f * .7f * opacity), 7);
        SpawnLight(point, drift, new Vector2(.7f, 1.4f) * MathHelper.Lerp(70, 40, appearance) * 1.1f,
            Color.Lerp(purple, Color.White, .6f) * (.7f * opacity), 5);
    }
    private static void SpawnLight(Vector2 point, Vector2 drift, Vector2 size, Color color, int life)
    {
        new MaterialistMindBloomParticle { Position = point, Velocity = drift, initialSize = size, Color = color, Lifetime = life }.Spawn();
    }
    public override void Update()
    {
        Velocity *= .95f; Scale *= .95f;
        Rotation = Velocity.ToRotation() + MathHelper.PiOver2;
    }
    public override void Draw()
    {
        float fade = 1 - MathF.Pow(LifetimeRatio, 3);
        Vector2 size = initialSize * Scale * new Vector2(MathF.Pow(.88f, Time), MathF.Pow(1.12f, Time));
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color * (fade * fade), Rotation,
            Texture.Size() * .5f, size / Texture.Size(), SpriteEffects.None, 0);
    }
}
