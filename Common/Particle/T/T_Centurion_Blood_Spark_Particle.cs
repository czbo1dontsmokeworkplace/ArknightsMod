using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Common.Particle.T;

// 两种切割碎光均使用原版 Extra_89；以世界尺寸保持换材质前的轮廓大小。
public class T_Centurion_Blood_Spark_Particle : T_Streak_Particle
{
    public override string TexturePath => "Terraria/Images/Extra_89";
    public override BlendState DrawBlendState => BlendState.AlphaBlend;
    protected virtual Vector2 DrawSize => new(36f, 115.2f);

    public T_Centurion_Blood_Spark_Particle(Vector2 position, Vector2 velocity, int lifetime, float scale, Color color)
        : base(position, velocity, lifetime, scale, color, true) { }

    public override void Draw()
    {
        Vector2 scale = DrawSize * Scale / Texture.Size();
        Vector2 position = Position - Main.screenPosition;
        Main.spriteBatch.Draw(Texture, position, null, Color, Rotation, Texture.Size() * 0.5f,
            scale, SpriteEffects.None, 0f);
        Main.spriteBatch.Draw(Texture, position, null, Color, Rotation, Texture.Size() * 0.5f,
            scale * new Vector2(0.45f, 1f), SpriteEffects.None, 0f);
    }
}

public sealed class T_Centurion_Blood_Line_Particle : T_Centurion_Blood_Spark_Particle
{
    public override BlendState DrawBlendState => BlendState.Additive;
    protected override Vector2 DrawSize => new(5f, 51.2f);

    public T_Centurion_Blood_Line_Particle(Vector2 position, Vector2 velocity, int lifetime, float scale, Color color)
        : base(position, velocity, lifetime, scale, color) { }
}
