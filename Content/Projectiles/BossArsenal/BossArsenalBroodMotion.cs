using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.BossArsenal;

public sealed partial class BossArsenalMinion
{
    private int broodStage, broodTimer, broodTarget = -1;
    private float broodAngle;
    private Vector2 broodOrigin, broodPrevious;
    private bool BroodCanHit => broodStage switch {
        1 => Math.Max(0, broodTimer - 1) % 56 >= 26,
        2 => Math.Max(0, broodTimer - 1) % 70 >= 28,
        3 => Math.Max(0, broodTimer - 1) % 86 >= 32,
        _ => false
    };
    public override void SendExtraAI(BinaryWriter writer) {
        writer.Write(broodStage); writer.Write(broodTimer); writer.Write(broodTarget); writer.Write(broodAngle);
        writer.Write(broodOrigin.X); writer.Write(broodOrigin.Y);
    }
    public override void ReceiveExtraAI(BinaryReader reader) {
        broodStage = reader.ReadInt32(); broodTimer = reader.ReadInt32(); broodTarget = reader.ReadInt32(); broodAngle = reader.ReadSingle();
        broodOrigin = new Vector2(reader.ReadSingle(), reader.ReadSingle());
    }
    private void UpdateBrood(Player owner, NPC target, Vector2 home) {
        broodPrevious = Projectile.Center;
        bool local = Projectile.owner == Main.myPlayer;
        if (target == null) {
            if (broodStage != 0 && local) Projectile.netUpdate = true;
            broodStage = broodTimer = 0; broodTarget = -1; Projectile.extraUpdates = 0;
            // Preserve the old following perch. There is no orbit around the player.
            Vector2 delta = home - Projectile.Center;
            if (delta.Length() > 300) Projectile.velocity = delta / 30;
            else if (delta.Length() > 100) Projectile.velocity = (Projectile.velocity * 37 + delta.SafeNormalize(Vector2.Zero) * 17) / 40;
            else Projectile.velocity *= .98f;
            Projectile.rotation = Projectile.velocity.X * .025f; return;
        }
        if (local && (broodStage == 0 || broodTarget != target.whoAmI)) {
            broodStage = 1; broodTimer = 0; broodTarget = target.whoAmI; Projectile.netUpdate = true;
        }
        if (broodStage == 0) return;
        Projectile.extraUpdates = 1;
        int cycle = broodStage == 1 ? 56 : broodStage == 2 ? 70 : 86;
        int phase = broodTimer % cycle;
        if (local && phase == 0) {
            broodAngle = Main.rand.NextFloat(-.32f, .32f) + (Main.rand.NextBool() ? MathHelper.Pi : 0);
            broodOrigin = target.Center - Vector2.UnitX.RotatedBy(broodAngle) * 240 + Main.rand.NextVector2Circular(40, 40);
            Projectile.netUpdate = true;
        }
        // Virid Vanguard's normal three attack patterns, rotated by 90 degrees.
        // Only the owner picks the angle and phase; ExtraAI carries those choices.
        if (broodStage == 1) {
            Vector2 axis = Vector2.UnitY.RotatedBy(broodAngle);
            if (phase < 26) {
                Vector2 hover = target.Center - axis * 160 + axis.RotatedBy(MathHelper.PiOver2) * (MathF.Cos(Projectile.identity * 1.7f) * 35);
                Projectile.Center = Vector2.Lerp(Projectile.Center, hover, .08f).MoveTowards(hover, 16);
                Projectile.velocity *= .5f;
            }
            else if (phase == 26) {
                Projectile.velocity = (target.Center + target.velocity * 5 - Projectile.Center).SafeNormalize(axis) * 16;
                if (local) Projectile.netUpdate = true;
            }
        }
        else if (broodStage == 2) {
            float completion = phase / 70f;
            Vector2 rise = -Vector2.UnitY.RotatedBy(MathHelper.PiOver2 + broodAngle) * 200;
            Vector2 start = broodOrigin + rise * Utils.GetLerpValue(0, .4f, completion, true);
            Vector2 offset = target.Center - start;
            float factor = MathHelper.Lerp(1.3f, 2, Projectile.identity / 7f % 1);
            Vector2 end = target.Center + offset.SafeNormalize(Vector2.UnitX) * Math.Clamp(offset.Length(), 60, 180) * factor;
            float pierce = Utils.GetLerpValue(.4f, .72f, completion, true);
            float through = Utils.GetLerpValue(.72f, 1, completion, true);
            Projectile.Center = Vector2.Lerp(Projectile.Center, Vector2.Lerp(start, target.Center, pierce), pierce * .5f);
            if (through > 0) Projectile.Center = Vector2.Lerp(target.Center, end, through);
            Projectile.velocity = Vector2.Zero;
        }
        else {
            Vector2 axis = Vector2.UnitX.RotatedBy(broodAngle);
            if (phase < 32) {
                Vector2 hover = target.Center - axis * 320 + axis.RotatedBy(MathHelper.PiOver2) * (MathF.Cos(Projectile.identity * 1.7f) * 30);
                Projectile.Center = Vector2.Lerp(Projectile.Center, hover, .08f).MoveTowards(hover, 16);
                Projectile.velocity *= .5f;
            }
            else if (phase == 32) {
                Projectile.velocity = (target.Center + target.velocity * 8 - Projectile.Center).SafeNormalize(axis) * 18;
                if (local) Projectile.netUpdate = true;
            }
            if (phase >= 38 && Vector2.Dot(Projectile.Center - target.Center, axis) > 850) {
                Projectile.Center = target.Center - axis * 595; broodPrevious = Projectile.Center;
                if (local) Projectile.netUpdate = true;
            }
        }
        Vector2 facing = Projectile.velocity.LengthSquared() > 1 ? Projectile.velocity : target.Center - Projectile.Center;
        Projectile.rotation = facing.ToRotation() + MathHelper.PiOver2;
        broodTimer++;
        int repeats = broodStage == 1 ? 7 : broodStage == 2 ? 6 : 5;
        if (local && broodTimer >= cycle * repeats) {
            broodStage = broodStage % 3 + 1; broodTimer = 0; Projectile.netUpdate = true;
        }
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
        if (Kind != 11) return null;
        if (!BroodCanHit) return false;
        float along = 0;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), broodPrevious, Projectile.Center, 24, ref along);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
        if (Kind == 11 && Projectile.owner == Main.myPlayer)
            Projectile.NewProjectile(Projectile.GetSource_OnHit(target), target.Center, Vector2.Zero,
                ModContent.ProjectileType<BossArsenalBurst>(), Math.Max(1, Projectile.damage / 2), 1, Projectile.owner, 11, 64);
    }
}
