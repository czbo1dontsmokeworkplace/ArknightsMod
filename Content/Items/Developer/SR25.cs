using ArknightsMod.Content.Projectiles.Developer;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;

namespace ArknightsMod.Content.Items.Developer;

public sealed class SR25 : ModItem
{
    internal const int ShotInterval = 12; // 60 * 60 / 300 RPM.
    private bool pickingAmmo;

    public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

    public override void SetDefaults()
    {
        Item.width = 122;
        Item.height = 44;
        Item.damage = 180;
        Item.DamageType = DamageClass.Ranged;
        Item.knockBack = 6f;
        Item.crit = 10;
        Item.useTime = Item.useAnimation = ShotInterval;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = Item.noUseGraphic = true;
        Item.autoReuse = true;
        Item.shootSpeed = 18f;
        Item.shoot = ProjectileID.Bullet;
        Item.useAmmo = AmmoID.Bullet;
        Item.rare = ItemRarityID.Red;
        Item.value = Item.buyPrice(gold: 55);
        Item.accessory = true;
    }

    // The holdout reads both buttons independently, including while scoped or out of ammo.
    public override bool CanUseItem(Player player) => false;
    public override bool CanConsumeAmmo(Item ammo, Player player) => pickingAmmo;
    public override void UpdateAccessory(Player player, bool hideVisual)
    {
        var radar = player.GetModPlayer<SR25Player>();
        radar.RadarEquipped = true;
        radar.HideRadar = hideVisual;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        int lastDescription = tooltips.FindLastIndex(line => line.Mod == "Terraria" && line.Name.StartsWith("Tooltip"));
        tooltips.Insert(lastDescription < 0 ? tooltips.Count : lastDescription + 1,
            new TooltipLine(Mod, "SR25Signature", Language.GetTextValue("Mods.ArknightsMod.Items.SR25.Signature")));
        tooltips.Add(new TooltipLine(Mod, "SR25DeveloperItem", Language.GetTextValue("Mods.ArknightsMod.Items.SR25.DeveloperNote"))
            { OverrideColor = new Color(138, 196, 170) });
    }

    public override bool PreDrawTooltipLine(DrawableTooltipLine line, ref int yOffset)
    {
        if (line.Mod != Mod.Name || line.Name != "SR25Signature") return true;
        SR25Tooltip.Draw(line);
        yOffset += 4;
        return false;
    }

    public override void HoldItem(Player player)
    {
        int type = ModContent.ProjectileType<SR25Holdout>();
        if (player.whoAmI != Main.myPlayer || player.dead || player.noItems || player.CCed
            || player.ownedProjectileCounts[type] != 0) return;
        Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.MountedCenter,
            (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction),
            type, 0, 0, player.whoAmI, player.selectedItem);
    }

    internal bool Fire(Player player, Vector2 muzzle, Vector2 aim)
    {
        bool picked;
        int type, damage, ammo;
        float speed, knockback;
        pickingAmmo = true;
        try { picked = player.PickAmmo(Item, out type, out speed, out damage, out knockback, out ammo); }
        finally { pickingAmmo = false; }
        if (!picked) return false;
        // Identify the actual consumed item: silver and tungsten share a vanilla projectile.
        if (ConvertsAmmo(ammo)) type = ModContent.ProjectileType<SR25Round>();
        Projectile shot = Projectile.NewProjectileDirect(player.GetSource_ItemUse_WithPotentialAmmo(Item, ammo),
            muzzle, aim * speed, type, damage, knockback, player.whoAmI);
        shot.CritChance = player.GetWeaponCrit(Item);
        shot.ArmorPenetration = player.GetWeaponArmorPenetration(Item);
        return true;
    }

    internal static bool ConvertsAmmo(int itemType)
        => itemType is ItemID.MusketBall or ItemID.SilverBullet or ItemID.TungstenBullet;
}
