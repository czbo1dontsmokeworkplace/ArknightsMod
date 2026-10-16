using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Projectiles.Caster.Passenger;

namespace ArknightsMod.Common.Particle.T;

public sealed class T_Electric_Dust_Particle : ModDust
{
    public override string Texture => "Terraria/Images/Dust";
    public override void SetStaticDefaults() => UpdateType = DustID.Electric;
    public override Color? GetAlpha(Dust dust, Color lightColor)
        => new Color(dust.color.R, dust.color.G, dust.color.B, 25) * MathHelper.Clamp(dust.scale * 1.5f, 0f, 1f);
}
