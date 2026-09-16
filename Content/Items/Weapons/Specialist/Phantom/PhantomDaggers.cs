using System.Collections.Generic;
using ArknightsMod.Content.Projectiles.Specialist.Phantom;
using ArknightsMod.Content.Items.Weapons.Specialist.Red;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Specialist.Phantom;

public sealed class PhantomDaggers : ExpansionWeaponBase
{
    protected override int[] EliteDamage => [PhantomBalance.Damage, PhantomBalance.Damage, PhantomBalance.Damage];
    public override string Texture => "ArknightsMod/Content/Items/Weapons/Specialist/Red/RedDagger";
    public override void SetDefaults()
    {
        Item.damage = EliteDamage[0];
        Item.DamageType = DamageClass.Melee;
        Item.width = 30;
        Item.height = 36;
        Item.useTime = Item.useAnimation = 5;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.channel = Item.autoReuse = Item.noMelee = Item.noUseGraphic = true;
        Item.knockBack = 2f;
        Item.crit = 8;
        Item.rare = ItemRarityID.Yellow;
        Item.value = Item.sellPrice(gold: 9);
        Item.shoot = ModContent.ProjectileType<PhantomHoldout>();
        Item.shootSpeed = 1f;
    }
    public override bool AltFunctionUse(Player player) => true;
    public override bool CanUseItem(Player player)
    {
        // 技能与右键在 ModPlayer 中按按下沿处理，连切中也能部署。
        if (ArknightsKeybinds.SkillActivatePressed(player) || player.altFunctionUse == 2)
            return false;
        return player.ownedProjectileCounts[Item.shoot] == 0 && base.CanUseItem(player);
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI == Main.myPlayer)
            Projectile.NewProjectile(source, player.MountedCenter,
                (Main.MouseWorld - player.MountedCenter).SafeNormalize(new Vector2(player.direction, 0)),
                type, damage, knockback, player.whoAmI);
        return false;
    }
    public override void AddRecipes() => CreateRecipe()
        .AddIngredient<RedDagger>()
        .AddIngredient(ItemID.SpectreBar, 12)
        .AddIngredient(ItemID.SoulofNight, 15)
        .AddIngredient(ItemID.Silk, 10)
        .AddTile(ModContent.TileType<global::ArknightsMod.Content.Tiles.Infrastructure.FactoryTile>()).Register();

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        base.ModifyTooltips(tooltips);
        var state = Main.LocalPlayer.GetModPlayer<PhantomPlayer>();
        tooltips.Add(new TooltipLine(Mod, "PhantomCooldown", Language.GetTextValue(
            "Mods.ArknightsMod.Phantom.Status", (state.DeployCooldown + 59) / 60, (state.EchoCooldown + 59) / 60)));
    }
    // 复用匕首素材，通过双刃构图和银蓝/暗红配色形成独立物品外观。
    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame,
        Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        PhantomVisuals.DrawEmblem(spriteBatch, position, scale);
        return false;
    }
    public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor,
        ref float rotation, ref float scale, int whoAmI)
    {
        PhantomVisuals.DrawEmblem(spriteBatch, Item.Center - Main.screenPosition, scale);
        return false;
    }
}
