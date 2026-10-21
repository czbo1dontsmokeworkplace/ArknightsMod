using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

public sealed partial class MaterialistHazard
{
    private static readonly ulong[] saucerSoundNextTick = new ulong[Main.maxNPCs];
    private static readonly int[] saucerSoundEncounter = new int[Main.maxNPCs];
    internal static void ResetSaucerAudio()
    {
        Array.Clear(saucerSoundNextTick);
        Array.Clear(saucerSoundEncounter);
    }

    private void PlaySaucerSound()
    {
        int owner = (int)Projectile.ai[0] - 1, encounter = (int)Projectile.ai[1];
        if (owner < 0 || owner >= Main.maxNPCs) return;
        if (saucerSoundEncounter[owner] == encounter && Main.GameUpdateCount < saucerSoundNextTick[owner]) return;
        saucerSoundEncounter[owner] = encounter;
        saucerSoundNextTick[owner] = Main.GameUpdateCount + 6;
        // Native Saucer gun sound, throttled per carrier rather than per projectile.
        SoundEngine.PlaySound(SoundID.Item12 with { Volume = .38f, MaxInstances = 3 }, Anchor);
    }

    private void SaucerImpact()
    {
        if (Main.dedServ || Retiring) return;
        Color tint = MaterialistVisuals.AmmoColor(this);
        for (int i = 0; i < 5; i++)
        {
            Dust dust = Dust.NewDustPerfect(Projectile.Center, 228,
                Main.rand.NextVector2Circular(1.6f, 1.6f) - Projectile.velocity * .16f, 100, default, .75f);
            dust.noGravity = true;
        }
        MaterialistVisuals.Nano(Projectile.Center, -Projectile.velocity * .12f, tint, .65f, 12);
    }
}
