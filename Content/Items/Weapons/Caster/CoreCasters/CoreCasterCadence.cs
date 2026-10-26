using Terraria;

namespace ArknightsMod.Content.Items.Weapons.Caster.CoreCasters;

internal static class CoreCasterCadence
{
    internal const int ShotInterval = 4, Shots = 3, RestAfterLastShot = 18;
    // Vanilla fires at 0,4,8; the 12-frame animation then has four frames left.
    // A 14-frame reuse delay makes the next round start at 26: eighteen after its last shot.
    internal static void Apply(Item item)
    {
        item.useTime = ShotInterval;
        item.useAnimation = ShotInterval * Shots;
        item.useLimitPerAnimation = Shots;
        item.reuseDelay = RestAfterLastShot - ShotInterval;
    }
}
