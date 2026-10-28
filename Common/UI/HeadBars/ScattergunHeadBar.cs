using System;
using ArknightsMod.Content.Items.Weapons.Scatterguns;
using ArknightsMod.Content.Projectiles.Scatterguns;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Common.UI.HeadBars;

public sealed class ScattergunHeadBar : WeaponHeadBar
{
    protected override bool TryGetBar(out HeadBarRequest request)
    {
        request = default;
        if (Player.HeldItem.ModItem is not Scattergun gun) return false;
        // Read the exact same movement magnitude and run-speed cap as Scattergun.Shoot.
        float cone = ScattergunSpread.TotalDegrees(Player.velocity.Length(), Math.Max(Player.maxRunSpeed, Player.accRunSpeed));
        float concentration = Math.Clamp((20f - cone) / 15f, 0, 1);
        Color color = ScatterVisuals.ColorFor(gun.Tier);
        request = new HeadBarRequest(concentration,
            new HeadBarStyle(new Color(5, 14, 28, 224), color, Color.Lerp(color, Color.White, .72f))
                { FillSmoothing = 1f });
        return true;
    }
}
