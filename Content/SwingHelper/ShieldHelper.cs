using ArknightsMod.Common.VisualEffects;
using ArknightsMod.Content.Projectiles.Defender;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.SwingHelper
{
    public sealed class ShieldHelper
    {
        private readonly Vertex[] vertices = new Vertex[6];
        public Defender_Player mp;
        public void SetDefaults(Projectile projectile, int localNpcHitCooldown = 0)
        {
            projectile.width = 60;
            projectile.height = 36;
            projectile.friendly = true;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.usesLocalNPCImmunity = true;
            projectile.ownerHitCheck = true;
            projectile.DamageType = DamageClass.MeleeNoSpeed;
            projectile.ignoreWater = true;
            projectile.localNPCHitCooldown = localNpcHitCooldown;
        }

        public void UpdateMovePose(Projectile projectile, Player player)
        {
            mp = player.GetModPlayer<Defender_Player>();
            player.heldProj = projectile.whoAmI;
            // Keep the shield anchored to the same relaxed arm pose at every movement speed.
            const float armRotation = 0f;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
            projectile.rotation = armRotation;
            projectile.Center = player.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, armRotation);
        }

        public void UpdateDefenderPose(Projectile projectile, Player player, float? synchronizedAim = null)
        {
	        mp = player.GetModPlayer<Defender_Player>();
	        mp.OpenDefender = true;
            player.heldProj = projectile.whoAmI;
            float mouseRotation = synchronizedAim ?? (Main.MouseWorld - player.MountedCenter).ToRotation();
            player.direction = MathF.Cos(mouseRotation) >= 0 ? 1 : -1;
            projectile.rotation = mouseRotation - MathHelper.PiOver2 + MathHelper.PiOver2 * player.direction;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, projectile.rotation);
            projectile.Center = player.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, projectile.rotation)
                + new Vector2(20f, 0f).RotatedBy(projectile.rotation) * player.direction;
        }

        public void DrawMainWeapon(Player player, Texture2D texture, Vector2 handPosition, float rotation, Color color, float? weaponLength = null)
        {
            // Cardigan's diagonal weapon mesh and 12px grip offset, shared by every melee defender.
            float length = weaponLength ?? texture.Size().Length();
            if (texture.Height > texture.Width * 2)
            {
                // Nian's vertical sprite has its grip at the top: preserve its aspect ratio.
                Main.EntitySpriteDraw(texture, handPosition - Main.screenPosition, null, color,
                    rotation - MathHelper.PiOver2, new Vector2(texture.Width * .5f, 8f), length / texture.Height,
                    player.direction < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
                return;
            }
            Vector2 start = handPosition - new Vector2(12f, 0f).RotatedBy(rotation) - Main.screenPosition;
            Vector2 half = rotation.ToRotationVector2() * length * .5f;
            Vector2 across = new Vector2(-half.Y, half.X) * player.direction;
            vertices[0] = new Vertex(start + half - across, new Vector3(0, 0, 1), color);
            vertices[1] = vertices[5] = new Vertex(start + half * 2f, new Vector3(1, 0, 1), color);
            vertices[2] = vertices[4] = new Vertex(start, new Vector3(0, 1, 1), color);
            vertices[3] = new Vertex(start + half + across, new Vector3(1, 1, 1), color);
            Main.graphics.GraphicsDevice.Textures[0] = texture;
            Main.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices, 0, vertices.Length / 3);
        }

        public void DrawShield(Projectile projectile, Player player, Texture2D texture, bool defending)
        {
            Vector2 center = projectile.Center - Main.screenPosition;
            Vector2 halfWidth = new Vector2(texture.Width * 0.5f, 0f).RotatedBy(projectile.rotation);
            Vector2 halfHeight = new Vector2(0f, texture.Height * 0.5f).RotatedBy(projectile.rotation);
            if (defending)
                halfWidth *= 0.8f;

            Vector2[] pos =
            {
                center + halfHeight + halfWidth * player.direction,
                center + halfHeight - halfWidth * player.direction,
                center - halfHeight - halfWidth * player.direction,
                center - halfHeight + halfWidth * player.direction
            };

            vertices[0] = new Vertex(pos[1], new Vector3(0f, 1f, 1f), Color.White);
            vertices[1] = vertices[5] = new Vertex(pos[0], new Vector3(1f, 1f, 1f), Color.White);
            vertices[2] = vertices[4] = new Vertex(pos[2], new Vector3(0f, 0f, 1f), Color.White);
            vertices[3] = new Vertex(pos[3], new Vector3(1f, 0f, 1f), Color.White);

            Main.graphics.GraphicsDevice.Textures[0] = texture;
            Main.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices, 0, vertices.Length / 3);
        }
    }
}
