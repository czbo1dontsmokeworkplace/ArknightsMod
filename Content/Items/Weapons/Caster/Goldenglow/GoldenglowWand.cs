using ArknightsMod.Content;
using ArknightsMod.Content.Projectiles.Caster.Goldenglow;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Caster.Goldenglow;

public class GoldenglowWand : ExpansionWeaponBase
{
    protected override int[] EliteDamage => [450, 450, 450];

    public override void AddRecipes() => CreateRecipe()
        .AddIngredient<global::ArknightsMod.Content.Items.Weapons.Caster.Passenger.PassengerConductor>()
        .AddIngredient(ItemID.LunarBar, 20)
        .AddTile(TileID.LunarCraftingStation)
        .Register();

    public override void SetDefaults()
    {
        Item.damage = GoldenglowLightningBalance.Damage;
        Item.DamageType = DamageClass.Magic;
        Item.width = Item.height = 54;
        Item.useTime = Item.useAnimation = 12;
        Item.knockBack = 6f;
        Item.value = Item.sellPrice(gold: 15);
        Item.rare = ItemRarityID.Red;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.shoot = ModContent.ProjectileType<GoldenglowHeldStaff>();
        Item.mana = GoldenglowLightningBalance.ManaPerPayment;
        Item.crit = 4;
        Item.shootSpeed = 1f;
        Item.useStyle = ItemUseStyleID.HoldUp;
        Item.channel = true;
        Item.autoReuse = false;
    }

    public override void HoldItem(Player player)
    {
        base.HoldItem(player);
        // Skill activation must also work while itemTime is held at 2 by the channeled staff.
        if (ArknightsKeybinds.SkillActivatePressed(player))
            TryActivateSkill(player);
    }

    private static void TryActivateSkill(Player player)
    {
        var mp = player.GetModPlayer<WeaponPlayer>();
        if (mp.StockCount <= 0 || mp.SkillActive)
            return;
        mp.SkillActive = true;
        mp.SkillTimer = 0;
        mp.DelStockCount();
        SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Sounds/SkillActive1")
            { Volume = 0.5f, MaxInstances = 2 }, player.Center);
    }

    public override bool AltFunctionUse(Player player) => true;

    public override bool CanUseItem(Player player)
    {
        if (ArknightsKeybinds.SkillActivatePressed(player))
        {
            TryActivateSkill(player);
            return false;
        }
        if (player.altFunctionUse == 2)
        {
            if (player.whoAmI != Main.myPlayer || !player.CheckMana(Item, 18, true))
                return false;
            int type = ModContent.ProjectileType<GoldenglowBeacon>();
            if (player.ownedProjectileCounts[type] >= GoldenglowBeacon.GetMaxBeacons(player))
            {
                Projectile oldest = null;
                foreach (Projectile proj in Main.ActiveProjectiles)
                    if (proj.owner == player.whoAmI && proj.type == type &&
                        (oldest == null || proj.timeLeft < oldest.timeLeft))
                        oldest = proj;
                oldest?.Kill();
            }
            Vector2 offset = Main.MouseWorld - player.Center;
            if (offset.Length() > GoldenglowLightningBalance.Range)
                offset = offset.SafeNormalize(Vector2.UnitX) * GoldenglowLightningBalance.Range;
            Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center + offset,
                Vector2.Zero, type, 0, 0f, player.whoAmI);
            return false;
        }
        return player.ownedProjectileCounts[ModContent.ProjectileType<GoldenglowHeldStaff>()] == 0;
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI == Main.myPlayer)
            Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, type, damage,
                knockback, player.whoAmI, 0f, player.selectedItem);
        return false;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        base.ModifyTooltips(tooltips);
        Player player = Main.LocalPlayer;
        tooltips.Add(new TooltipLine(Mod, "GoldenglowBeaconCount", Language.GetTextValue(
            "Mods.ArknightsMod.Items.GoldenglowWand.BeaconCount",
            player.ownedProjectileCounts[ModContent.ProjectileType<GoldenglowBeacon>()],
            GoldenglowBeacon.GetMaxBeacons(player))));
    }

    public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
    {
        base.ModifyWeaponDamage(player, ref damage);
        var mp = player.GetModPlayer<WeaponPlayer>();
        if (mp.SkillActive)
            damage *= mp.Skill switch { 0 => 1.4f, 1 => 1.6f, 2 => 1.8f, _ => 1f };
    }
}
