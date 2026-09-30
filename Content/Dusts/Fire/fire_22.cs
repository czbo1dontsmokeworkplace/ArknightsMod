using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Dusts.Fire
{
	public class fire_22 : ModDust
    {
	    public override void OnSpawn(Dust dust) {
		    dust.noGravity = true;
		    dust.noLight = false;
		    dust.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
		    dust.customData = 0;
	    }

	    public override bool Update(Dust dust)
	    {
		    dust.position += dust.velocity;
		    dust.velocity *= 0.9f;
		    dust.customData = (int)dust.customData + 1;
		    dust.frame = new Rectangle((64 * ((int)dust.customData))%4, (64 * ((int)dust.customData / 4)), 64, 64);
		    Lighting.AddLight(dust.position, Color.OrangeRed.ToVector3()*0.5f*(32-(int)dust.customData)/32);
		    if ((int)dust.customData > 16)
			    dust.active = false;
		    return false;
	    }

	    public Texture2D tex => ModContent.Request<Texture2D>("ArknightsMod/Content/Dusts/Fire/fire_22").Value;
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
		    sb.Draw(tex, dust.position - Main.screenPosition,dust.frame, Color.White);
		    sb.End();
		    sb.Begin();
		    return false;
	    }
    }
}

