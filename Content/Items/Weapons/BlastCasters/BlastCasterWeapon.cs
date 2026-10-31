using System;
using System.Collections.Generic;
using ArknightsMod.Content;
using ArknightsMod.Content.Projectiles.BlastCasters;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.BlastCasters;

public abstract class BlastCasterWeapon : ExpansionWeaponBase
{
    public abstract BlastCasterKind Kind { get; }
    // Repository artwork and skill icons are explicit placeholders, not Calamity assets.
    public override string Texture => BlastCasterRules.GunTexture;
    private bool pickingAmmo;
    public override ModItem Clone(Item newEntity)
    { var copy = (BlastCasterWeapon)base.Clone(newEntity); copy.pickingAmmo = false; return copy; }
    public override void SetDefaults()
    {
        Item.width = 58; Item.height = 26;
        Item.damage = EliteDamage[0]; Item.DamageType = DamageClass.Ranged;
        Item.useTime = Item.useAnimation = BlastCasterRules.Interval(Kind);
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = Item.noUseGraphic = Item.channel = Item.autoReuse = true;
        Item.useAmmo = AmmoID.Gel;
        Item.shoot = ModContent.ProjectileType<BlastCasterHoldout>();
        Item.shootSpeed = BlastCasterRules.Step(Kind);
        Item.knockBack = Kind == BlastCasterKind.Corroserum ? .8f : 1.5f;
        Item.rare = Kind == BlastCasterKind.Corroserum ? ItemRarityID.LightRed : Kind == BlastCasterKind.Ifrit ? ItemRarityID.Yellow : ItemRarityID.Red;
        Item.value = Item.sellPrice(gold: 3 + (int)Kind * 5);
    }
    public override bool AltFunctionUse(Player player) => false;
    public override bool CanConsumeAmmo(Item ammo, Player player) => pickingAmmo;
    internal bool PickGel(Player player, bool consume, out int damage, out float knockback, out int ammo)
    {
        pickingAmmo = true;
        try { return player.PickAmmo(Item, out _, out _, out damage, out knockback, out ammo, !consume); }
        finally { pickingAmmo = false; }
    }
    public override void HoldItem(Player player)
    {
        base.HoldItem(player);
        player.GetModPlayer<BlastCasterPlayer>().HandleInput(this);
    }
    public override bool CanUseItem(Player player)
    {
        if (ArknightsKeybinds.SkillActivatePressed(player))
        { player.GetModPlayer<BlastCasterPlayer>().HandleInput(this); return false; }
        if (player.altFunctionUse == 2) return false; // No alternate-fire action.
        return player.ownedProjectileCounts[ModContent.ProjectileType<BlastCasterHoldout>()] == 0;
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI == Main.myPlayer)
            Projectile.NewProjectile(source, player.MountedCenter, velocity.SafeNormalize(Vector2.UnitX * player.direction),
                ModContent.ProjectileType<BlastCasterHoldout>(), 0, 0, player.whoAmI, (int)Kind);
        return false;
    }
    public override string GetSkillActivateKeyHint() => Language.GetTextValue("Mods.ArknightsMod.BlastCasterText.SkillKey",
        string.IsNullOrEmpty(ArknightsKeybinds.SkillActivateKeyDisplay) ? "?" : ArknightsKeybinds.SkillActivateKeyDisplay);
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        base.ModifyTooltips(tooltips);
        tooltips.Add(new TooltipLine(Mod, "BlastSkillKey", GetSkillActivateKeyHint()));

    }
}

public sealed class CorroserumFlamethrower : BlastCasterWeapon
{
    public override BlastCasterKind Kind => BlastCasterKind.Corroserum;
    protected override int[] EliteDamage => [24, 28, 32];
    public override void AddRecipes()
    {
        foreach (int fuel in new[] { ItemID.CursedFlame, ItemID.Ichor })
            CreateRecipe().AddIngredient(ItemID.IllegalGunParts).AddIngredient(ItemID.HellstoneBar, 12)
                .AddIngredient(fuel, 10).AddIngredient(ItemID.Gel, 100).AddRecipeGroup("IronBar", 10)
                .AddCondition(Condition.Hardmode).AddTile(TileID.Anvils).Register();
    }
}
public sealed class IfritFlamethrower : BlastCasterWeapon
{
    public override string Texture => "ArknightsMod/Content/Items/Weapons/BlastCasters/IfritFlamethrower";
    public override BlastCasterKind Kind => BlastCasterKind.Ifrit;
    protected override int[] EliteDamage => [55, 65, 75];
    // This source sprite points left. Mirror it wherever the item is displayed,
    // matching the Ifrit-only mirror used by the holdout and its right-facing muzzle.
    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame,
        Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        spriteBatch.Draw(TextureAssets.Item[Type].Value, position, frame, drawColor, 0f, origin,
            scale, SpriteEffects.FlipHorizontally, 0f);
        return false;
    }
    public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor,
        ref float rotation, ref float scale, int whoAmI)
    {
        Texture2D texture = TextureAssets.Item[Type].Value;
        Vector2 position = Item.position - Main.screenPosition
            + new Vector2(Item.width * .5f, Item.height - texture.Height * .5f);
        spriteBatch.Draw(texture, position, null, lightColor, rotation, texture.Size() * .5f,
            scale, SpriteEffects.FlipHorizontally, 0f);
        return false;
    }
    public override void AddRecipes() => CreateRecipe().AddIngredient<CorroserumFlamethrower>()
        .AddIngredient(ItemID.ShroomiteBar, 14).AddIngredient(ItemID.Ectoplasm, 8)
        .AddIngredient(ItemID.Gel, 150).AddCondition(Condition.DownedPlantera)
        .AddTile(TileID.MythrilAnvil).Register();
}
public sealed class AphrissaProjector : BlastCasterWeapon
{
    public override BlastCasterKind Kind => BlastCasterKind.Aphrissa;
    protected override int[] EliteDamage => [72, 84, 96];
    public override void AddRecipes() => CreateRecipe().AddIngredient<IfritFlamethrower>()
        .AddIngredient(ItemID.FragmentVortex, 18).AddIngredient(ItemID.FragmentNebula, 12)
        .AddIngredient(ItemID.ShroomiteBar, 12).AddIngredient(ItemID.Gel, 200)
        .AddTile(TileID.LunarCraftingStation).Register();
}
