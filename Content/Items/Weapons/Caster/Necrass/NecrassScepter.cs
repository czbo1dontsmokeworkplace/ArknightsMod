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
    protected override int[] EliteDamage => [76, 92, 110];
    public override string Texture => "Terraria/Images/Item_" + ItemID.InfernoFork;
    public override void SetStaticDefaults() => Item.staff[Type] = true;
    public override void SetDefaults()
    {
        Item.width = Item.height = 48;
        Item.damage = EliteDamage[0];
        Item.DamageType = DamageClass.Magic;
        Item.mana = 9;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.useTime = Item.useAnimation = 32;
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
        Vector2 direction = velocity.SafeNormalize(new Vector2(player.direction, 0));
        position = player.MountedCenter;
        if (Collision.CanHitLine(position, 1, 1, position + direction * 38, 1, 1)) position += direction * 38;
        int index = Projectile.NewProjectile(source, position, direction * Item.shootSpeed, type, damage, knockback, player.whoAmI);
        if (Main.projectile.IndexInRange(index)) Main.projectile[index].CritChance = player.GetWeaponCrit(Item);
        NecrassVisuals.Embers(position, 9, 2.5f);
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
