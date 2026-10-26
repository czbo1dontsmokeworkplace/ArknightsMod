using ArknightsMod.Content.Items.Weapons.Charged;
using ArknightsMod.Content.Projectiles.Charged;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Common.UI.HeadBars;

public sealed class ChargedSniperHeadBar : WeaponHeadBar
{
    protected override bool TryGetBar(out HeadBarRequest request)
    {
        request = default;
        if (Player.HeldItem.ModItem is not ChargedOperatorWeapon weapon || weapon.Kind is not
            (ChargedOperatorKind.Ambriel or ChargedOperatorKind.Andreana or ChargedOperatorKind.Firewatch)) return false;
        foreach (Projectile projectile in Main.ActiveProjectiles)
        {
            if (projectile.owner != Player.whoAmI || projectile.ModProjectile is not ChargedOperatorHoldout held
                || held.Kind != weapon.Kind || !held.IsSniperCharging(Player.HeldItem)) continue;
            Color color = ChargedOperatorRules.ColorFor(weapon.Kind);
            request = new HeadBarRequest(held.Completion,
                new HeadBarStyle(new Color(5, 14, 28, 224), color, Color.Lerp(color, Color.White, .72f))
                    { FillSmoothing = 1f, FadeInTicks = 4, FadeOutTicks = 8 });
            return true;
        }
        return false;
    }
}
