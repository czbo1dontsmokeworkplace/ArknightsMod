using System;
using ArknightsMod.Content;
using ArknightsMod.Content.Items.Weapons.Caster.Lava;
using ArknightsMod.Content.Projectiles.Caster.Gitano;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Caster.Gitano;

/// <summary>远山的水晶球：直接施放水晶群攻术式。</summary>
public sealed class GitanoOrb : ExpansionWeaponBase
{
    protected override int[] EliteDamage => [200, 228, 263];
    private static readonly float[] ChantBonus = [.15f, .20f, .25f, .30f, .35f, .40f, .45f, .55f, .65f, .75f];
    private static readonly float[] FateBonus = [.3f, .35f, .4f, .45f, .5f, .55f, .7f, .8f, .9f, 1f];
    public override string Texture => "Terraria/Images/Item_" + ItemID.CrystalBall;

    public override void SetDefaults()
    {
        Item.width = Item.height = 32;
        Item.damage = EliteDamage[0];
        Item.DamageType = DamageClass.Magic;
        Item.mana = 13;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.useTime = Item.useAnimation = 65;
        Item.autoReuse = true;
        Item.noMelee = true;
        Item.knockBack = 4f;
        Item.shoot = ModContent.ProjectileType<GitanoCrystal>();
        Item.shootSpeed = 15f;
        Item.UseSound = SoundID.Item73 with { Volume = .46f, Pitch = .2f };
        Item.rare = ItemRarityID.LightRed;
        Item.value = Item.sellPrice(gold: 2);
    }

    public override bool CanUseItem(Player player)
    {
        if (ArknightsKeybinds.SkillActivatePressed(player))
        {
            player.GetModPlayer<GitanoOrbPlayer>().TryActivate();
            return false;
        }
        return !player.GetModPlayer<GitanoOrbPlayer>().Exhausted && base.CanUseItem(player);
    }

    public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
    {
        base.ModifyWeaponDamage(player, ref damage);
        WeaponPlayer skills = player.GetModPlayer<WeaponPlayer>();
        if (!skills.SkillActive || skills.CurrentSkill?.Key.Item != nameof(GitanoOrb)) return;
        int rank = Math.Clamp((skills.CurrentSkill.ForceReplaceLevel ?? skills.CurrentSkill.Level) - 1, 0, 9);
        damage *= 1f + (skills.Skill == 0 ? ChantBonus[rank] : FateBonus[rank]);
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
        Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        WeaponPlayer skills = player.GetModPlayer<WeaponPlayer>();
        bool fate = skills.SkillActive && skills.Skill == 1
            && skills.CurrentSkill?.Key.Item == nameof(GitanoOrb);
        Vector2 direction = velocity.SafeNormalize(Vector2.UnitX * player.direction);
        Vector2 muzzle = player.MountedCenter + direction * 35f;
        int index = Projectile.NewProjectile(source, fate ? player.MountedCenter : muzzle,
            fate ? Vector2.Zero : direction * (Item.shootSpeed * 1.2f),
            fate ? ModContent.ProjectileType<GitanoBurst>() : type, damage, knockback,
            player.whoAmI, fate ? 1f : 0f);
        if (Main.projectile.IndexInRange(index))
            Main.projectile[index].CritChance = player.GetWeaponCrit(Item);
        if (!Main.dedServ) GitanoVisuals.FireParticles(muzzle, direction, fate);
        return false;
    }

    public override void AddRecipes() => CreateRecipe()
        .AddIngredient<Lava_Dagger>()
        .AddIngredient(ItemID.CrystalShard, 16)
        .AddIngredient(ItemID.SoulofLight, 6)
        .AddTile(TileID.MythrilAnvil)
        .Register();
}
