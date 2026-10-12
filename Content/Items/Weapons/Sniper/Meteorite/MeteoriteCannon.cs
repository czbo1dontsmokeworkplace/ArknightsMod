using ArknightsMod.Content;
using ArknightsMod.Content.Projectiles.Sniper.Meteorite;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Sniper.Meteorite;

/// <summary>陨星的平射爆破弩炮；任何箭矢只提供弹药，不决定弹幕外观或效果。</summary>
public sealed class MeteoriteCannon : ExpansionWeaponBase
{
    protected override int[] EliteDamage => [104, 119, 137];
    private static readonly SoundStyle FireSound = new("ArknightsMod/Sounds/MeteoriteCannonFire")
    { Volume = .64f, MaxInstances = 3 };
    private static readonly SoundStyle SkillSound = new("ArknightsMod/Sounds/SkillActive1")
    { Volume = .46f, MaxInstances = 3 };

    // 武器本体仍等待正式贴图；原版榴弹发射器只充当物品占位图。
    public override string Texture => "Terraria/Images/Item_" + ItemID.GrenadeLauncher;

    public override void SetDefaults()
    {
        Item.damage = EliteDamage[0];
        Item.DamageType = DamageClass.Ranged;
        Item.width = 54;
        Item.height = 24;
        Item.useTime = Item.useAnimation = 50;
        Item.noUseGraphic = true;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.knockBack = 5f;
        Item.value = Item.sellPrice(gold: 1);
        Item.rare = ItemRarityID.LightRed;
        Item.autoReuse = true;
        Item.noMelee = true;
        Item.useAmmo = AmmoID.Arrow;
        Item.shoot = ModContent.ProjectileType<MeteoriteShell>();
        Item.shootSpeed = 20f;
        Item.UseSound = null;
    }

    public override bool CanUseItem(Player player)
    {
        if (ArknightsKeybinds.SkillActivatePressed(player))
        {
            WeaponPlayer skills = player.GetModPlayer<WeaponPlayer>();
            if (skills.Skill == 1 && skills.StockCount > 0 && !skills.SkillActive)
                ArmSkill(skills, player);
            return false;
        }
        return base.CanUseItem(player);
    }

    public override void HoldItem(Player player)
    {
        base.HoldItem(player);
        WeaponPlayer skills = player.GetModPlayer<WeaponPlayer>();
        if (player.whoAmI == Main.myPlayer && skills.Skill == 0
            && skills.StockCount > 0 && !skills.SkillActive)
            ArmSkill(skills, player);
    }

    private static void ArmSkill(WeaponPlayer skills, Player player)
    {
        skills.SkillActive = true;
        skills.SkillTimer = 0;
        skills.DelStockCount();
        if (!Main.dedServ)
            SoundEngine.PlaySound(SkillSound, player.Center);
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
        Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        WeaponPlayer skills = player.GetModPlayer<WeaponPlayer>();
        int mode = skills.SkillActive ? skills.Skill + 1 : 0;
        Vector2 direction = velocity.SafeNormalize(Vector2.UnitX * player.direction);
        float speed = MathHelper.Max(20f, velocity.Length());
        int shellDamage = mode switch
        {
            1 => (int)(damage * 1.45f),
            2 => (int)(damage * 2.1f),
            _ => damage
        };
        foreach (Projectile old in Main.ActiveProjectiles)
            if (old.owner == player.whoAmI && old.type == ModContent.ProjectileType<MeteoriteHoldout>()) old.Kill();
        Projectile.NewProjectile(source, player.MountedCenter, direction, ModContent.ProjectileType<MeteoriteHoldout>(), 0, 0f, player.whoAmI);
        Vector2 muzzle = player.MountedCenter + Collision.TileCollision(player.MountedCenter - new Vector2(4f), direction * 30f, 8, 8, true, true);
        Projectile.NewProjectile(source, muzzle, direction * speed,
            ModContent.ProjectileType<MeteoriteShell>(), shellDamage, knockback,
            player.whoAmI, mode);
        if (!Main.dedServ)
            SoundEngine.PlaySound(FireSound, player.Center);
        skills.OffensiveRecovery();
        if (mode != 0)
        {
            skills.SkillActive = false;
            skills.SkillTimer = 0;
        }
        return false;
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.MythrilRepeater)
            .AddIngredient(ItemID.MythrilBar, 12)
            .AddIngredient(ItemID.SoulofNight, 8)
            .AddIngredient(ItemID.ExplosivePowder, 12)
            .AddTile(TileID.MythrilAnvil)
            .Register();
        CreateRecipe()
            .AddIngredient(ItemID.OrichalcumRepeater)
            .AddIngredient(ItemID.OrichalcumBar, 12)
            .AddIngredient(ItemID.SoulofNight, 8)
            .AddIngredient(ItemID.ExplosivePowder, 12)
            .AddTile(TileID.MythrilAnvil)
            .Register();
    }
}
