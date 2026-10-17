using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Common.Particle.T;

/// <summary>仅由本模组ParticleManager管理；世界半径、寿命、宽高均有硬上限。</summary>
public sealed class T_Catapult_Debris : global::ArknightsMod.Common.Particle.Particle
{
    public override string TexturePath => "Terraria/Images/MagicPixel";
    public override BlendState DrawBlendState => ember ? BlendState.Additive : BlendState.AlphaBlend;
    private readonly Vector2 origin;
    private readonly float limit;
    private readonly float width;
    private readonly bool ember;

    public T_Catapult_Debris(Vector2 position, Vector2 velocity, Color color, float width,
        int lifetime, Vector2 origin, float limit, bool ember)
    {
        Position = position;
        Velocity = velocity;
        Color = color;
        this.origin = origin;
        this.limit = MathHelper.Clamp(limit, 8f, 100f);
        this.width = MathHelper.Clamp(width, 1f, 4f);
        this.ember = ember;
        Lifetime = Math.Clamp(lifetime, 8, 28);
        Rotation = velocity.ToRotation();
    }
    public override void Update()
    {
        Velocity *= .90f;
        if (!ember) { Velocity.Y += .06f; Rotation += .12f; }
        // 检查下一步，避免管理器随后积分时越过构图边界。
        if (Vector2.DistanceSquared(Position + Velocity, origin) > limit * limit)
        { Velocity = Vector2.Zero; Kill(); }
    }
    public override void Draw()
    {
        if (Time >= Lifetime) return;
        float fade = 1f - LifetimeRatio;
        // 只截取一个源像素，缩放值就是世界像素尺寸。Alpha始终参与发光亮度。
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, new Rectangle(0, 0, 1, 1),
            Color * fade, Rotation, new Vector2(.5f), new Vector2(width * (ember ? 2f : 1.4f), width) * (.5f + .5f * fade),
            SpriteEffects.None, 0f);
    }
}
