using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Medic.Arts;

public sealed class MedicArtsGraphics : ModSystem
{
    public override void Unload()
    {
        // Effects and textures are ModContent-owned; release references rather than double-dispose them.
        MedicIdentityVisuals.Material = null;
        MedicPointedTrail.Material = null;
        MedicArtsVfx.Materials.Clear();
    }
}
