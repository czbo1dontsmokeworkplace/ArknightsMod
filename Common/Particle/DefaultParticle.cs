using Microsoft.Xna.Framework;
using ArknightsMod.Common.Particle.T;
namespace ArknightsMod.Common.Particle;

// Compatibility for existing effects outside the T weapon collection.
public class DefaultParticle : T_Streak_Particle
{
    public DefaultParticle(Vector2 relativePosition, Vector2 velocity, int lifetime, float scale, Color color, bool noGravity)
        : base(relativePosition, velocity, lifetime, scale, color, noGravity) { }
}
