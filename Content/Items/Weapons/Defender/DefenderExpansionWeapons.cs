using ArknightsMod.Content.Projectiles.Defender;
using ArknightsMod.Content.Projectiles.Defender.Horn;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Defender;

public class FirewhistleLauncher : DefenderWeapon
{
    // Temporary art: the existing Horn mortar, as requested.
    public override string Texture => "ArknightsMod/Content/Items/Weapons/Defender/Horn/HornMortar";
    protected override int[] EliteDamage => [58, 76, 95];
    protected override int Interval => 48;
    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.shootSpeed = 12f;
        Item.shoot = ModContent.ProjectileType<HornGrenade>();
    }
    public override bool CanUseItem(Player player)
    {
        if (ArknightsKeybinds.SkillActivatePressed(player))
        { player.GetModPlayer<DefenderCombatPlayer>().TryActivate(); return false; }
        return player.HasItem(ItemID.Grenade);
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer || !player.ConsumeItem(ItemID.Grenade)) return false;
        Vector2 aim = (Main.MouseWorld - player.MountedCenter).SafeNormalize(new Vector2(player.direction, 0));
        int index = Projectile.NewProjectile(source, player.MountedCenter + aim * 20f, aim * 12f,
            ModContent.ProjectileType<HornGrenade>(), DefenderRules.SkillActive(player) ? (int)(damage * 1.7f) : damage,
            knockback, player.whoAmI, 95f);
        if (Main.projectile.IndexInRange(index))
        {
            var shell = (HornGrenade)Main.projectile[index].ModProjectile;
            shell.HeavyGravity = true;
            shell.VisualState = DefenderRules.Skill(player, 1) ? (byte)6 : DefenderRules.SkillActive(player) ? (byte)2 : (byte)0;
            Main.projectile[index].CritChance = player.GetWeaponCrit(Item);
            Main.projectile[index].netUpdate = true;
        }
        return false;
    }
}
public sealed class LiskarmPistol : DefenderWeapon
{
    public override string Texture => "Terraria/Images/Item_" + ItemID.Handgun;
    protected override int[] EliteDamage => [78, 102, 126];
    protected override int Interval => 24;
}
public sealed class CroissantHammer : DefenderWeapon
{
    public override string Texture => "ArknightsMod/Content/Items/Weapons/Defender/Vulcan/Vulcan_Weapon";
    protected override int[] EliteDamage => [80, 104, 132];
    protected override int Interval => 36;
}
public sealed class MudrockHammer : DefenderWeapon
{
    public override string Texture => "ArknightsMod/Content/Items/Weapons/Defender/Vulcan/Vulcan_Weapon";
    protected override int[] EliteDamage => [170, 210, 252];
    protected override int Interval => 42;
}
public sealed class EunectesAxe : DefenderWeapon
{
    public override string Texture => "ArknightsMod/Content/Items/Weapons/Defender/Nian/NianWeapon";
    protected override int[] EliteDamage => [150, 190, 238];
    protected override int Interval => 38;
}
