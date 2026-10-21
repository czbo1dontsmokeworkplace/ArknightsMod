using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace ArknightsMod.Content.Items.Evolution.Arsenal;

public sealed partial class NestGuardian
{
    private bool Grounded => Projectile.velocity.Y >= 0 &&
        Collision.TileCollision(Projectile.position, new Vector2(0, 3), Projectile.width, Projectile.height, FallingThrough(), false).Y < 2.9f;

    private bool FallingThrough()
    {
        float goalBottom = Enemy?.Bottom.Y ?? Main.player[Projectile.owner].Bottom.Y;
        return goalBottom > Projectile.Bottom.Y + 56;
    }
    public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
    {
        fallThrough = FallingThrough(); return true;
    }
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        // Native tile collision has already resolved position/velocity; never destroy the minion.
        if (Math.Abs(oldVelocity.X - Projectile.velocity.X) > .01f && dashActive)
        {
            dashActive = false; dashVelocity = Vector2.Zero; Projectile.velocity.X = 0;
            Projectile.netUpdate = true;
            if (!Main.dedServ) EvolutionWeaponVisuals.Sparks(Projectile.Center + new Vector2(Projectile.spriteDirection * 30, 12), 6, 2);
        }
        if (oldVelocity.Y > 3 && Math.Abs(Projectile.velocity.Y) < .01f && !Main.dedServ)
            EvolutionWeaponVisuals.Sparks(Projectile.Bottom, 4, 1.5f);
        return false;
    }
    internal static bool TryFindGround(Vector2 requested, out Vector2 grounded)
    {
        grounded = requested;
        for (int down = 0; down <= 640; down += 8)
        {
            Vector2 center = requested + new Vector2(0, down), position = center - new Vector2(32, 25);
            if (position.X < 16 || position.Y < 16 || position.X + 64 > Main.maxTilesX * 16 - 16 || position.Y + 58 > Main.maxTilesY * 16 - 16) continue;
            if (Collision.SolidCollision(position, 64, 50)) continue;
            float fall = Collision.TileCollision(position, new Vector2(0, 8), 64, 50).Y;
            if (fall >= 7.9f) continue;
            grounded = center + new Vector2(0, fall); return true;
        }
        return false;
    }
    private bool GroundAhead(int direction, float reach)
    {
        Vector2 probe = new(Projectile.Center.X + direction * (Projectile.width * .5f + reach) - 3, Projectile.Bottom.Y - 6);
        return Collision.TileCollision(probe, new Vector2(0, 24), 6, 6, FallingThrough(), false).Y < 23.9f;
    }
    private bool WallAhead(int direction)
        => Math.Abs(Collision.TileCollision(Projectile.position, new Vector2(direction * 14, 0), Projectile.width, Projectile.height).X) < 13.9f;

    private void GroundMovement(Player owner, NPC enemy, Vector2 idle, int tick, bool authority)
    {
        if (jumpCooldown > 0) jumpCooldown--;
        bool grounded = Grounded;
        float deltaX = (enemy?.Center.X ?? idle.X) - Projectile.Center.X;
        int direction = deltaX < 0 ? -1 : 1;
        bool dashWindow = EvolutionArsenalRules.GuardianDash(tick);
        if (!dashWindow || enemy == null) dashActive = false;
        if (!dashActive && enemy != null && grounded && tick >= 42 && tick <= 45 && !WallAhead(direction) && GroundAhead(direction, 52))
        {
            dashActive = true; dashVelocity = new Vector2(direction * 29, 0);
            Projectile.velocity.Y = 0; Projectile.spriteDirection = direction;
            if (authority) Projectile.netUpdate = true;
            if (!Main.dedServ) SoundEngine.PlaySound(SoundID.Item71 with { Volume = .5f, Pitch = -.35f }, Projectile.Center);
        }
        if (dashActive)
        {
            direction = dashVelocity.X < 0 ? -1 : 1;
            // Do not turn to track the target, charge through a wall, or launch off a cliff.
            if (WallAhead(direction) || !GroundAhead(direction, 44))
            {
                dashActive = false; dashVelocity = Vector2.Zero; Projectile.velocity.X = 0;
                if (authority) Projectile.netUpdate = true;
            }
            else { Projectile.velocity.X = dashVelocity.X; Projectile.spriteDirection = direction; }
        }
        else
        {
            float standOff = enemy == null ? 24 : 82;
            bool moving = Math.Abs(deltaX) > standOff;
            float desired = moving ? direction * (enemy == null ? 8.5f : 11.5f) : 0;
            if (enemy != null && tick >= 14 && tick <= 34 && Math.Abs(deltaX) < 175) desired = 0;
            Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, desired, grounded ? .24f : .08f);
            if (enemy != null || Math.Abs(Projectile.velocity.X) > .4f) Projectile.spriteDirection = direction;
            float goalBottom = enemy?.Bottom.Y ?? owner.Bottom.Y;
            bool elevated = goalBottom < Projectile.Bottom.Y - 50 && Math.Abs(deltaX) < 260;
            bool obstacle = moving && (WallAhead(direction) || !GroundAhead(direction, 55));
            if (grounded && jumpCooldown == 0 && (elevated || obstacle))
            {
                float height = Math.Clamp(Projectile.Bottom.Y - goalBottom + 40, 68, 170);
                Projectile.velocity.Y = -Math.Clamp(MathF.Sqrt(2 * .65f * height), 9.5f, 15);
                Projectile.velocity.X = direction * Math.Max(5.5f, Math.Abs(Projectile.velocity.X));
                jumpCooldown = 24;
                if (authority) Projectile.netUpdate = true;
            }
        }
        Projectile.velocity.Y = Math.Min(18, Projectile.velocity.Y + .65f);
    }
}
