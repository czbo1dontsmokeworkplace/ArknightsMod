using ArknightsMod.Content.Projectiles.BossArsenal;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Players;

public sealed class YetiCrownCapacityPlayer : ModPlayer
{
    public override void PostUpdateEquips()
    {
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == Player.whoAmI && p.ModProjectile is BossArsenalMinion && p.ai[0] == 7)
                BossArsenalMinion.RefreshCrown(p, Player);
    }
}
