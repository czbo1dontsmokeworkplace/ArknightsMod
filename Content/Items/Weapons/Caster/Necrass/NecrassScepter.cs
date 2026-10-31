using System;
using ArknightsMod.Content.Projectiles.Caster.Necrass;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Caster.Necrass;

public sealed class NecrassScepter : ExpansionWeaponBase
{
    private const int GammaInterval = 5; // Source 0.08s, rounded to a native 60 Hz item tick.
    internal const float GammaBarrelOffset = 13.25f, GammaSpread = .75f, GammaSpeed = 8.5f;
    private const int ManaWindow = 40; // Preserve the previous nine-mana burst budget.
    private ulong nextGammaManaTick;
    private bool swapBarrels;
    protected override int[] EliteDamage => [399, 483, 578];
    public override string Texture => "ArknightsMod/Content/Items/Weapons/Caster/Necrass/NecrassScepter";
    public override void SetStaticDefaults() => Item.staff[Type] = true;
    public override void SetDefaults()
    {
        Item.width = Item.height = 48;
        Item.damage = EliteDamage[0];
        Item.DamageType = DamageClass.Magic;
        Item.mana = 9;
        Item.useStyle = ItemUseStyleID.Shoot;
        // Two source Gamma Blasters per rapid volley.
        Item.useTime = GammaInterval;
        Item.useAnimation = GammaInterval;
        Item.reuseDelay = 0;
        Item.useLimitPerAnimation = 1;
        Item.noMelee = Item.autoReuse = true;
        Item.knockBack = 3f;
        Item.shootSpeed = 13f;
        Item.shoot = ModContent.ProjectileType<NecrassGammaBlast>();
        Item.rare = ItemRarityID.Yellow;
        Item.value = Item.sellPrice(gold: 8);
    }
    public override bool AltFunctionUse(Player player) => true;
    public override bool CanUseItem(Player player)
    {
        var state = player.GetModPlayer<NecrassPlayer>();
        if (ArknightsKeybinds.SkillActivatePressed(player))
        {
            state.TryActivate();
            return false;
        }
        if (player.itemAnimation <= 0)
        {
            bool seed = player.altFunctionUse == 2;
            Item.mana = seed ? 40 : 9;
            Item.useTime = seed ? 18 : GammaInterval;
            Item.useAnimation = seed ? 18 : GammaInterval;
            Item.reuseDelay = 0;
            Item.useLimitPerAnimation = 1;
        }
        return player.altFunctionUse != 2 || (state.SeedCooldown == 0 && NecrassCourt.Servants(player.whoAmI).Count == 0);
    }
    public override void ModifyManaCost(Player player, ref float reduce, ref float mult)
    {
        // Cadence changes the presentation, not the former 40-tick mana budget.
        if (player.altFunctionUse != 2 && Main.GameUpdateCount < nextGammaManaTick) mult = 0f;
    }
    public override ModItem Clone(Item newEntity)
    {
        var copy = (NecrassScepter)base.Clone(newEntity);
        copy.nextGammaManaTick = 0; copy.swapBarrels = false;
        return copy;
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        var state = player.GetModPlayer<NecrassPlayer>();
        state.EnsureCourt();
        if (player.altFunctionUse == 2)
        {
            // 无杂兵的首领战也能启动塑灵循环；只能在仆役全灭时补一名。
            state.SeedCooldown = 300;
            state.Command(0, NecrassCourt.SafePosition(player, Main.MouseWorld));
            return false;
        }
        Vector2 center = player.RotatedRelativePoint(player.MountedCenter, true);
        Vector2 direction = velocity.SafeNormalize(new Vector2(player.direction, 0));
        Vector2 side = new(-direction.Y, direction.X);
        // Two shots every five ticks retain one full panel-damage budget per
        // former forty-tick cycle. The summon/skill system remains unchanged.
        int shotDamage = Math.Max(1, (int)MathF.Round(damage / 16f));
        for (int i = 0; i < 2; i++)
        {
            int barrel = (i + (swapBarrels ? 1 : 0)) % 2;
            Vector2 muzzle = center + direction * 24 + side * (barrel == 0 ? -GammaBarrelOffset : GammaBarrelOffset);
            float spread = MathHelper.ToRadians(Main.rand.NextFloat(-GammaSpread, GammaSpread));
            int index = Projectile.NewProjectile(source, muzzle, direction.RotatedBy(spread) * GammaSpeed,
                ModContent.ProjectileType<NecrassGammaBlast>(), shotDamage, knockback, player.whoAmI);
            if (Main.projectile.IndexInRange(index)) Main.projectile[index].CritChance = player.GetWeaponCrit(Item);
        }
        swapBarrels = !swapBarrels;
        if (Main.GameUpdateCount >= nextGammaManaTick)
            nextGammaManaTick = Main.GameUpdateCount + (ulong)Math.Max(1, (int)(ManaWindow / player.GetWeaponAttackSpeed(Item)));
        return false;
    }
    public override void AddRecipes() => CreateRecipe()
        .AddIngredient<global::ArknightsMod.Content.Items.Material.PolymerizationPreparation>(1)
        .AddIngredient<global::ArknightsMod.Content.Items.Material.TransmutedSaltAgglomerate>(2)
        .AddIngredient(ItemID.ShadowFlameKnife)
        .AddRecipeGroup(OperatorWeaponRecipeGroups.AnyVanillaTombstone, 5)
        .AddIngredient(ItemID.SpectreBar, 12)
        .AddIngredient(ItemID.SpookyWood, 150)
        .AddIngredient(ItemID.SoulofNight, 15)
        .AddTile(TileID.MythrilAnvil).Register();
}
