using System;
using ArknightsMod.Content.Items.Weapons.Caster.Indigo;
using ArknightsMod.Content.Items.Weapons.Caster.Harmonie;
using ArknightsMod.Content.Items.Weapons.Caster.Ebenholz;
using ArknightsMod.Content.Projectiles.Caster.Indigo;
using ArknightsMod.Content.Projectiles.Caster.Harmonie;
using ArknightsMod.Content.Projectiles.Caster.Ebenholz;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Caster;

public sealed class MysticCoordinatedAttack : GlobalItem
{
    public override void UseAnimation(Item item, Player player)
    {
        if (player.whoAmI != Main.myPlayer || player.altFunctionUse == 2 || item.damage <= 0) return;
        player.GetModPlayer<IndigoStaffPlayer>().ReleaseCoordinated(item);
        player.GetModPlayer<HarmonieStaffPlayer>().ReleaseCoordinated(item);
        player.GetModPlayer<EbenholzStaffPlayer>().ReleaseCoordinated(item);
    }

    internal static void Fire(Player player, Item staff, int count, int kind)
    {
        if (staff == null || count <= 0) return;
        Vector2 aim = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
        int type = kind == 0 ? ModContent.ProjectileType<IndigoDroplet>()
            : kind == 1 ? ModContent.ProjectileType<HarmonieNote>() : ModContent.ProjectileType<EbenholzCube>();
        int target = EbenholzCombat.FindTarget(player.MountedCenter, 900f, Main.MouseWorld, false);
        int damage = Math.Max(1, (int)MathF.Round(player.GetWeaponDamage(staff) * 1.15f));
        for (int i = 0; i < count; i++)
        {
            int id = Projectile.NewProjectile(player.GetSource_ItemUse(staff), player.MountedCenter,
                aim.RotatedBy((i - (count - 1) * .5f) * .22f) * 11f, type, damage,
                player.GetWeaponKnockback(staff), player.whoAmI, 1, target + 1, 1 + i * 4);
            if (Main.projectile.IndexInRange(id)) Main.projectile[id].CritChance = player.GetWeaponCrit(staff);
        }
    }
}
