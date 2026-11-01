using ArknightsMod.Players;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Systems.Gameplay.Skill;

// Custom duration owners reset their clocks, without replaying activation effects or restoring ammo.
public interface IActiveSkillTimeReset
{
    void ResetActiveSkillTime();
}

internal static class ActiveSkillTimeReset
{
    internal static void Reset(Player player) {
        if (!player.GetModPlayer<WeaponPlayer>().SkillActive) return;
        foreach (ModPlayer prototype in ModContent.GetContent<ModPlayer>())
            if (prototype is IActiveSkillTimeReset && player.TryGetModPlayer(prototype, out ModPlayer instance)
                && instance is IActiveSkillTimeReset timer) timer.ResetActiveSkillTime();
        foreach (Projectile projectile in Main.ActiveProjectiles)
            if (projectile.owner == player.whoAmI && projectile.ModProjectile is IActiveSkillTimeReset timer)
                timer.ResetActiveSkillTime();
        player.GetModPlayer<WeaponPlayer>().SkillTimer = 0;
    }
}
