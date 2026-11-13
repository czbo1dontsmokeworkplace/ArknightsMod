using System;
using System.IO;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using ArknightsMod.Content.Items.Weapons.Charged;
using ArknightsMod.Content.Projectiles.Sniper.Typhon;
namespace ArknightsMod.Content.Items.Weapons.Sniper.Archers;

public sealed class ArcherCharge : GlobalItem
{
    internal static int Tier(Item item) => item.ModItem?.Name switch {
        "RangersBow" => 1, "MeteorBow" => 2, "PlatinumBow" => 3, "ArchettoBow" => 4, "TyphonBow" => 5, _ => 0
    };
    internal static bool Supports(Item item) => item.ModItem?.Mod.Name == "ArknightsMod" && Tier(item) > 0;
    public override bool AltFunctionUse(Item item, Player player) => Supports(item);
    public override bool CanUseItem(Item item, Player player) => !Supports(item) ||
        (player.altFunctionUse != 2 && !Main.mouseRight && player.ownedProjectileCounts[ModContent.ProjectileType<TyphonMagazine>()] == 0);
    public override void HoldItem(Item item, Player player) {
        if (!Supports(item) || Tier(item) == 5) return; // Typhon keeps its existing item-bound input guard.
        item.noUseGraphic = player.ownedProjectileCounts[ModContent.ProjectileType<TyphonMagazine>()] > 0;
        if (player.whoAmI != Main.myPlayer) return;
        var input = player.GetModPlayer<ChargedOperatorPlayer>();
        if (!Main.mouseRight) input.SuppressSniperRight = false;
        else if (!ChargedOperatorWeapon.WorldInputAllowed(player) || player.tileInteractionHappened) input.SuppressSniperRight = true;
        if (input.SuppressSniperRight || !Main.mouseRight || item.noUseGraphic || player.itemAnimation > 0
            || !ChargedOperatorWeapon.WorldInputAllowed(player) || player.tileInteractionHappened
            || global::ArknightsMod.Content.ArknightsKeybinds.SkillActivatePressed(player) || !player.HasAmmo(item)) return;
        Projectile.NewProjectile(player.GetSource_ItemUse(item), player.MountedCenter,
            (Main.MouseWorld-player.MountedCenter).SafeNormalize(Vector2.UnitX*player.direction),
            ModContent.ProjectileType<TyphonMagazine>(),0,0,player.whoAmI);
        item.noUseGraphic = true;
    }
    // PickAmmo is called exactly once at the completed insertion, not on every held frame.
    internal static bool Pick(Item item, Player player, out int damage, out float knockback, out int ammo, out int type, out float speed)
        => player.PickAmmo(item, out type, out speed, out damage, out knockback, out ammo);
    internal static int Resolve(int type, int tier) => ModContent.ProjectileType<ArcherChargedArrow>();
}

public sealed class ArcherChargedArrow : ModProjectile
{
    public const float DamageMultiplier = 3, SpeedMultiplier = 1.6f, LaunchSpeed = 32;
    public const int ArtLength = 82;
    private int age;
    public override string Texture => "ArknightsMod/Content/Projectiles/Sniper/Typhon/TyphonArrow";
    public override void SetStaticDefaults() {
        ProjectileID.Sets.TrailCacheLength[Type] = 18;
        ProjectileID.Sets.TrailingMode[Type] = 0;
    }
    public override void SetDefaults() {
        Projectile.width = Projectile.height = 14;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Ranged;
        Projectile.arrow = true; Projectile.penetrate = 1;
        Projectile.tileCollide = true; Projectile.ignoreWater = true;
        Projectile.timeLeft = 240; Projectile.extraUpdates = 2;
        Projectile.scale = TyphonAstralShot.LeftScale;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override void AI() {
        if (Projectile.numUpdates == 0) age++;
        if (Projectile.ai[0] > 0 && age >= 3) {
            NPC target = Projectile.FindTargetWithinRange(Projectile.ai[0] >= 4 ? 800 : 480, true);
            if (target != null) {
                float angle = Projectile.velocity.ToRotation().AngleTowards((target.Center - Projectile.Center).ToRotation(),
                    (.018f + Projectile.ai[0] * .004f) / Projectile.MaxUpdates);
                Projectile.velocity = angle.ToRotationVector2() * Projectile.velocity.Length();
            }
        }
        Projectile.rotation = Projectile.velocity.ToRotation();
        if (Projectile.numUpdates == 0 && !Main.dedServ) TyphonAstralVisuals.Flight(Projectile);
    }
    public override bool? Colliding(Rectangle a, Rectangle b) {
        float point = 0;
        return Collision.CheckAABBvLineCollision(b.TopLeft(), b.Size(), Projectile.Center - Projectile.velocity,
            Projectile.Center, 14, ref point);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
        Projectile.ai[1] = target.whoAmI + 1;
        Projectile.Kill();
    }
    public override void OnKill(int timeLeft) {
        if (Projectile.owner == Main.myPlayer)
            Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<ArcherArrowExplosion>(), Projectile.damage, Projectile.knockBack,
                Projectile.owner, Projectile.ai[1]);
    }
    public override bool PreDraw(ref Color lightColor) {
        TyphonAstralVisuals.DrawTrail(Projectile);
        TyphonAstralVisuals.DrawHead(Projectile.Center, Projectile.rotation, Projectile.scale, Projectile.identity * .17f);
        return false;
    }
}

public sealed class ArcherArrowExplosion : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    public override void SetDefaults() {
        Projectile.width = Projectile.height = 128;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Ranged;
        Projectile.penetrate = -1; Projectile.timeLeft = 2;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override bool ShouldUpdatePosition() => false;
    // Direct impact already dealt the full charged damage to this target.
    public override bool? CanHitNPC(NPC target) => target.whoAmI + 1 == (int)Projectile.ai[0] ? false : null;
    public override void AI() {
        if (Projectile.localAI[0]++ == 0 && !Main.dedServ)
            global::ArknightsMod.Content.Projectiles.Guard.Gavial.GavialVisuals.GrenadeExplosion(Projectile.GetSource_FromAI(), Projectile.Center);
    }
    public override bool PreDraw(ref Color lightColor) => false;
}

// Meteor's empowered shot keeps its chosen ammo body and native behavior.
public sealed class MeteorArrowEnhancement : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    private bool empowered;
    internal void Configure(Projectile projectile) {
        empowered = true;
        projectile.tileCollide = false; projectile.penetrate = 5;
        projectile.usesLocalNPCImmunity = true; projectile.localNPCHitCooldown = -1;
        projectile.netUpdate = true;
    }
    public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone) {
        if (empowered) target.AddBuff(BuffID.BrokenArmor, 300);
    }
    public override void SendExtraAI(Projectile p, BitWriter bits, BinaryWriter writer) {
        writer.Write(empowered);
        if (empowered) writer.Write(p.penetrate);
    }
    public override void ReceiveExtraAI(Projectile p, BitReader bits, BinaryReader reader) {
        empowered = reader.ReadBoolean();
        if (empowered) {
            p.penetrate = Math.Clamp(reader.ReadInt32(), 0, 5);
            p.tileCollide = false; p.usesLocalNPCImmunity = true; p.localNPCHitCooldown = -1;
        }
    }
}
