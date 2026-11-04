using System;
using ArknightsMod.Content.Items.BossArsenal;
using ArknightsMod.Content.Projectiles.Guard.Reaper.Phoenix;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.GameContent;
using Microsoft.Xna.Framework.Graphics;

namespace ArknightsMod.Content.Projectiles.BossArsenal;

public sealed class BossArsenalHoldout : ModProjectile
{
    private Item bound;
    private ReaperSwarmVisualNode charge;
    internal int Kind => (int)Projectile.ai[0];
    internal int Clock => (int)Projectile.ai[1];
    internal int Cycle => Kind == 2 ? 54 : 126;
    public override string Texture => "Terraria/Images/MagicPixel";
    public override void SetDefaults() { Projectile.width = Projectile.height = 4; Projectile.timeLeft = 2; Projectile.netImportant = true; Projectile.tileCollide = false; }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead || owner.CCed || owner.noItems || owner.silence
            || owner.HeldItem.ModItem is not BossArsenalWeapon item || item.Kind != Kind) { Projectile.Kill(); return; }
        bool local = Projectile.owner == Main.myPlayer;
        if (local && (!owner.channel || bound != null && !ReferenceEquals(bound, owner.HeldItem))) { Projectile.Kill(); return; }
        bound ??= owner.HeldItem;
        if (local)
        {
            Vector2 aim = (Main.MouseWorld - owner.MountedCenter).SafeNormalize(Vector2.UnitX * owner.direction);
            if (Vector2.DistanceSquared(aim, Projectile.velocity) > .0001f) { Projectile.velocity = aim; Projectile.netUpdate = true; }
            if (Clock > 0 && Clock % Cycle == 0 && !owner.CheckMana(bound, -1, true)) { Projectile.Kill(); return; }
        }
        Vector2 axis = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        Projectile.Center = owner.MountedCenter + axis * 32; Projectile.timeLeft = 2;
        owner.ChangeDir(axis.X < 0 ? -1 : 1); owner.heldProj = Projectile.whoAmI;
        owner.itemTime = owner.itemAnimation = 2; owner.itemRotation = (axis * owner.direction).ToRotation();
        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, axis.ToRotation() - MathHelper.PiOver2);
        int phase = Clock % Cycle;
        if (Kind == 10 && !Main.dedServ && phase == 0)
            charge = ReaperSwarmVisuals.Spawn("emitter.player.weapon.74.charge.core", () => Projectile.Center, () => Projectile.velocity,
                active: () => Projectile.active && Clock % Cycle < 60, style: new PhoenixFxStyle { SuppressShake = true });
        if (local && phase == (Kind == 2 ? 0 : 60))
        {
            charge?.Stop();
            var beam = Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, axis,
                ModContent.ProjectileType<BossArsenalBeam>(), Projectile.damage, Projectile.knockBack, Projectile.owner, Kind);
            beam.CritChance = Projectile.CritChance;
        }
        Projectile.ai[1]++;
    }
    public override void OnKill(int timeLeft) => charge?.Stop();
    public override bool PreDraw(ref Color lightColor)
    {
        Player owner = Main.player[Projectile.owner];
        Texture2D texture = TextureAssets.Item[owner.HeldItem.type].Value;
        Vector2 axis = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        Main.EntitySpriteDraw(texture, owner.MountedCenter + axis * 8 - Main.screenPosition, null, lightColor,
            axis.ToRotation(), new Vector2(0, texture.Height * .5f), 1,
            owner.direction < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None);
        return false;
    }
}

