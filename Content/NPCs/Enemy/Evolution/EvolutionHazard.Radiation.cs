using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.GameContent;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

public sealed partial class EvolutionHazard
{
    private bool pressureIgnited;
    internal bool PressureLive => Kind == EvolutionShot.Radiation && Parent is Evolution boss && boss.Phase == 1 && boss.Serial == Serial;
    internal int PressureAge => Age - FireAge;
    private void UpdateRadiation(Evolution boss)
    {
        if (boss.Phase != 1) { Projectile.Kill(); return; }
        if (!pressureIgnited && Age >= FireAge)
        {
            pressureIgnited = true;
            if (!Main.dedServ && PressureAge <= 3)
            {
                SoundEngine.PlaySound(SoundID.Zombie104 with { Volume = .6f, Pitch = .15f }, Projectile.Center);
                EvolutionImpactSystem.Kick(Projectile.Center, 5.5f);
            }
        }
    }
    internal bool RadiationCollides(Rectangle box) => EvolutionShockwaveRules.Collides(box, Projectile.Center, Parameter, PressureAge);
    internal void DrawPressureMaterial(Effect material)
    {
        Vector2 at = Projectile.Center - Main.screenPosition;
        if (PressureAge < 0)
            EvolutionShockwaveVisuals.DrawWarningMaterial(Main.spriteBatch, material, EvolutionVisuals.Asset("PressureNoise"), at,
                EvolutionShockwaveRules.Reach, Parameter, Age, Delay, FireAge, EvolutionVisuals.Sand, EvolutionVisuals.SandCore);
        else if (PressureAge < EvolutionShockwaveRules.Duration)
            EvolutionShockwaveVisuals.DrawPressureFront(Main.spriteBatch, material, EvolutionVisuals.Asset("PressureNoise"), at,
                PressureAge, Parameter, EvolutionVisuals.Sand, EvolutionVisuals.SandCore);
    }
    internal void DrawPressureDetails(bool fallback)
    {
        if (PressureAge < 0 || PressureAge >= EvolutionShockwaveRules.Duration) return;
        EvolutionShockwaveVisuals.DrawPressureDebris(Main.spriteBatch, EvolutionVisuals.Asset("Bloom"), TextureAssets.MagicPixel.Value,
            EvolutionVisuals.Asset("CinematicRing"), Projectile.Center - Main.screenPosition,
            PressureAge, Parameter, EvolutionVisuals.Sand, EvolutionVisuals.SandCore, EvolutionCinematics.Reduced, fallback);
    }
    private void DrawRadiation()
    {
        if (PressureAge >= 0) return;
        EvolutionShockwaveVisuals.DrawWarningGuides(Main.spriteBatch, TextureAssets.MagicPixel.Value, EvolutionVisuals.Asset("Bloom"),
            Projectile.Center - Main.screenPosition, EvolutionShockwaveRules.Reach, Parameter, Age, Delay, FireAge,
            EvolutionVisuals.Sand, EvolutionVisuals.SandCore, EvolutionCinematics.Reduced);
    }
}
