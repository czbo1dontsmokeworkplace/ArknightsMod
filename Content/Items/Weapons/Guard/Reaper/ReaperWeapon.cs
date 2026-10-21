using ArknightsMod.Content;
using ArknightsMod.Content.Projectiles.Guard.Reaper;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Guard.Reaper;

public abstract class ReaperWeapon : ExpansionWeaponBase
{
    public abstract ReaperKind Kind { get; }
    public override string Texture => ReaperRules.Art(Kind);
    public override void SetDefaults()
    {
        Item.damage = EliteDamage[0]; Item.DamageType = DamageClass.Melee;
        Item.width = Item.height = 70; Item.knockBack = 5 + (int)Kind;
        Item.useTime = Item.useAnimation = ReaperRules.SwingTicks;
        Item.value = Item.sellPrice(silver: 300 + 250 * (int)Kind);
        Item.rare = Kind switch { ReaperKind.Humus => ItemRarityID.LightRed, ReaperKind.LaPluma => ItemRarityID.Lime, _ => ItemRarityID.Red };
        Item.shootSpeed = 14; Item.shoot = ModContent.ProjectileType<ReaperSwing>();
        Item.useStyle = ItemUseStyleID.Shoot; Item.noMelee = Item.noUseGraphic = true;
        Item.autoReuse = true; Item.channel = false;
    }
    public override bool AltFunctionUse(Player player) => false;
    public override void HoldItem(Player player)
    {
        base.HoldItem(player);
        var state = player.GetModPlayer<ReaperPlayer>();
        if (player.whoAmI != Main.myPlayer || !state.RightHeld || state.SuppressAttacks || player.noItems || player.CCed) return;
        ReaperSwing.Find(player)?.Projectile.Kill();
        if (ReaperSpin.Find(player) == null)
            Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.MountedCenter,
                new Vector2(Main.MouseWorld.X >= player.Center.X ? 1 : -1, 0), ModContent.ProjectileType<ReaperSpin>(),
                player.GetWeaponDamage(Item), player.GetWeaponKnockback(Item), player.whoAmI, (int)Kind, player.selectedItem);
    }
    public override bool CanUseItem(Player player)
    {
        if (ArknightsKeybinds.SkillActivatePressed(player)) { player.GetModPlayer<ReaperPlayer>().TryActivate(); return false; }
        var state = player.GetModPlayer<ReaperPlayer>();
        return player.altFunctionUse != 2 && !state.RightHeld && !state.SuppressAttacks
            && ReaperSwing.Find(player) == null && ReaperSpin.Find(player) == null && base.CanUseItem(player);
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI == Main.myPlayer && !player.GetModPlayer<ReaperPlayer>().RightHeld && ReaperSwing.Find(player) == null)
            Projectile.NewProjectile(source, player.MountedCenter, velocity.SafeNormalize(new Vector2(player.direction, 0)),
                ModContent.ProjectileType<ReaperSwing>(), damage, knockback, player.whoAmI, (int)Kind, player.selectedItem);
        return false;
    }
    public override float UseSpeedMultiplier(Player player) => player.GetModPlayer<ReaperPlayer>().AttackSpeed;
    public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
    {
        base.ModifyWeaponDamage(player, ref damage); damage *= player.GetModPlayer<ReaperPlayer>().DamageMultiplier;
    }
    public override void ModifyHitNPC(Player player, NPC target, ref NPC.HitModifiers modifiers)
    {
        modifiers.SourceDamage *= player.GetModPlayer<ReaperPlayer>().AttackPower;
        player.GetModPlayer<ReaperPlayer>().ModifyCut(target, ref modifiers);
    }
    public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
    {
        player.GetModPlayer<ReaperPlayer>().OnRightHit(target, damageDone);
        ReaperVisuals.Hit(target.Center,Kind,.7f);
    }
}
public sealed class HumusScythe : ReaperWeapon
{
    public override ReaperKind Kind => ReaperKind.Humus;
    protected override int[] EliteDamage => [42, 51, 60];
    public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.Sickle)
        .AddRecipeGroup(OperatorWeaponRecipeGroups.CobaltOrPalladiumBar, 14).AddIngredient(ItemID.SoulofLight, 6).AddIngredient(ItemID.Chain, 3).AddTile(TileID.Anvils).Register();
}
public sealed class LaPlumaScythe : ReaperWeapon
{
    public override ReaperKind Kind => ReaperKind.LaPluma;
    protected override int[] EliteDamage => [60, 75, 90];
    public override void AddRecipes() => CreateRecipe().AddIngredient<HumusScythe>()
        .AddIngredient(ItemID.ChlorophyteBar, 14).AddIngredient(ItemID.Ectoplasm, 6).AddTile(TileID.MythrilAnvil).Register();
}
