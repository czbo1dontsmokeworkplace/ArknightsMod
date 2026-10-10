using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Dusts.Fire
{
    public class fire_28 : ModDust
    {
	    private const int FrameSize = 85;
	    private const int FrameCount = 9;

	    public override void OnSpawn(Dust dust) {
		    dust.noGravity = true;
		    dust.noLight = false;
		    dust.rotation = 0f;
		    dust.velocity = Vector2.Zero;
		    dust.customData = 0;
		    dust.frame = new Rectangle(0, 0, FrameSize, FrameSize);
	    }

	    public override bool Update(Dust dust)
	    {
		    int frame = (int)dust.customData;
		    if (frame >= FrameCount) {
			    dust.active = false;
			    return false;
		    }
		    dust.frame = new Rectangle((frame % 3) * FrameSize, (frame / 3) * FrameSize,
			    FrameSize, FrameSize);
		    Lighting.AddLight(dust.position, Color.OrangeRed.ToVector3() * (0.85f - frame * 0.06f));
		    dust.customData = frame + 1;
		    return false;
	    }

	    public Texture2D tex => ModContent.Request<Texture2D>("ArknightsMod/Content/Dusts/Fire/fire_28").Value;
	    public override bool PreDraw(Dust dust) {
		    SpriteBatch sb = Main.spriteBatch;
		    sb.End();
		    sb.Begin(
			    SpriteSortMode.Immediate,
			    BlendState.Additive,
			    SamplerState.LinearClamp,
			    DepthStencilState.None,
			    RasterizerState.CullNone,
		    null,
				Main.GameViewMatrix.TransformationMatrix
			);
		    float opacity = 1f - (int)dust.customData / (float)(FrameCount + 2);
		    sb.Draw(tex, dust.position - Main.screenPosition, dust.frame, Color.White * opacity,
			    dust.rotation, dust.frame.Size() * 0.5f, dust.scale, SpriteEffects.None, 0f);
			sb.End();
			sb.Begin();
		    return false;
	    }
    }
}

