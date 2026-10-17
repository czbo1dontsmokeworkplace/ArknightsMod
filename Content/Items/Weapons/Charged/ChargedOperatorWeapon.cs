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
        Item.noMelee = Item.noUseGraphic = true;
        Item.channel = Melee;
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
    public override bool AltFunctionUse(Player player) => !Melee;
    internal static bool WorldInputAllowed(Player player) => Main.hasFocus && !Main.gameMenu && !Main.playerInventory
        && !Main.blockInput && !Main.drawingPlayerChat && !player.mouseInterface && player.talkNPC < 0
        && player.sign < 0 && player.chest == -1 && !player.dead && !player.noItems && !player.CCed;
    public override void HoldItem(Player player)
    {
        base.HoldItem(player);
        if (Melee || player.whoAmI != Main.myPlayer) return;
        var state = player.GetModPlayer<ChargedOperatorPlayer>();
        bool world = WorldInputAllowed(player);
        if (!Main.mouseRight) state.SuppressSniperRight = false;
        else if (!world || player.tileInteractionHappened) state.SuppressSniperRight = true;
        if (!world || state.Recovery > 0 || ArknightsKeybinds.SkillActivatePressed(player)) return;
        bool right = Main.mouseRight && !state.SuppressSniperRight;
        bool left = ChargedOperatorRules.SniperLeftReady(Main.mouseLeft, Main.mouseRight, Main.GameUpdateCount, state.NextSniperLeftShot);
        if ((!right && !left) || !player.HasAmmo(Item)) return;
        int holdoutType = Kind == ChargedOperatorKind.Fartooth ? ModContent.ProjectileType<FartoothHoldout>() : ModContent.ProjectileType<ChargedOperatorHoldout>();
        foreach (Projectile held in Main.ActiveProjectiles)
        {
            if (held.owner != player.whoAmI || held.type != holdoutType) continue;
            if (held.ModProjectile is ChargedOperatorHoldout && held.ai[2] > 0
                || held.ModProjectile is FartoothHoldout && held.ai[0] == 2) held.Kill();
            else return;
        }
        Vector2 aim = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
        Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.MountedCenter, aim,
            holdoutType, player.GetWeaponDamage(Item), player.GetWeaponKnockback(Item),
            player.whoAmI, Kind == ChargedOperatorKind.Fartooth ? right ? 1 : 0 : (int)Kind, right ? 0 : -1);
    }
    public override string GetSkillActivateKeyHint() => string.IsNullOrEmpty(ArknightsKeybinds.SkillActivateKeyDisplay)
        ? Language.GetTextValue("Mods.ArknightsMod.ChargedArsenal.Unbound")
        : Language.GetTextValue("Mods.ArknightsMod.ChargedArsenal.SkillKey", ArknightsKeybinds.SkillActivateKeyDisplay);
    public override void ModifyTooltips(System.Collections.Generic.List<TooltipLine> tooltips)
    {
        base.ModifyTooltips(tooltips);
        tooltips.Add(new TooltipLine(Mod, "ChargedSkillKey", GetSkillActivateKeyHint()));
        tooltips.Add(new TooltipLine(Mod, "ChargedPlaceholder", Language.GetTextValue("Mods.ArknightsMod.ChargedArsenal.Placeholder")));
    }
    // Sniper inputs are polled in HoldItem; vanilla channel only drives the melee branch.
    public override bool CanUseItem(Player player) => Melee && !ArknightsKeybinds.SkillActivatePressed(player)
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
