using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Dusts.Fire
{
	public class fire_13 : ModDust
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
    }
}

