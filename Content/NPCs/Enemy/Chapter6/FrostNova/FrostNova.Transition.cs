using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.Chapter6.FrostNova;

public partial class FrostNova
{
    private Vector2 revivalAnchor;
    private bool revivalAnchorSet;
    internal bool FNBirthCompleted => FNBirthEnd;
    internal bool DomainActive => FrostNovaDomainRules.InDomain(this);
    internal float StormProgress => MathHelper.SmoothStep(0, 1, Math.Clamp(ReBirthTimer / 240f, 0, 1));
    internal Vector2 RevivalAnchor => revivalAnchor;
    internal float LandingAge => FNRevivalStart && revivalAnchorSet ? DeathTimer - 540 : -1;
    private void BeginRevivalMotion()
    {
        revivalAnchor = NPC.Center; revivalAnchorSet = true;
        NPC.velocity = Vector2.Zero; NPC.noGravity = NPC.noTileCollide = true;
    }
    internal static float RevivalHeight(float age)
    {
        if (age < 360 || age >= 540) return 0;
        if (age < 450) return MathHelper.SmoothStep(0, 64, (age - 360) / 90);
        if (age < 510) return 64 + MathHelper.SmoothStep(0, 144, (age - 450) / 60);
        if (age < 522) return 208;
        return 208 * (1 - MathF.Pow(Math.Clamp((age - 522) / 18, 0, 1), 2.4f));
    }
    private void TickRevivalMotion()
    {
        if (!revivalAnchorSet) BeginRevivalMotion();
        NPC.noGravity = NPC.noTileCollide = true;
        NPC.velocity = Vector2.Zero;
        NPC.Center = revivalAnchor - Vector2.UnitY * RevivalHeight(DeathTimer);
        if (DeathTimer == 144 && !Main.dedServ)
            SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Sounds/FNRevive1") { Volume = .8f }, NPC.Center);
        if (DeathTimer == 450 && !Main.dedServ)
            SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Sounds/FNRevive2") { Volume = 1.2f }, NPC.Center);
        if (DeathTimer >= 360 && DeathTimer < 522 && (int)DeathTimer % 12 == 0)
            FrostNovaVisuals.Cast(NPC.Center + new Vector2(0, 24), 1, .4f);
        if (DeathTimer == 540) {
            FrostNovaIceBurstSystem.Emit(revivalAnchor + new Vector2(0, 29), 1.7f);
            // No attack, collision or hostile child is created by this landing.
            FrostNovaVisuals.Cast(revivalAnchor + new Vector2(0, 29), 2, 2.2f);
            FrostNovaVisuals.Shatter(revivalAnchor + new Vector2(0, 29), Vector2.Zero, 28);
            if (!Main.dedServ) {
                SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Sounds/FNIceRingStg1") { Volume = 1.1f, Pitch = -.35f }, NPC.Center);
                if (Vector2.DistanceSquared(Main.LocalPlayer.Center, NPC.Center) < 1800 * 1800) {
                    var shake = Main.LocalPlayer.GetModPlayer<global::ArknightsMod.Common.VisualEffects.ShakeEffectPlayer>();
                    shake.screenShakeTime = Math.Max(shake.screenShakeTime, 20);
                    shake.screenShakeMaxDistance = Math.Max(shake.screenShakeMaxDistance, 7);
                }
            }
            NPC.netUpdate = true;
        }
    }
    private bool TryRevivalFrame(int height)
    {
        if (!FNRevivalStart || height <= 0) return false;
        int row = DeathTimer < 60 ? 34 + Math.Min(4, (int)DeathTimer / 12)
            : DeathTimer < 320 ? 42
            : DeathTimer < 360 ? 43 + Math.Min(4, (int)(DeathTimer - 320) / 8)
            : DeathTimer < 450 ? 48 + Math.Min(6, (int)(DeathTimer - 360) / 13)
            : DeathTimer < 522 ? 55 + Math.Min(10, (int)(DeathTimer - 450) / 7)
            : DeathTimer < 540 ? 33 : 66 + Math.Min(9, (int)(DeathTimer - 540) / 6);
        NPC.frame.Y = row * height; NPC.frameCounter = 0;
        return true;
    }
}
