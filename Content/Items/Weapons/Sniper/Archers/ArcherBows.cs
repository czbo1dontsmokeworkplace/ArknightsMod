using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using ArknightsMod.Players;
using ArknightsMod.Content;
namespace ArknightsMod.Content.Items.Weapons.Sniper.Archers;

internal static class ArcherRoute
{
    internal static bool Converts(int projectile) => projectile == ProjectileID.WoodenArrowFriendly;
}
public abstract class ArcherBow : ExpansionWeaponBase
{
    protected abstract bool Advanced { get; }
    public override string Texture => "Terraria/Images/Item_" + (Advanced ? ItemID.Tsunami : ItemID.Marrow);
    protected override int[] EliteDamage => Advanced ? [100, 118, 138] : [49, 59, 70];
    public override void SetDefaults() {
        Item.damage = EliteDamage[0]; Item.DamageType = DamageClass.Ranged;
        Item.width = 28; Item.height = 56; Item.noMelee = true; Item.autoReuse = true;
        Item.useStyle = ItemUseStyleID.Shoot; Item.useTime = Item.useAnimation = Advanced ? 14 : 17;
        Item.useAmmo = AmmoID.Arrow; Item.shoot = ProjectileID.WoodenArrowFriendly;
        Item.shootSpeed = Advanced ? 19 : 16; Item.knockBack = 3;
        Item.rare = Advanced ? ItemRarityID.Yellow : ItemRarityID.LightRed;
        Item.value = Item.sellPrice(gold: Advanced ? 7 : 3); Item.UseSound = SoundID.Item5;
    }
    public override void HoldItem(Player player) {
        base.HoldItem(player);
        var state = player.GetModPlayer<WeaponPlayer>();
        if (ArknightsKeybinds.SkillActivatePressed(player) && !state.SkillActive && state.CurrentSkill != null && state.StockCount > 0) {
            state.SkillActive = true; state.SkillTimer = 0; state.DelStockCount();
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, player.Center);
        }
        Item.useTime = Item.useAnimation = state.SkillActive ? (Advanced ? 9 : 13) : (Advanced ? 14 : 17);
    }
    public override void ModifyWeaponDamage(Player player, ref StatModifier damage) {
        base.ModifyWeaponDamage(player, ref damage);
        if (player.GetModPlayer<WeaponPlayer>().SkillActive) damage *= Advanced ? 1.15f : 1.4f;
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
        if (player.whoAmI != Main.myPlayer) return false;
        int count = Advanced && player.GetModPlayer<WeaponPlayer>().SkillActive ? 2 : 1;
        for (int i = 0; i < count; i++) {
            Vector2 v = velocity.RotatedBy(count == 1 ? 0 : (i == 0 ? -.035f : .035f));
            Projectile.NewProjectile(source, position, v, type, damage, knockback, player.whoAmI);
        }
        return false;
    }
}
public sealed class PlatinumBow : ArcherBow
{
    protected override bool Advanced => false;
    public override void AddRecipes() => CreateRecipe().AddIngredient<global::ArknightsMod.Content.Items.Weapons.Sniper.Meteor.MeteorBow>()
        .AddRecipeGroup("ArknightsMod:ArcherHardmodeBars", 12).AddIngredient(ItemID.SoulofLight, 8).AddTile(TileID.MythrilAnvil).Register();
}
public sealed class ArchettoBow : ArcherBow
{
    protected override bool Advanced => true;
    public override void AddRecipes() => CreateRecipe().AddIngredient<PlatinumBow>().AddIngredient(ItemID.ChlorophyteBar, 14)
        .AddIngredient(ItemID.Ectoplasm, 8).AddTile(TileID.MythrilAnvil).Register();
}
public sealed class ArcherRecipes : ModSystem
{
    public override void AddRecipeGroups() => RecipeGroup.RegisterGroup("ArknightsMod:ArcherHardmodeBars",
        new RecipeGroup(() => Terraria.Localization.Language.GetTextValue("LegacyMisc.37") + " " + Terraria.Lang.GetItemNameValue(ItemID.CobaltBar), ItemID.CobaltBar, ItemID.PalladiumBar));
}
public sealed class ArcherArrow : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.WoodenArrowFriendly;
    public override void SetStaticDefaults() { ProjectileID.Sets.TrailCacheLength[Type] = 12; ProjectileID.Sets.TrailingMode[Type] = 0; }
    public override void SetDefaults() {
        Projectile.CloneDefaults(ProjectileID.WoodenArrowFriendly); Projectile.aiStyle = -1;
        Projectile.DamageType = DamageClass.Ranged; Projectile.arrow = true; Projectile.timeLeft = 180;
    }
    public override void AI() {
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        if (Projectile.ai[1]++ == 0 && Projectile.ai[0] >= 2) Projectile.penetrate = Projectile.ai[0] >= 3 ? 3 : 2;
        if (Projectile.ai[0] == 2 && Projectile.ai[1] > 12 && Projectile.ai[1] < 55) {
            NPC target = Projectile.FindTargetWithinRange(300, true);
            if (target != null) {
                float angle=Projectile.velocity.ToRotation().AngleTowards((target.Center-Projectile.Center).ToRotation(),.012f);
                Projectile.velocity=angle.ToRotationVector2()*Projectile.velocity.Length();
            }
        }
        if (Projectile.ai[0] == 4) { Projectile.timeLeft=Math.Min(Projectile.timeLeft,40); Projectile.penetrate=1; }
        if (Projectile.velocity.LengthSquared() < 24 * 24) Projectile.velocity *= 1.003f;
        if (!Main.dedServ && Projectile.timeLeft % 4 == 0) {
            var d = Dust.NewDustPerfect(Projectile.Center, DustID.TintableDustLighted, -Projectile.velocity * .06f, 110,
                Projectile.ai[0] >= 3 ? new Color(245, 190, 80) : new Color(130, 220, 185), .65f); d.noGravity = true;
        }
    }
    public override bool OnTileCollide(Vector2 oldVelocity) {
        // One forgiving terrain ricochet for the starter bow; never an infinite bounce.
        if (Projectile.ai[0] != 0 || Projectile.ai[2] != 0) return true;
        Projectile.ai[2] = 1;
        if (Projectile.velocity.X != oldVelocity.X) Projectile.velocity.X = -oldVelocity.X;
        if (Projectile.velocity.Y != oldVelocity.Y) Projectile.velocity.Y = -oldVelocity.Y;
        Projectile.velocity *= .8f; Projectile.damage = Math.Max(1,(int)(Projectile.damage*.8f));
        Projectile.netUpdate = true; return false;
    }
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
        if (Projectile.ai[0] == 2) modifiers.ArmorPenetration += 8;
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
        if (Projectile.ai[0] == 1 && Projectile.ai[2] == 0) {
            Projectile.ai[2] = 1; Projectile.netUpdate = true;
            if (Projectile.owner == Main.myPlayer) {
                Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                for(int side=-1;side<=1;side+=2)
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(),Projectile.Center,forward.RotatedBy(side*.35f)*8,
                        Type,Math.Max(1,Projectile.damage/4),.5f,Projectile.owner,4);
            }
        }
        if (Projectile.ai[0] == 2) Projectile.velocity *= .9f;
    }
    public override bool PreDraw(ref Color lightColor) {
        var tex = Terraria.GameContent.TextureAssets.Projectile[ProjectileID.WoodenArrowFriendly].Value;
        for (int i = 2; i < Projectile.oldPos.Length; i += 2) {
            if (Projectile.oldPos[i] == Vector2.Zero) continue;
            Main.EntitySpriteDraw(tex, Projectile.oldPos[i] + Projectile.Size * .5f - Main.screenPosition, null,
                new Color(120, 215, 185) * (.35f * (1 - i / 12f)), Projectile.rotation, tex.Size() * .5f, Projectile.scale, Microsoft.Xna.Framework.Graphics.SpriteEffects.None);
        }
        return true;
    }
}
