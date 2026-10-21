using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

// A destruction payload survives carrier round/phase/death cleanup; it has its own short, finite fuse.
public sealed class MaterialistRaptorDeathBomb : ModProjectile
{
    private enum BombState { Falling, Grounded, Attached, Exploding }
    private const int FuseTicks = 180, BlastSize = 32, BlastDamage = 9999;
    private BombState State { get => (BombState)(int)Projectile.ai[0]; set => Projectile.ai[0] = (int)value; }
    private int AttachedPlayer { get => (int)Projectile.ai[1] - 1; set => Projectile.ai[1] = value + 1; }
    private int FuseAge { get => (int)Projectile.ai[2]; set => Projectile.ai[2] = value; }
    private Vector2 attachmentOffset;
    private readonly bool[] struck = new bool[Main.maxPlayers];
    private bool explosionVisual;
    private ulong nextBeep;
    private int lastSecond = -1;

    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.StickyBomb;
    public override void SetDefaults()
    {
        Projectile.width = 14; Projectile.height = 18;
        Projectile.aiStyle = -1;
        Projectile.hostile = true; Projectile.friendly = false;
        Projectile.tileCollide = true; Projectile.ignoreWater = true;
        Projectile.penetrate = -1; Projectile.timeLeft = 600;
        Projectile.netImportant = true;
        CooldownSlot = ImmunityCooldownID.Bosses;
    }

    internal static void Spawn(NPC raptor)
    {
        if (!MaterialistAntagonizer.Authority) return;
        Projectile.NewProjectile(raptor.GetSource_Death(), raptor.Center - new Vector2(0, 24),
            new Vector2(Main.rand.NextFloat(-2.2f, 2.2f), -8),
            ModContent.ProjectileType<MaterialistRaptorDeathBomb>(), BlastDamage, 0, Main.myPlayer);
    }

    public override bool ShouldUpdatePosition() => State == BombState.Falling;
    public override bool? CanDamage() => State == BombState.Exploding ? null : false;
    public override bool CanHitPlayer(Player target) => State == BombState.Exploding && !struck[target.whoAmI];
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
        State == BombState.Exploding && BlastBounds.Intersects(targetHitbox);
    private Rectangle BlastBounds => new((int)Projectile.Center.X - BlastSize / 2,
        (int)Projectile.Center.Y - BlastSize / 2, BlastSize, BlastSize);

    public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
    {
        // A successful explosion hit is exactly 9999, independent of armor/difficulty multipliers.
        // Ordinary immunity/dodge hooks still apply; this is damage, not an unconditional KillMe call.
        modifiers.ModifyHurtInfo += SetBlastDamage;
    }
    private static void SetBlastDamage(ref Player.HurtInfo info) => info.Damage = BlastDamage;
    public override void OnHitPlayer(Player target, Player.HurtInfo info) => struck[target.whoAmI] = true;

    public override void AI()
    {
        FuseAge++;
        Projectile.tileCollide = State == BombState.Falling;
        if (State == BombState.Exploding)
        {
            // Preserve the attached endpoint on each client during the short hit window.
            FollowAttachedPlayer();
            ResizeForBlast();
            if (!Main.dedServ && !explosionVisual) ExplodeVisual();
            if (MaterialistAntagonizer.Authority && FuseAge >= 8) Projectile.Kill();
            return;
        }
        if (State == BombState.Falling)
        {
            Projectile.velocity.Y = Math.Min(12, Projectile.velocity.Y + .24f);
            Projectile.rotation += Projectile.velocity.X * .035f + .035f;
            if (MaterialistAntagonizer.Authority && Projectile.velocity.Y > 0)
                foreach (Player player in Main.ActivePlayers)
                {
                    if (player.dead || player.ghost || !Projectile.Hitbox.Intersects(player.Hitbox)) continue;
                    AttachedPlayer = player.whoAmI;
                    attachmentOffset = Vector2.Clamp(Projectile.Center - player.Center,
                        -player.Size * .5f, player.Size * .5f);
                    State = BombState.Attached;
                    FuseAge = 0; // A caught bomb always grants a complete three-second countdown.
                    Projectile.velocity = Vector2.Zero;
                    Projectile.tileCollide = false;
                    Projectile.netUpdate = true;
                    break;
                }
        }
        else if (State == BombState.Attached)
        {
            bool valid = AttachedPlayer >= 0 && AttachedPlayer < Main.maxPlayers &&
                Main.player[AttachedPlayer].active && !Main.player[AttachedPlayer].dead && !Main.player[AttachedPlayer].ghost;
            if (valid)
                FollowAttachedPlayer();
            else if (MaterialistAntagonizer.Authority)
            {
                State = BombState.Falling;
                AttachedPlayer = -1;
                Projectile.netUpdate = true;
            }
        }
        if (!Main.dedServ) UpdateFuseVisual();
        if (MaterialistAntagonizer.Authority && FuseAge >= FuseTicks)
        {
            State = BombState.Exploding;
            FuseAge = 0;
            Projectile.velocity = Vector2.Zero;
            Projectile.tileCollide = false;
            ResizeForBlast();
            Projectile.netUpdate = true;
            if (!Main.dedServ) ExplodeVisual();
        }
        else if (MaterialistAntagonizer.Authority && FuseAge % 30 == 0) Projectile.netUpdate = true;
    }

