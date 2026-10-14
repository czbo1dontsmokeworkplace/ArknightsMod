using System;
using ArknightsMod.Content.Items.Weapons.Caster.Skyfire;
using ArknightsMod.Content.Projectiles.Caster.Mostima;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Caster.Mostima;

public sealed class MostimaStaff : ExpansionWeaponBase
{
    protected override int[] EliteDamage => [280, 320, 370];
    public override void SetStaticDefaults() => Item.staff[Type] = true;
    public override void SetDefaults()
    {
        Item.width = Item.height = 48;
        Item.damage = EliteDamage[0];
        Item.DamageType = DamageClass.Magic;
        Item.mana = 32;
        Item.useTime = Item.useAnimation = 20;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.channel = Item.autoReuse = Item.noMelee = Item.noUseGraphic = true;
        Item.knockBack = 7f;
        Item.shoot = ModContent.ProjectileType<MostimaFocus>();
        Item.shootSpeed = 1f;
        Item.rare = ItemRarityID.Red;
        Item.value = Item.sellPrice(gold: 12);
    }
    internal static bool SkillActive(Player player, int index)
    {
        WeaponPlayer s = player.GetModPlayer<WeaponPlayer>();
        return s.SkillActive && s.Skill == index && s.CurrentSkill?.Key.Item == nameof(MostimaStaff);
    }
    internal static float Rank(Player player)
    {
        var s = player.GetModPlayer<WeaponPlayer>().CurrentSkill;
        return Math.Clamp((s?.ForceReplaceLevel ?? s?.Level ?? 1) - 1, 0, 9) / 9f;
    }
    internal static Vector2 Target(Player player)
    {
        Vector2 offset = Main.MouseWorld - player.MountedCenter;
        if (!float.IsFinite(offset.X) || !float.IsFinite(offset.Y)) return player.MountedCenter;
        return player.MountedCenter + offset.SafeNormalize(Vector2.UnitX) * Math.Min(offset.Length(), 1000f);
    }
    public override bool CanUseItem(Player player)
    {
        if (ArknightsKeybinds.SkillActivatePressed(player))
        {
            player.GetModPlayer<MostimaStaffPlayer>().TryActivate();
            return false;
        }
        if (player.ownedProjectileCounts[ModContent.ProjectileType<MostimaCrossBurst>()] > 0) return false;
        Item.channel = true;
        Item.useTime = Item.useAnimation = 20;
        return player.ownedProjectileCounts[ModContent.ProjectileType<MostimaFocus>()] == 0
            && player.ownedProjectileCounts[ModContent.ProjectileType<MostimaSwing>()] == 0
            && base.CanUseItem(player);
    }
    public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
    {
        base.ModifyWeaponDamage(player, ref damage);
        if (SkillActive(player, 0)) damage *= 1f + MathHelper.Lerp(.3f, 1f, Rank(player));
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI == Main.myPlayer)
        {
            int p = Projectile.NewProjectile(source, player.MountedCenter,
                (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX),
                ModContent.ProjectileType<MostimaFocus>(),
                damage, knockback, player.whoAmI);
            if (Main.projectile.IndexInRange(p)) Main.projectile[p].CritChance = player.GetWeaponCrit(Item);
        }
        return false;
    }
    public override void AddRecipes() => CreateRecipe().AddIngredient<SkyfireStaff>()
        .AddIngredient(ItemID.FragmentNebula, 18).AddIngredient(ItemID.FragmentStardust, 8)
        .AddTile(TileID.LunarCraftingStation).Register();
}
