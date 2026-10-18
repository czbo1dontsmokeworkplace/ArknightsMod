using ArknightsMod.Content.Items.Weapons.Medic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace ArknightsMod.Content.Projectiles.Medic.Arts;

internal enum MedicSoundCue { Cast, Release, Treatment, FieldOpen, FieldPulse, FieldClose, Skill }

internal static class MedicArtsAudio
{
    // Separate identifiers keep casting, healing and release layers from cutting one another off,
    // even when they use the same vanilla recording.
    internal static (SoundStyle Body, SoundStyle Accent) Styles(int profile, MedicSoundCue cue)
    {
        var p = MedicCatalog.Get(profile);
        SoundStyle voice = p.Branch switch
        {
            MedicBranch.Therapist => SoundID.Item21,
            MedicBranch.MultiTarget => SoundID.Item8,
            MedicBranch.Chain => SoundID.Item29,
            MedicBranch.Wandering => SoundID.Item30,
            _ => SoundID.Item20
        };
        float pitch = -.18f + p.Tier * .045f;
        (SoundStyle body, SoundStyle accent, float volume, float overtone, float shift) = cue switch
        {
            MedicSoundCue.Cast => (voice, SoundID.Item29, .82f, .30f, 0f),
            MedicSoundCue.Release => (SoundID.Item4, voice, .62f, .32f, .16f),
            MedicSoundCue.Treatment => (SoundID.Item29, SoundID.Item4, .72f, .30f, .28f),
            MedicSoundCue.FieldOpen => (SoundID.Item8, SoundID.Item29, .90f, .48f, -.30f),
            MedicSoundCue.FieldPulse => (SoundID.Item29, SoundID.Item4, .24f, .12f, -.12f),
            MedicSoundCue.FieldClose => (SoundID.Item4, SoundID.Item8, .58f, .26f, -.35f),
            _ => (voice, SoundID.Item29, .95f, .48f, -.10f)
        };
        return (Layer(body, volume, pitch + shift, "Body"),
            Layer(accent, overtone, pitch + shift + .20f, "Accent"));

        SoundStyle Layer(SoundStyle sound, float volume, float tone, string layer) => sound with
        {
            Volume = volume,
            Pitch = MathHelper.Clamp(tone, -.8f, .8f),
            PitchVariance = .07f,
            Identifier = $"ArknightsMod:Medical:{p.Branch}:{cue}:{layer}",
            MaxInstances = cue == MedicSoundCue.FieldPulse ? 2 : 8,
            SoundLimitBehavior = cue == MedicSoundCue.FieldPulse
                ? SoundLimitBehavior.IgnoreNew : SoundLimitBehavior.ReplaceOldest
        };
    }

    internal static void Play(Vector2 position, int profile, MedicSoundCue cue)
    {
        if (Main.dedServ) return;
        var (body, accent) = Styles(profile, cue);
        SoundEngine.PlaySound(body, position);
        SoundEngine.PlaySound(accent, position);
    }

    internal static void Treatment(Player patient, int profile, int cleansed)
    {
        if (Main.dedServ) return;
        var state = patient.GetModPlayer<MedicalTreatmentPlayer>();
        if (MedicCatalog.Get(profile).Branch == MedicBranch.MultiTarget)
        {
            // Fields check patients every 15 ticks, including full-health contacts.
            // Keep a clear healing cue without turning those checks into a rapid audio loop.
            if (Main.GameUpdateCount < state.NextFieldTreatmentSound) return;
            state.NextFieldTreatmentSound = Main.GameUpdateCount + 60;
        }
        Play(patient.Center, profile, MedicSoundCue.Treatment);
        if (cleansed > 0)
            SoundEngine.PlaySound(SoundID.Item30 with
            {
                Volume = .48f, Pitch = .35f, PitchVariance = .08f, MaxInstances = 4,
                Identifier = "ArknightsMod:Medical:Cleanse"
            }, patient.Center);
    }
}