    private void FollowAttachedPlayer()
    {
        if (AttachedPlayer >= 0 && AttachedPlayer < Main.maxPlayers && Main.player[AttachedPlayer].active &&
            !Main.player[AttachedPlayer].dead && !Main.player[AttachedPlayer].ghost)
            Projectile.Center = Main.player[AttachedPlayer].Center + attachmentOffset;
    }

    private void ResizeForBlast()
    {
        if (Projectile.width == BlastSize) return;
        Vector2 center = Projectile.Center;
        Projectile.width = Projectile.height = BlastSize;
        Projectile.Center = center;
    }
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        Projectile.velocity = Vector2.Zero;
        if (MaterialistAntagonizer.Authority)
        {
            State = BombState.Grounded;
            Projectile.tileCollide = false;
            Projectile.netUpdate = true;
        }
        return false;
    }
    public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
    {
        fallThrough = false;
        return true;
    }

    private void UpdateFuseVisual()
    {
        int second = Math.Clamp((FuseTicks - FuseAge + 59) / 60, 1, 3);
        bool tick = second != lastSecond || FuseAge > 150 && FuseAge % 10 == 0;
        lastSecond = second;
        if (tick && Main.GameUpdateCount >= nextBeep)
        {
            nextBeep = Main.GameUpdateCount + 8;
            SoundEngine.PlaySound(SoundID.Item4 with { Volume = .4f, Pitch = .25f + FuseAge / 360f, MaxInstances = 3 }, Projectile.Center);
        }
        Lighting.AddLight(Projectile.Center, .55f, .07f, .015f);
        if (FuseAge % 6 == 0)
            MaterialistVisuals.Nano(Projectile.Center - new Vector2(0, 9), new Vector2(0, -.5f),
                new Color(255, 110, 45), .6f, 14);
    }
    private void ExplodeVisual()
    {
        explosionVisual = true;
        SoundEngine.PlaySound(SoundID.Item14 with { Volume = .8f, Pitch = -.15f, MaxInstances = 3 }, Projectile.Center);
        for (int i = 0; i < 12; i++)
        {
            Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(12, 12),
                DustID.Torch, Main.rand.NextVector2Circular(3, 3), 50, default, 1.1f);
            dust.noGravity = true;
        }
        MaterialistVisuals.Burst(Projectile.Center, new Color(255, 124, 45), .55f);
    }

    public override void SendExtraAI(BinaryWriter writer) => writer.WriteVector2(attachmentOffset);
    public override void ReceiveExtraAI(BinaryReader reader) => attachmentOffset = reader.ReadVector2();
    public override bool PreDraw(ref Color lightColor)
    {
        if (State == BombState.Exploding)
        {
            float fade = MathHelper.Clamp(1 - FuseAge / 8f, 0, 1);
            MaterialistVisuals.Glow(Projectile.Center, Color.OrangeRed * fade, new Vector2(46));
            MaterialistVisuals.Brackets(Projectile.Center, new Vector2(BlastSize), Color.White, fade);
            return false;
        }
        Main.instance.LoadProjectile(ProjectileID.StickyBomb);
        Texture2D texture = TextureAssets.Projectile[ProjectileID.StickyBomb].Value;
        Color warning = Color.Lerp(new Color(255, 190, 58), Color.Red, MathHelper.Clamp(FuseAge / 180f, 0, 1));
        float flash = .55f + .45f * MathF.Sin(FuseAge * (FuseAge > 120 ? .5f : .18f));
        MaterialistVisuals.Glow(Projectile.Center, warning * (.4f + flash * .3f), new Vector2(26));
        Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, null, Color.White,
            Projectile.rotation, texture.Size() * .5f, 1.15f, SpriteEffects.None, 0);
        MaterialistVisuals.Brackets(Projectile.Center, new Vector2(BlastSize), warning, .5f + flash * .4f);
        int second = Math.Clamp((FuseTicks - FuseAge + 59) / 60, 1, 3);
        Vector2 text = Projectile.Center - Main.screenPosition - new Vector2(0, 24);
        Utils.DrawBorderString(Main.spriteBatch, second.ToString(), text, warning, .65f, .5f, .5f);
        return false;
    }
}
