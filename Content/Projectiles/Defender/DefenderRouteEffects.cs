using System;
using ArknightsMod.Content.ElementalImpairment.Effect;
using ArknightsMod.Content.Items.Weapons.Defender;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Defender;

internal static class DefenderRouteEffects
{
    internal static void FireSentinel(Player owner, Vector2 direction, int damage, float knockback)
    {
        if (owner.whoAmI != Main.myPlayer) return;
        bool spread = owner.HeldItem.ModItem.Name == "FaithMixer";
        for (int i = 0; i < (spread ? 3 : 1); i++)
            Projectile.NewProjectile(owner.GetSource_ItemUse(owner.HeldItem), owner.MountedCenter + direction * 22f,
                direction.RotatedBy(spread ? (i - 1) * .12f : 0) * 20f,
                ModContent.ProjectileType<DefenderElectron>(), spread ? Math.Max(1, (int)(damage * .45f)) : damage,
                knockback, owner.whoAmI, DefenderRules.Skill(owner, 1) ? 1 : 0, owner.HeldItem.type);
    }
    internal static void Contact(Player owner, NPC target, int damageDone)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || damageDone <= 0) return;
        string name = owner.HeldItem.ModItem?.Name;
        if (name == "AuroraAxe") target.AddBuff(BuffID.Frostburn, 180);
        if (name is "AsbestosBlade" or "HoshigumaBlazingBlade" && DefenderRules.SkillActive(owner)) target.AddBuff(BuffID.OnFire3, 180);
        if (!DefenderRules.IsPrimal(owner.HeldItem)) return;
        int amount = Math.Max(1, (int)(damageDone * (DefenderRules.SkillActive(owner) ? .3f : .15f)));
        var container = target.GetGlobalNPC<AfflictionGlobalNPC>().Container;
        if (container == null) return;
        switch (name) {
            case "BasslineBlade": container.AddAfflictionValue<NecrosisImpairment>(amount); break;
            case "UnderflowBlade": container.AddAfflictionValue<CorrosionImpairment>(amount); break;
            case "SanbyBlade": container.AddAfflictionValue<BurnImpairment>(amount); break;
            default: container.AddAfflictionValue<NervousImpairment>(amount); break;
        }
    }
}
