using System;
using ArknightsMod.Content.Projectiles.Scatterguns;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Scatterguns;

public abstract class Scattergun : ExpansionWeaponBase
{
    public abstract int Tier { get; }
    public override string Texture => "Terraria/Images/Item_" + new[] { ItemID.NailGun, ItemID.Shotgun, ItemID.WaterGun }[Tier];
    public override void SetDefaults()
    {
        Item.width = 56; Item.height = 24;
        Item.damage = EliteDamage[0]; Item.DamageType = DamageClass.Ranged;
        Item.useTime = Tier == 0 ? 28 : Tier == 1 ? 4 : 6;
        Item.useAnimation = Tier == 0 ? 28 : Item.useTime * (Tier + 1);
        Item.useLimitPerAnimation = Tier + 1;
        // The final use interval belongs to the pause: last shot -> next burst is 46 / 32 frames.
        Item.reuseDelay = Tier == 0 ? 0 : (Tier == 1 ? 46 : 32) - Item.useTime;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = Item.noUseGraphic = Item.autoReuse = true;
        Item.useAmmo = AmmoID.Bullet;
        Item.shoot = Tier == 0 ? ProjectileID.NailFriendly : ModContent.ProjectileType<ScatterPellet>();
        Item.shootSpeed = new[] { 12f, 14f, 13f }[Tier];
        Item.knockBack = Tier == 1 ? 6f : 3f;
        Item.rare = new[] { ItemRarityID.Green, ItemRarityID.LightRed, ItemRarityID.Red }[Tier];
        Item.value = Item.sellPrice(gold: 1 + Tier * 6);
    }
    internal static void ConfigureNailImmunity(Projectile nail)
    {
        nail.usesLocalNPCImmunity = true;
        nail.usesIDStaticNPCImmunity = false;
        // A finite cooldown lets the same nail damage its host again when its vanilla fuse explodes.
        nail.localNPCHitCooldown = 10;
    }
    public override bool AltFunctionUse(Player player) => false;
    public override bool CanUseItem(Player player)
    {
        if (ArknightsKeybinds.SkillActivatePressed(player))
        {
            player.GetModPlayer<ScattergunPlayer>().TryActivate(); return false;
        }
        return true;
    }
    public override float UseSpeedMultiplier(Player player)
        => player.GetModPlayer<ScattergunPlayer>().Mode == 2 && Tier < 2 ? (Tier == 0 ? 1.65f : 1.75f) : 1;
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        int mode = player.GetModPlayer<ScattergunPlayer>().Mode;
        Vector2 aim = (Main.MouseWorld - player.MountedCenter).SafeNormalize(new Vector2(player.direction, 0));
        Vector2 muzzle = player.MountedCenter + aim * 42;
        if (!Collision.CanHitLine(player.MountedCenter, 1, 1, muzzle, 1, 1)) muzzle = player.MountedCenter;
        int count = new[] { 5, 8, 7 }[Tier];
        float fullRunSpeed = Math.Max(player.maxRunSpeed, player.accRunSpeed);
        float spread = MathHelper.ToRadians(ScattergunSpread.TotalDegrees(player.velocity.Length(), fullRunSpeed)) * .5f;
        float power = mode == 1 ? 1.35f : mode == 2 && Tier == 2 ? 1.35f : 1f;
        for (int i = 0; i < count; i++)
        {
            float angle = MathHelper.Lerp(-spread, spread, i / (float)(count - 1));
            Vector2 shotVelocity = aim.RotatedBy(angle) * velocity.Length() * Main.rand.NextFloat(.9f, 1.1f);
            // Vanilla nails own their sticking/fuse/explosion AI. Never pass our tier/skill values into it.
            int id = Tier == 0
                ? Projectile.NewProjectile(source, muzzle, shotVelocity, ProjectileID.NailFriendly,
                    (int)(damage * power), knockback, player.whoAmI)
                : Projectile.NewProjectile(source, muzzle, shotVelocity, ModContent.ProjectileType<ScatterPellet>(),
                    (int)(damage * power), knockback, player.whoAmI, Tier, mode);
            if (Main.projectile.IndexInRange(id))
            {
                Main.projectile[id].CritChance = player.GetWeaponCrit(Item);
                if (Tier == 0) ConfigureNailImmunity(Main.projectile[id]);
            }
        }
        // One held sprite; subsequent volleys restart its recoil and muzzle feedback.
        foreach (Projectile held in Main.ActiveProjectiles)
        {
            if (held.owner != player.whoAmI || held.type != ModContent.ProjectileType<ScattergunHoldout>()) continue;
            held.velocity = aim;
            held.ai[0] = Tier;
            held.ai[1] = Math.Max(10, player.itemAnimationMax + player.reuseDelay);
            held.ai[2]++;
            held.netUpdate = true;
            return false;
        }
        Projectile.NewProjectile(source, player.MountedCenter, aim,
            ModContent.ProjectileType<ScattergunHoldout>(), 0, 0, player.whoAmI,
            Tier, Math.Max(10, player.itemAnimationMax + player.reuseDelay), 1);
        return false;
    }

}

public sealed class PineconeNailgun : Scattergun
{
    public override int Tier => 0;
    protected override int[] EliteDamage => [14, 17, 20];
    public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.Boomstick)
        .AddRecipeGroup(OperatorWeaponRecipeGroups.IronOrLeadBar, 8).AddIngredient(ItemID.Wire, 12)
        .AddTile(ModContent.TileType<global::ArknightsMod.Content.Tiles.Infrastructure.FactoryTile>()).Register();
}
public sealed class ExecutorShotgun : Scattergun
{
    public override int Tier => 1;
    protected override int[] EliteDamage => [12, 14, 16];
    public override void AddRecipes() => CreateRecipe().AddIngredient<PineconeNailgun>()
        .AddIngredient(ItemID.Shotgun).AddRecipeGroup(OperatorWeaponRecipeGroups.CobaltOrPalladiumBar, 12)
        .AddIngredient(ItemID.SoulofNight, 8)
        .AddTile(ModContent.TileType<global::ArknightsMod.Content.Tiles.Infrastructure.FactoryTile>()).Register();
}
public sealed class ChalterWatergun : Scattergun
{
    public override int Tier => 2;
    protected override int[] EliteDamage => [23, 27, 32];
    public override void AddRecipes() => CreateRecipe().AddIngredient<ExecutorShotgun>()
        .AddIngredient(ItemID.TacticalShotgun).AddIngredient(ItemID.ShroomiteBar, 20).AddIngredient(ItemID.WetBomb, 20).AddIngredient(ItemID.BottledWater, 20)
        .AddTile(ModContent.TileType<global::ArknightsMod.Content.Tiles.Infrastructure.FactoryTile>()).Register();
}
