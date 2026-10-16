using System;
using ArknightsMod.Content.Projectiles.Charged;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;

namespace ArknightsMod.Content.Items.Weapons.Charged;

public abstract class ChargedOperatorWeapon : ExpansionWeaponBase
{
    public abstract ChargedOperatorKind Kind { get; }
    protected abstract int PlaceholderItem { get; }
    protected abstract int Rarity { get; }
    // Explicit placeholder: only vanilla textures, replaced here when original art is available.
    public override string Texture => "Terraria/Images/Item_" + PlaceholderItem;
    internal bool Melee => ChargedOperatorRules.IsMelee(Kind);
    private bool consumingAmmo;
    public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;
    public override void SetDefaults()
    {
        Item.width = 52; Item.height = 52;
        Item.damage = EliteDamage[0];
        Item.DamageType = Melee ? DamageClass.Melee : DamageClass.Ranged;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.useTime = Item.useAnimation = 2;
        Item.noMelee = Item.noUseGraphic = Item.channel = true;
        Item.autoReuse = false;
        Item.knockBack = Melee ? 6f : 4f;
        Item.shootSpeed = 16f;
        Item.shoot = Melee ? ModContent.ProjectileType<ChargedOperatorHoldout>() : ProjectileID.Bullet;
        Item.useAmmo = Melee ? 0 : AmmoID.Bullet;
        Item.rare = Rarity;
        Item.value = Item.sellPrice(silver: 30 * (Rarity + 1));
    }
    public override ModItem Clone(Item newEntity)
    {
        var clone = (ChargedOperatorWeapon)base.Clone(newEntity);
        clone.consumingAmmo = false;
        return clone;
    }
    public override bool CanConsumeAmmo(Item ammo, Player player) => consumingAmmo;
    public override bool AltFunctionUse(Player player) => false;
    public override string GetSkillActivateKeyHint() => string.IsNullOrEmpty(ArknightsKeybinds.SkillActivateKeyDisplay)
        ? Language.GetTextValue("Mods.ArknightsMod.ChargedArsenal.Unbound")
        : Language.GetTextValue("Mods.ArknightsMod.ChargedArsenal.SkillKey", ArknightsKeybinds.SkillActivateKeyDisplay);
    public override void ModifyTooltips(System.Collections.Generic.List<TooltipLine> tooltips)
    {
        base.ModifyTooltips(tooltips);
        tooltips.Add(new TooltipLine(Mod, "ChargedSkillKey", GetSkillActivateKeyHint()));
        tooltips.Add(new TooltipLine(Mod, "ChargedPlaceholder", Language.GetTextValue("Mods.ArknightsMod.ChargedArsenal.Placeholder")));
    }
    public override bool CanUseItem(Player player) => !ArknightsKeybinds.SkillActivatePressed(player)
        && player.GetModPlayer<ChargedOperatorPlayer>().Recovery == 0
        && player.ownedProjectileCounts[ModContent.ProjectileType<ChargedOperatorHoldout>()] == 0;
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI == Main.myPlayer)
            Projectile.NewProjectile(source, player.MountedCenter, velocity.SafeNormalize(Vector2.UnitX * player.direction),
                ModContent.ProjectileType<ChargedOperatorHoldout>(), damage, knockback, player.whoAmI, (int)Kind);
        return false;
    }
    internal bool ReleaseAmmo(Player player, out int damage, out float knockback, out int ammo)
    {
        damage = player.GetWeaponDamage(Item); knockback = player.GetWeaponKnockback(Item); ammo = 0;
        if (Melee) return true;
        consumingAmmo = true;
        try { return player.PickAmmo(Item, out _, out _, out damage, out knockback, out ammo); }
        finally { consumingAmmo = false; }
    }
}
