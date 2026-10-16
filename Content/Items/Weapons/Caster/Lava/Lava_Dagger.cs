using ArknightsMod.Content;
using ArknightsMod.Content.Projectiles.Caster.Lava;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Caster.Lava;

public class Lava_Dagger : UpgradeWeaponBase
{
    public override void SetDefaults()
    {
        Item.damage = 48;
        Item.knockBack = 3f;
        Item.useTime = Item.useAnimation = 65;
        Item.shootSpeed = 12f;
        Item.shoot = ModContent.ProjectileType<LavaBolt>();
        Item.DamageType = DamageClass.Magic;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.autoReuse = true;
        Item.noMelee = true;
        Item.useTurn = true;
        Item.mana = 5;
        Item.UseSound = SoundID.Item73 with { Volume = .43f };
    }

    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame,
        Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        Texture2D texture = ModContent.Request<Texture2D>(
            "ArknightsMod/Content/Items/Weapons/Caster/Lava/LavaWeapon").Value;
        spriteBatch.Draw(texture, position, null, drawColor, 0f, texture.Size() / 2, scale,
            SpriteEffects.None, 0f);
        return false;
    }

    public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor,
        ref float rotation, ref float scale, int whoAmI)
    {
        Texture2D texture = ModContent.Request<Texture2D>(
            "ArknightsMod/Content/Items/Weapons/Caster/Lava/LavaWeapon").Value;
        spriteBatch.Draw(texture, Item.position - texture.Size() / 2 - Main.screenPosition, null,
            lightColor, rotation, texture.Size() / 2, scale, SpriteEffects.None, 0f);
        return false;
    }

    public override bool CanUseItem(Player player)
    {
        if (ArknightsKeybinds.SkillActivatePressed(player))
        {
            if (player.whoAmI == Main.myPlayer)
            {
                WeaponPlayer skills = player.GetModPlayer<WeaponPlayer>();
                if (!skills.SummonMode && skills.Skill == 0 && skills.StockCount > 0 && !skills.SkillActive)
                {
                    skills.SkillActive = true;
                    skills.SkillTimer = 0;
                    skills.DelStockCount();
                    SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Sounds/SkillActive1")
                        { Volume = .4f, MaxInstances = 4 }, player.Center);
                }
            }
            return false;
        }
        return base.CanUseItem(player);
    }

    public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
    {
        base.ModifyWeaponDamage(player, ref damage);
        WeaponPlayer skills = player.GetModPlayer<WeaponPlayer>();
        if (skills.SkillActive && skills.Skill == 0
            && skills.CurrentSkill?.Key.Item == nameof(Lava_Dagger))
            damage *= 1.5f;
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
        Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI == Main.myPlayer)
        {
            Vector2 direction = velocity.SafeNormalize(Vector2.UnitX * player.direction);
            Vector2 muzzle = player.MountedCenter + direction * 36f;
            int index = Projectile.NewProjectile(source, muzzle, direction * Item.shootSpeed,
                type, damage, knockback, player.whoAmI);
            if (Main.projectile.IndexInRange(index))
                Main.projectile[index].CritChance = player.GetWeaponCrit(Item);
            if (!Main.dedServ)
                global::ArknightsMod.Common.VisualEffects.SpreadArtsVfx.Release(
                    muzzle, direction, global::ArknightsMod.Common.VisualEffects.SpreadArt.Ember);
        }
        return false;
    }

    public override void AddRecipes() => CreateRecipe()
        .AddIngredient<global::ArknightsMod.Content.Items.Material.Oriron>()
        .AddIngredient<global::ArknightsMod.Content.Items.Material.SugarSubstitute>(2)
        .AddIngredient(ItemID.WandofSparking)
        .AddIngredient(ItemID.Torch, 10)
        .AddTile(ModContent.TileType<FactoryTile>())
        .Register();
}
