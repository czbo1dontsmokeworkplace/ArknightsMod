using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Evolution.Arsenal;

// Prototype inventory art deliberately references vanilla items; no new bitmap assets.
public sealed class TerminusLight : ModItem
{
    public override string LocalizationCategory => "Items.EvolutionArsenal";
    public override string Texture => $"Terraria/Images/Item_{ItemID.CrystalSerpent}";
    public override void SetStaticDefaults() { Item.staff[Type] = true; Item.ResearchUnlockCount = 1; }
    public override void SetDefaults()
    {
        Item.width = Item.height = 40; Item.damage = 73; Item.DamageType = DamageClass.Magic;
        Item.mana = 22; Item.useTime = Item.useAnimation = 70; Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true; Item.autoReuse = true; Item.knockBack = 3;
        Item.rare = ItemRarityID.Pink; Item.value = Item.sellPrice(gold: 7);
        Item.shoot = ModContent.ProjectileType<TerminusMatrix>(); Item.shootSpeed = 1;
        Item.UseSound = SoundID.Item117;
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI == Main.myPlayer)
            Projectile.NewProjectile(source, EvolutionArsenalRules.ClampAnchor(Main.MouseWorld, player), Vector2.Zero, type, damage, knockback, player.whoAmI);
        return false;
    }
}

public sealed class OriginSever : ModItem
{
    public override string LocalizationCategory => "Items.EvolutionArsenal";
    public override string Texture => $"Terraria/Images/Item_{ItemID.BreakerBlade}";
    public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;
    public override void SetDefaults()
    {
        Item.width = 40; Item.height = 50; Item.damage = 82; Item.DamageType = DamageClass.Melee;
        Item.useTime = Item.useAnimation = 16; Item.useStyle = ItemUseStyleID.Shoot; Item.noMelee = Item.noUseGraphic = true;
        Item.autoReuse = true; Item.knockBack = 6; Item.rare = ItemRarityID.Pink; Item.value = Item.sellPrice(gold: 7);
        Item.shoot = ModContent.ProjectileType<OriginSwing>(); Item.shootSpeed = 1;
    }
    public override bool CanUseItem(Player player)
    {
        if (player.ownedProjectileCounts[Item.shoot] > 0) return false;
        int stage = player.GetModPlayer<EvolutionComboPlayer>().NextStage;
        Item.useAnimation = Item.useTime = EvolutionArsenalRules.SwingTicks(stage);
        return true;
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        var combo = player.GetModPlayer<EvolutionComboPlayer>();
        int stage = combo.NextStage; combo.NextStage = (stage + 1) % 3; combo.ResetIn = 110;
        Vector2 aim = (Main.MouseWorld - player.MountedCenter).SafeNormalize(new Vector2(player.direction, 0));
        int duration = Math.Max(6, (int)(EvolutionArsenalRules.SwingTicks(stage) / player.GetTotalAttackSpeed(DamageClass.Melee)));
        Projectile.NewProjectile(source, player.MountedCenter, aim, type, stage == 2 ? (int)(damage * 1.45f) : damage, knockback, player.whoAmI, stage, 0, duration);
        return false;
    }
}

public sealed class EvolutionComboPlayer : ModPlayer
{
    internal int NextStage, ResetIn;
    public override void PostUpdate()
    {
        if (ResetIn > 0) ResetIn--;
        if (ResetIn == 0 || Player.dead || Player.HeldItem.type != ModContent.ItemType<OriginSever>()) NextStage = 0;
    }
}

public sealed class NestOfLife : ModItem
{
    public override string LocalizationCategory => "Items.EvolutionArsenal";
    public override string Texture => $"Terraria/Images/Item_{ItemID.PygmyStaff}";
    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 1; ItemID.Sets.StaffMinionSlotsRequired[Type] = 2f;
        ItemID.Sets.GamepadWholeScreenUseRange[Type] = true; ItemID.Sets.LockOnIgnoresCollision[Type] = true;
    }
    public override void SetDefaults()
    {
        Item.width = Item.height = 40; Item.damage = 58; Item.DamageType = DamageClass.Summon;
        Item.mana = 10; Item.useTime = Item.useAnimation = 30; Item.useStyle = ItemUseStyleID.Swing;
        Item.noMelee = true; Item.autoReuse = true; Item.knockBack = 3; Item.rare = ItemRarityID.Pink;
        Item.value = Item.sellPrice(gold: 7); Item.UseSound = SoundID.Item44;
        Item.shoot = ModContent.ProjectileType<NestGuardian>(); Item.buffType = ModContent.BuffType<NestGuardianBuff>(); Item.shootSpeed = 1;
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI == Main.myPlayer)
        {
            player.AddBuff(Item.buffType, 18000);
            Vector2 spawn = EvolutionArsenalRules.ClampAnchor(Main.MouseWorld, player);
            if (NestGuardian.TryFindGround(spawn, out Vector2 ground)) spawn = ground;
            int slot = Projectile.NewProjectile(source, spawn, Vector2.Zero, type, damage, knockback, player.whoAmI);
            if (slot < Main.maxProjectiles) Main.projectile[slot].originalDamage = Item.damage;
        }
        return false;
    }
}