public sealed class BossArsenalBeam : ModProjectile
{
    private PhoenixBeamFx beam;
    private ReaperSwarmVisualNode muzzle;
    private int firstTarget = -1;
    internal int Kind => (int)Projectile.ai[0];
    internal float Length { get; private set; }
    internal int DamageInterval => Kind == 10 ? 10 : 8;
    public override string Texture => "Terraria/Images/MagicPixel";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 4; Projectile.friendly = true; Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = -1; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true; Projectile.netImportant = true;
    }
    public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
    { Projectile.timeLeft = Kind == 10 ? 48 : 30; Projectile.localNPCHitCooldown = DamageInterval; }
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead) { Projectile.Kill(); return; }
        if (Kind == 2)
        {
            if (!owner.channel || owner.HeldItem.ModItem is not ScarletMeson) { Projectile.Kill(); return; }
            if (Projectile.owner == Main.myPlayer)
            {
                Vector2 aim = (Main.MouseWorld - owner.MountedCenter).SafeNormalize(Projectile.velocity);
                if (Vector2.DistanceSquared(aim, Projectile.velocity) > .0001f) { Projectile.velocity = aim; Projectile.netUpdate = true; }
            }
            Projectile.Center = owner.MountedCenter + Projectile.velocity * 32;
        }
        // Inferno Laser retains its birth position and direction after release.
        Vector2 axis = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        Length = 0;
        int range = Kind == 2 ? 1920 : 1440;
        for (int distance = 8; distance <= range; distance += 8)
        {
            if (Collision.SolidCollision(Projectile.Center + axis * distance - new Vector2(2), 4, 4)) break;
            Length = distance;
        }
        firstTarget = -1;
        if (Kind == 2) {
            // Clip against the first enemy's near face, not its center or slot order.
            float nearest = Length;
            foreach (NPC npc in Main.ActiveNPCs) {
                // Target dummies are immortal, so CanBeChasedBy deliberately rejects
                // them even though ordinary player weapons must be able to hit them.
                if (!npc.CanBeChasedBy(Projectile) && npc.type != Terraria.ID.NPCID.TargetDummy) continue;
                if (ArsenalBeamGeometry.Entry(Projectile.Center, axis, npc.Hitbox, Length, 5, out float entry)
                    && (firstTarget < 0 || entry < nearest)) { nearest = entry; firstTarget = npc.whoAmI; }
            }
            if (firstTarget >= 0) Length = Math.Min(Length, nearest + 2);
        }
        if (Main.dedServ) return;
        if (beam == null)
        {
            var style = Kind == 2 ? PhoenixFx.Colored(new Color(255, 30, 45), new Color(255, 170, 170)) : PhoenixFx.Original();
            style.SuppressShake = true;
            if (Kind == 2) {
                style.MonochromeTexture = true;
                // Keep impact graphics, move their sound to the start of every firing pulse.
                style.SoundVolume = 0;
                var fireStyle = PhoenixFx.Original(); fireStyle.SoundVolume = .65f; fireStyle.SuppressShake = true;
                PhoenixFx.Burst("soundemitter.player.34.hit", Projectile.Center, axis, fireStyle);
            }
            else PhoenixFx.Burst("soundemitter.player.74.hit", Projectile.Center, axis, style);
            beam = new PhoenixBeamFx(Kind == 2 ? "projectile.player.34.1" : "projectile.player.74",
                () => Projectile.Center, () => Projectile.Center + Projectile.velocity * Length,
                () => Projectile.active, range, style);
            if (Kind == 2) muzzle = ReaperSwarmVisuals.Spawn("actor.player.weapon.34.muzzle", () => Projectile.Center,
                () => Projectile.velocity, active: () => Projectile.active, style: style);
        }
        beam.Extend(); Lighting.AddLight(Projectile.Center, BossArsenalFx.Tint(Kind).ToVector3() * .7f);
    }
    public override bool? CanHitNPC(NPC target) => Kind == 2 && target.whoAmI != firstTarget ? false : null;
    public override bool? Colliding(Rectangle a, Rectangle b)
    {
        if (Kind == 2) {
            if (firstTarget < 0) return false;
            // Use the same slab test as target selection. A vanilla line's endpoint
            // test can reject a beam visually clipped to the expanded near face.
            return ArsenalBeamGeometry.Entry(Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.UnitX), b, Length, 5, out _);
        }
        float at = 0;
        return Collision.CheckAABBvLineCollision(b.TopLeft(), b.Size(), Projectile.Center,
            Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX) * Length, Kind == 10 ? 22 : 10, ref at);
    }
    internal Vector2 HitPosition(Vector2 target) => Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX)
        * Math.Clamp(Vector2.Dot(target - Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.UnitX)), 0, Length);
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        => beam?.HitAlong(Vector2.Distance(Projectile.Center, HitPosition(target.Center)));
    public override void OnKill(int timeLeft) { beam?.Stop(); muzzle?.Stop(); }
    public override bool PreDraw(ref Color lightColor) => false;
}
