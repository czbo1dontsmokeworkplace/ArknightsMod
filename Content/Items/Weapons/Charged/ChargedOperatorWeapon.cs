using System;
using ArknightsMod.Content.Projectiles.Charged;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
using Terraria.Audio;

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
    private Vector2 remoteAim;
    private int lastRemoteAnimation;
    public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;
    public override void SetDefaults()
    {
        Item.width = 52; Item.height = 52;
        Item.damage = EliteDamage[0];
        Item.DamageType = Melee ? DamageClass.Melee : DamageClass.Ranged;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.useTime = Item.useAnimation = Melee ? DreadnoughtRules.BaseSwingTicks : 60;
        Item.noMelee = true; Item.noUseGraphic = Melee;
        Item.channel = false;
        Item.autoReuse = true;
        Item.useTurn = Melee;
        Item.knockBack = Melee ? 6f : 4f;
        Item.shootSpeed = 16f;
        Item.shoot = Melee ? ModContent.ProjectileType<DreadnoughtSwing>() : ProjectileID.Bullet;
        Item.useAmmo = Melee ? 0 : AmmoID.Bullet;
        Item.rare = Rarity;
        Item.value = Item.sellPrice(silver: 30 * (Rarity + 1));
        if (!Melee) Item.UseSound = new SoundStyle("ArknightsMod/Assets/Sounds/ChargedSniper/NitroFire") { Volume = .55f };
    }
    public override ModItem Clone(Item newEntity)
    {
        var clone = (ChargedOperatorWeapon)base.Clone(newEntity);
        clone.consumingAmmo = false;
        clone.lastRemoteAnimation = 0;
        clone.remoteAim = Vector2.Zero;
        return clone;
    }
    public override bool CanConsumeAmmo(Item ammo, Player player) => consumingAmmo || !Melee && player.altFunctionUse != 2;
    public override bool AltFunctionUse(Player player) => !Melee;
    public override bool MeleePrefix() => Melee;
    public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
    {
        base.ModifyWeaponDamage(player, ref damage);
        if (Melee) damage *= player.GetModPlayer<DreadnoughtPlayer>().Multiplier;
    }
    internal static bool WorldInputAllowed(Player player) => Main.hasFocus && !Main.gameMenu && !Main.playerInventory
        && !Main.blockInput && !Main.drawingPlayerChat && !player.mouseInterface && player.talkNPC < 0
        && player.sign < 0 && player.chest == -1 && !player.dead && !player.noItems && !player.CCed;
    public override void HoldItem(Player player)
    {
        base.HoldItem(player);
        if (!Melee)
        {
            Item.noUseGraphic = false;
            foreach (Projectile p in Main.ActiveProjectiles)
                if (p.owner == player.whoAmI && p.ModProjectile is ChargedOperatorHoldout)
                { Item.noUseGraphic = true; break; }
        }
        if (Melee || player.whoAmI != Main.myPlayer) return;
        var state = player.GetModPlayer<ChargedOperatorPlayer>();
        bool world = WorldInputAllowed(player);
        if (!Main.mouseRight) state.SuppressSniperRight = false;
        else if (!world || player.tileInteractionHappened) state.SuppressSniperRight = true;
        if (!world || state.Recovery > 0 || ArknightsKeybinds.SkillActivatePressed(player)) return;
        bool right = Main.mouseRight && !state.SuppressSniperRight;
        if (!right || player.itemAnimation > 0 || !player.HasAmmo(Item)) return;
        int holdoutType = ModContent.ProjectileType<ChargedOperatorHoldout>();
        foreach (Projectile held in Main.ActiveProjectiles)
        {
            if (held.owner != player.whoAmI || held.type != holdoutType) continue;
            return;
        }
        Vector2 aim = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
        Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.MountedCenter, aim,
            holdoutType, player.GetWeaponDamage(Item), player.GetWeaponKnockback(Item),
            player.whoAmI, (int)Kind);
        Item.noUseGraphic = true;
    }
    public override string GetSkillActivateKeyHint() => string.IsNullOrEmpty(ArknightsKeybinds.SkillActivateKeyDisplay)
        ? Language.GetTextValue("Mods.ArknightsMod.ChargedArsenal.Unbound")
        : Language.GetTextValue("Mods.ArknightsMod.ChargedArsenal.SkillKey", ArknightsKeybinds.SkillActivateKeyDisplay);
    public override void ModifyTooltips(System.Collections.Generic.List<TooltipLine> tooltips)
    {
        base.ModifyTooltips(tooltips);
        tooltips.Add(new TooltipLine(Mod, "ChargedSkillKey", GetSkillActivateKeyHint()));
        if (Kind is not (ChargedOperatorKind.Melantha or ChargedOperatorKind.Matoimaru))
            tooltips.Add(new TooltipLine(Mod, "ChargedPlaceholder", Language.GetTextValue("Mods.ArknightsMod.ChargedArsenal.Placeholder")));
    }
    // Right hold is polled in HoldItem; left fire uses Terraria's normal item animation.
    public override bool CanUseItem(Player player) => !ArknightsKeybinds.SkillActivatePressed(player)
        && (Melee || player.altFunctionUse != 2 && !Main.mouseRight && WorldInputAllowed(player))
        && (Melee || player.GetModPlayer<ChargedOperatorPlayer>().Recovery == 0)
        && player.ownedProjectileCounts[Melee ? ModContent.ProjectileType<DreadnoughtSwing>() : ModContent.ProjectileType<ChargedOperatorHoldout>()] == 0;
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (!Melee)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                int skill = ChargedOperatorPlayer.ActiveSkill(player);
                Vector2 aim = velocity.SafeNormalize(Vector2.UnitX * player.direction);
                Vector2 muzzle = player.MountedCenter + Collision.TileCollision(player.MountedCenter - new Vector2(3), aim * 50, 6, 6, true, true);
                int index = Projectile.NewProjectile(source, muzzle, aim * 22, ModContent.ProjectileType<ChargedOperatorShot>(),
                    Math.Max(1, (int)(damage * ChargedOperatorPlayer.DamageMultiplier(Kind, skill))), knockback, player.whoAmI, (int)Kind, 0, skill);
                if (Main.projectile.IndexInRange(index)) Main.projectile[index].CritChance = player.GetWeaponCrit(Item);
                ChargedSniperVisuals.MuzzleBurst(muzzle, aim, Kind, false);
            }
            return false;
        }
        if (player.whoAmI == Main.myPlayer)
            Projectile.NewProjectile(source, player.MountedCenter, velocity.SafeNormalize(Vector2.UnitX * player.direction),
                ModContent.ProjectileType<DreadnoughtSwing>(), damage, knockback, player.whoAmI, (int)Kind);
        return false;
    }
    public override Vector2? HoldoutOffset() => Melee ? null : new Vector2(-10, 8);
    public override void UseItemFrame(Player player)
    {
        if (Melee || Item.noUseGraphic) return;
        if (player.whoAmI != Main.myPlayer)
        {
            // Cache the received aim once per animation; never feed recoil back into next frame's aim.
            if (lastRemoteAnimation == 0 || player.itemAnimation > lastRemoteAnimation)
                remoteAim = (player.itemRotation + (player.direction < 0 ? MathHelper.Pi : 0)).ToRotationVector2();
            lastRemoteAnimation = player.itemAnimation;
        }
        Vector2 aim = player.whoAmI == Main.myPlayer
            ? (Main.MouseWorld - player.Center).SafeNormalize(Vector2.UnitX * player.direction)
            : remoteAim.SafeNormalize(Vector2.UnitX * player.direction);
        if (player.whoAmI == Main.myPlayer) player.ChangeDir(aim.X < 0 ? -1 : 1);
        float progress = 1 - player.itemTime / (float)Math.Max(1, player.itemTimeMax);
        float rotation = (-aim).ToRotation() * player.gravDir + MathHelper.PiOver2;
        if (progress < .5f) rotation -= .45f * MathF.Pow((.5f - progress) / .5f, 2) * player.direction;
        player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation);
        if (progress > .5f)
            player.SetCompositeArmBack(true, MathF.Sin(MathHelper.Pi * (progress - .5f) / .36f) > .5f
                ? Player.CompositeArmStretchAmount.Full : Player.CompositeArmStretchAmount.Quarter, rotation + .52f * player.direction);
    }
    public override void UseStyle(Player player, Rectangle heldItemFrame)
    {
        if (Melee || Item.noUseGraphic) return;
        float rotation = player.compositeFrontArm.rotation + MathHelper.PiOver2 * player.gravDir;
        Vector2 position = player.MountedCenter + rotation.ToRotationVector2() * 35;
        // Same center-anchor construction as Nitro's CleanHoldStyle, using this item's real dimensions.
        player.itemRotation = rotation + (player.direction < 0 ? MathHelper.Pi : 0);
        Vector2 origin = new Vector2(-5 * player.direction, 6 * player.gravDir);
        Vector2 size = heldItemFrame.Size();
        player.itemLocation = position - size / 2
            + player.itemRotation.ToRotationVector2() * (-size.X / 2 - 10) * player.direction
            - origin.RotatedBy(player.itemRotation) + new Vector2(size.X / 2, 0);
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
