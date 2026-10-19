using System.IO;
using System;
using System.Collections.Generic;
using ArknightsMod.Content.Items.Weapons.Defender.Horn;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Defender.Horn;

public sealed class HornGuard : ModProjectile
{
    private byte visualState;
    private float shieldRaise;
    internal float ShieldRaise => MathHelper.SmoothStep(0f, 1f, shieldRaise);
    public override void SendExtraAI(BinaryWriter writer) => writer.Write(visualState);
    public override void ReceiveExtraAI(BinaryReader reader) => visualState = reader.ReadByte();
    public override string Texture => "ArknightsMod/Content/Items/Weapons/Defender/Horn/HornMortar";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 24;
        Projectile.tileCollide = false;
        Projectile.timeLeft = 2;
        Projectile.hide = true;
    }
    public override bool? CanDamage() => false;
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead || owner.HeldItem.ModItem is not HornGrenadeLauncher || owner.CCed || owner.noItems)
        { Projectile.Kill(); return; }
        Projectile.timeLeft = 2;
        if (Projectile.owner == Main.myPlayer)
        {
            byte nextState = owner.GetModPlayer<HornLauncherPlayer>().VisualState;
            if (nextState != visualState) { visualState = nextState; Projectile.netUpdate = true; }
            float angle = (Main.MouseWorld - owner.MountedCenter).SafeNormalize(new Vector2(owner.direction, 0)).ToRotation();
            float guard = ShieldGuardRules.IsRaised(owner) ? 1f : 0f;
            if (MathF.Abs(MathHelper.WrapAngle(angle - Projectile.ai[0])) > .04f || guard != Projectile.ai[1])
            { Projectile.ai[0] = angle; Projectile.ai[1] = guard; Projectile.netUpdate = true; }
        }
        Vector2 aim = Projectile.ai[0].ToRotationVector2();
        owner.ChangeDir(aim.X >= 0 ? 1 : -1);
        owner.heldProj = Projectile.whoAmI;
        // A reversible 20-frame smoothstep carries the shield around the shoulder.
        shieldRaise = MathHelper.Clamp(shieldRaise + (Projectile.ai[1] == 1f ? 1f : -1f) / 20f, 0f, 1f);
        float raised = ShieldRaise;
        Projectile.Center = owner.MountedCenter + new Vector2(
            owner.direction * MathHelper.Lerp(-15f, 24f, raised),
            (1f - 10f * MathF.Sin(raised * MathHelper.Pi)) * owner.gravDir);
        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.ai[0] - MathHelper.PiOver2);
        if (Projectile.ai[2] > 0) Projectile.ai[2]--;
        HornVisuals.SkillBody(owner, visualState, ++Projectile.localAI[0]);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Player owner = Main.player[Projectile.owner];
        HornVisuals.SkillBodyGlow(owner, visualState, Projectile.localAI[0]);
        Texture2D texture = TextureAssets.Projectile[Type].Value;
        Vector2 aim = Projectile.ai[0].ToRotationVector2();
        Vector2 gunCenter = owner.MountedCenter + aim * (19f - Projectile.ai[2] * .5f);
        // The supplied mortar sprite droops by about 25 degrees; rotate it upward so its muzzle follows the aim vector.
        float mortarRotation = Projectile.ai[0] - MathHelper.ToRadians(25f)
            + (owner.direction < 0 ? MathHelper.ToRadians(50f) : 0f);
        Main.EntitySpriteDraw(texture, gunCenter - Main.screenPosition, null, lightColor, mortarRotation,
            texture.Size() * .5f, 1f, owner.direction < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None);
        return false;
    }
    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
        List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => overPlayers.Add(index);
}

