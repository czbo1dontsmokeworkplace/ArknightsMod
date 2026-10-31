using ArknightsMod.Content.Projectiles.Defender;
using ArknightsMod.Content.Projectiles.Defender.Horn;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Audio;
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
        bool ashlock = Name == "AshlockLauncher";
        Item.rare = ashlock ? ItemRarityID.Pink : ItemRarityID.Orange;
        Item.value = Item.sellPrice(gold: ashlock ? 6 : 4);
        Item.shootSpeed = ashlock ? 15f : 12f;
        Item.shoot = ModContent.ProjectileType<HornGrenade>();
    }
    public override bool CanUseItem(Player player)
    {
        var skills = player.GetModPlayer<global::ArknightsMod.Players.WeaponPlayer>();
        if (Name == "FirewhistleLauncher" && skills.Skill == 0 && !skills.SkillActive && skills.StockCount > 0 && player.HasItem(ItemID.Grenade))
            player.GetModPlayer<DefenderCombatPlayer>().TryActivate();
        if (ArknightsKeybinds.SkillActivatePressed(player))
        { player.GetModPlayer<DefenderCombatPlayer>().TryActivate(); return false; }
        return !player.dead && !player.CCed && !player.noItems && player.HasItem(ItemID.Grenade);
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer || !player.ConsumeItem(ItemID.Grenade)) return false;
        bool ashlock = Name == "AshlockLauncher";
        Vector2 aim = (Main.MouseWorld - player.MountedCenter).SafeNormalize(new Vector2(player.direction, 0));
        player.ChangeDir(aim.X >= 0 ? 1 : -1);
        Vector2 origin = player.MountedCenter;
        if (HornCombat.Clear(origin, origin + aim * 35f)) origin += aim * 35f;
        int index = Projectile.NewProjectile(source, origin, aim * (ashlock ? 15f : 12f),
            ModContent.ProjectileType<HornGrenade>(), damage, knockback, player.whoAmI,
            ashlock ? (DefenderRules.Skill(player, 1) ? 105f : 90f) : 75f);
        if (Main.projectile.IndexInRange(index))
        {
            var shell = (HornGrenade)Main.projectile[index].ModProjectile;
            shell.HeavyGravity = !ashlock;
            shell.SourceItemType = Item.type;
            shell.VisualState = !ashlock && DefenderRules.SkillActive(player) ? (byte)6
                : DefenderRules.SkillActive(player) ? (byte)2 : (byte)0;
            shell.FireField = !ashlock && DefenderRules.Skill(player, 1);
            Main.projectile[index].CritChance = player.GetWeaponCrit(Item);
            Main.projectile[index].netUpdate = true;
        }
        HornVisuals.Muzzle(origin, aim, false);
        SoundEngine.PlaySound(SoundID.Item61 with { Volume = .45f, Pitch = ashlock ? -.1f : -.35f, MaxInstances = 3 }, origin);
        player.GetModPlayer<DefenderCombatPlayer>().AttackFinished();
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
