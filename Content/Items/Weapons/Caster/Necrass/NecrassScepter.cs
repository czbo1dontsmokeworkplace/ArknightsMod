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
    protected override int[] EliteDamage => [399, 483, 578];
    public override string Texture => "Terraria/Images/Item_" + ItemID.InfernoFork;
    public override void SetStaticDefaults() => Item.staff[Type] = true;
    public override void SetDefaults()
    {
        Item.width = Item.height = 48;
        Item.damage = EliteDamage[0];
        Item.DamageType = DamageClass.Magic;
        Item.mana = 9;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.useTime = 8;
        Item.useAnimation = 32;
        Item.useLimitPerAnimation = 4;
        Item.noMelee = Item.autoReuse = true;
        Item.knockBack = 3f;
        Item.shootSpeed = 13f;
        Item.shoot = ModContent.ProjectileType<NecrassSoulBolt>();
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
        Item.mana = player.altFunctionUse == 2 ? 40 : 9;
        return player.altFunctionUse != 2 || (state.SeedCooldown == 0 && NecrassCourt.Servants(player.whoAmI).Count == 0);
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
        // Four releases per animation: one, two, one, then two flames.
        // The six flames share the displayed damage across the full animation.
        int release = Math.Clamp((Item.useAnimation - player.itemAnimation) / Item.useTime, 0, 3);
        int boltCount = release % 2 == 0 ? 1 : 2;
        int firstBolt = release switch { 0 => 0, 1 => 1, 2 => 3, _ => 4 };
        Vector2 center = player.RotatedRelativePoint(player.MountedCenter, true);
        float startAngle = Main.rand.NextFloat(MathHelper.TwoPi);
        for (int i = 0; i < boltCount; i++)
        {
            // Separate sectors keep a two-flame release visibly spread around the player.
            position = center;
            for (int attempt = 0; attempt < 16; attempt++)
            {
                Vector2 offset = (startAngle + i * MathHelper.TwoPi / boltCount + Main.rand.NextFloat(-.35f, .35f))
                    .ToRotationVector2() * Main.rand.NextFloat(24, 60);
                Vector2 candidate = center + offset;
                if (!Collision.CanHitLine(center, 1, 1, candidate + offset.SafeNormalize(Vector2.UnitX) * 8, 1, 1)
                    || Collision.SolidCollision(candidate - new Vector2(7), 14, 14)) continue;
                position = candidate;
                break;
            }
            Vector2 direction = (Main.MouseWorld - position).SafeNormalize(new Vector2(player.direction, 0));
            int shotDamage = damage / 6 + (firstBolt + i < damage % 6 ? 1 : 0);
            int index = Projectile.NewProjectile(source, position, direction * 2.4f, type, shotDamage, knockback,
                player.whoAmI, 2, direction.ToRotation(), Main.rand.NextFloat(MathHelper.TwoPi));
            if (Main.projectile.IndexInRange(index)) Main.projectile[index].CritChance = player.GetWeaponCrit(Item);
            NecrassVisuals.Embers(position, 5, 2.5f);
        }
        SoundEngine.PlaySound(SoundID.Item20 with { Volume = .42f, Pitch = -.65f, MaxInstances = 3 }, position);
        return false;
    }
    public override void AddRecipes() => CreateRecipe()
        .AddIngredient(ItemID.ShadowFlameKnife)
        .AddRecipeGroup(OperatorWeaponRecipeGroups.AnyVanillaTombstone, 5)
        .AddIngredient(ItemID.SpectreBar, 12)
        .AddIngredient(ItemID.SpookyWood, 150)
        .AddIngredient(ItemID.SoulofNight, 15)
        .AddTile(TileID.MythrilAnvil).Register();
}
