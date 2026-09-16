using System;
using System.IO;
using Terraria.Audio;
using Terraria.ID;
using ArknightsMod.Content.Items.Weapons.Phalanx;
using Microsoft.Xna.Framework;
using Terraria.GameContent;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Phalanx;

public sealed class PhalanxFocus : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    private float opacity, flash;
    private int layers, serial;
    private bool attacking;
    private int chargeFrames, releaseSerial;
    private float releaseFlash;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.penetrate = -1; Projectile.timeLeft = 30;
        Projectile.netImportant = true;
    }
    public override bool? CanDamage() => false;
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(opacity); writer.Write(layers); writer.Write(serial); writer.Write(attacking);
        writer.Write((byte)chargeFrames); writer.Write(releaseSerial);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        opacity = reader.ReadSingle(); layers = reader.ReadInt32();
        int next = reader.ReadInt32();
        if (next != serial) { flash = 1; PhalanxVisuals.Burst(Projectile.Center, (int)Projectile.ai[0], 26, 6); }
        serial = next; attacking = reader.ReadBoolean();
        chargeFrames = Math.Clamp((int)reader.ReadByte(), 0, PhalanxCycle.ChargeDuration - 1);
        int nextRelease = reader.ReadInt32();
        if (nextRelease != releaseSerial) releaseFlash = 1f;
        releaseSerial = nextRelease;
    }
    public override void AI()
    {
        Player player = Main.player[Projectile.owner];
        if (!player.active || player.dead) { Projectile.Kill(); return; }
        var state = player.GetModPlayer<PhalanxPlayer>();
        bool holding = player.HeldItem.ModItem is PhalanxStaff && !player.CCed && !player.noItems;
        Projectile.Center = player.MountedCenter;
        if (holding) Projectile.timeLeft = 30;
        else opacity = MathHelper.Max(0, opacity - 1f / 24f);
        flash = MathHelper.Max(0, flash - .055f);
        releaseFlash = Math.Max(0, releaseFlash - 1f / 16f);
        int previousCharge = chargeFrames;
        if (Main.myPlayer == Projectile.owner)
        {
            if (serial != state.FlashSerial)
            {
                flash = 1; PhalanxVisuals.Burst(player.Center, (int)Projectile.ai[0], 30, 6);
            }
            int tier = state.Staff?.Tier ?? (int)Projectile.ai[0];
            int nextLayers = state.Shield.Remaining(tier);
            bool release = releaseSerial != state.ReleaseSerial;
            if (release) releaseFlash = 1f;
            bool chargeSync = (state.ChargeFrames == 0 && chargeFrames != 0)
                || (state.ChargeFrames > 0 && chargeFrames == 0) || (state.ChargeFrames > 0 && state.ChargeFrames % 6 == 0);
            bool changed = release || chargeSync || serial != state.FlashSerial || layers != nextLayers || attacking != state.Attacking
                || Projectile.ai[0] != tier || System.Math.Abs(opacity - state.Opacity) > .015f;
            serial = state.FlashSerial; opacity = state.Opacity; layers = nextLayers;
            attacking = state.Attacking; Projectile.ai[0] = tier;
            chargeFrames = state.ChargeFrames; releaseSerial = state.ReleaseSerial;
            if (changed) Projectile.netUpdate = true;
        }
        else chargeFrames = holding && attacking ? Math.Min(chargeFrames + 1, PhalanxCycle.ChargeDuration - 1) : 0;
        if (!Main.dedServ && chargeFrames > 0)
        {
            if (previousCharge == 0)
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = .25f, Pitch = -.45f }, Projectile.Center);
            if (previousCharge < 65 && chargeFrames >= 65)
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = .38f, Pitch = .2f }, Projectile.Center);
        }
        if (holding)
        {
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, -.55f * player.direction - MathHelper.Pi);
            Lighting.AddLight(player.Center, PhalanxVisuals.Palette((int)Projectile.ai[0]).ToVector3() * .35f);
        }
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Player player = Main.player[Projectile.owner];
        int tier = (int)Projectile.ai[0];
        PhalanxVisuals.Shield(Projectile.Center, tier, opacity, flash, layers);
        if (player.HeldItem.ModItem is PhalanxStaff)
        {
            var texture = TextureAssets.Item[player.HeldItem.type].Value;
            float scale = 58f / System.Math.Max(texture.Width, texture.Height);
            PhalanxVisuals.Sprite(texture,
                player.MountedCenter - Main.screenPosition + new Vector2(player.direction * 22, -22),
                texture.Size() * scale, lightColor, -MathHelper.PiOver4 + player.direction * .2f);
        }
        Vector2 focus = player.MountedCenter - Main.screenPosition + new Vector2(player.direction * 27, -45);
        if (HoldingForDraw(player) && chargeFrames > 0)
        {
            DrawCharge(focus, tier, chargeFrames / (float)PhalanxCycle.ChargeDuration);
            PhalanxSignatureVisuals.DrawCharge(Projectile.Center - Main.screenPosition, focus, tier,
                chargeFrames / (float)PhalanxCycle.ChargeDuration, Main.GlobalTimeWrappedHourly);
        }
        if (releaseFlash > 0)
        {
            Color color = PhalanxVisuals.Palette(tier); color.A = 0;
            PhalanxVisuals.Glow(focus, new Vector2(28 + (1 - releaseFlash) * 95), color * releaseFlash * .6f);
            PhalanxVisuals.Glow(focus, new Vector2(18 + (1 - releaseFlash) * 32), new Color(255, 255, 255, 0) * releaseFlash);
        }
        return false;
    }
    private static bool HoldingForDraw(Player player) => player.HeldItem.ModItem is PhalanxStaff && !player.CCed && !player.noItems;
    private static void DrawCharge(Vector2 focus, int tier, float progress)
    {
        // Discrete motes converge along spirals. No lines, stretched pixels, or inventory overlays.
        Color color = PhalanxVisuals.Palette(tier); color.A = 0;
        float intensity = progress * progress;
        float time = Main.GlobalTimeWrappedHourly;
        PhalanxVisuals.Glow(focus, new Vector2(24 + progress * 50), color * (.1f + intensity * .55f));
        PhalanxVisuals.Glow(focus, new Vector2(5 + progress * 17), new Color(255, 245, 225, 0) * progress * .85f);
        for (int i = 0; i < 18; i++)
        {
            float travel = (progress * 3 + i / 18f) % 1;
            float angle = i * 2.399963f + travel * 1.8f + time * .35f;
            float radius = 8 + (1 - travel) * (78 - progress * 22);
            Vector2 mote = focus + angle.ToRotationVector2() * radius;
            float fade = MathF.Sin(travel * MathHelper.Pi) * Math.Min(1, progress * 5);
            PhalanxVisuals.Glow(mote, new Vector2(4 + progress * 5), color * fade * (.35f + progress * .5f));
        }
    }
}
