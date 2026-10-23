using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

internal static partial class MaterialistVisuals
{
    internal static void DrawMedicLaser(Vector2 origin, Vector2 delta, float strength, int age)
    {
        float length = delta.Length();
        if (length < 1 || length > 500 || strength <= 0) return;
        Main.instance.LoadProjectile(ProjectileID.PhantasmalDeathray);
        Texture2D start = TextureAssets.Projectile[ProjectileID.PhantasmalDeathray].Value;
        Texture2D middle = TextureAssets.Extra[21].Value, end = TextureAssets.Extra[22].Value;
        Vector2 direction = delta / length;
        // Native segmented light with its own green tint; bypass the purple recolouring material.
        DrawBeamLayer(origin, direction, length, 12, age, start, middle, end,
            20 * strength, new Color(28, 141, 78, 0) * (.35f * strength));
        DrawBeamLayer(origin, direction, length, 12, age, start, middle, end,
            12 * strength, new Color(72, 243, 136, 0) * strength);
        DrawBeamLayer(origin, direction, length, 12, age, start, middle, end,
            4 * strength, new Color(225, 255, 231, 0) * strength);
        Glow(origin + delta, new Color(91, 244, 155) * (.35f * strength), new Vector2(38));
    }
}
