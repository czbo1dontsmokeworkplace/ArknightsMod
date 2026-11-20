using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Content.Projectiles.Guard.Reaper.Phoenix;

// Immutable source fields are decoded once, retaining the original curve math.
internal sealed class ReaperSwarmDefinition
{
    private const int MaximumBurstParticles = 24;
    private const float MinimumContinuousInterval = 1f / 30f;
    private static readonly Dictionary<JsonElement, ReaperSwarmDefinition> cache = new();
    internal static ReaperSwarmDefinition Get(JsonElement data)
    {
        if (!cache.TryGetValue(data, out var result)) cache.Add(data, result = new(data));
        return result;
    }
    internal static void Clear() => cache.Clear();
    internal readonly JsonElement Data;
    internal readonly string Name, Kind, AppearanceName, TextureName, Asset, MonochromeAsset;
    internal readonly Vector2 Scale, Size, Acceleration, MotionDirection;
    internal readonly Vector4 Color;
    internal readonly float RotationSpeed, Duration, MinimumSpeed, MaximumSpeed, History, Fade, Width, Bloom, Lensflare;
    internal readonly int MaximumParticles;
    internal readonly float FirstEmission, EmissionInterval, BurstDuration, ShakeAmplitude, ShakeFrequency;
    internal readonly bool Looping, Additive, Invisible, RandomEmission, HasAttachments, HasDestruction;
    private readonly float[] alphaTimes, alphaValues, scaleTimes;
    private readonly float alphaFirst, alphaLast;
    private readonly Vector2[] scaleValues;
    private ReaperSwarmDefinition appearance, texture;
    private ReaperSwarmDefinition[] particles;
    internal ReaperSwarmDefinition Appearance => appearance ??= ReaperSwarmData.Definition(AppearanceName);
    internal ReaperSwarmDefinition Texture => texture ??= ReaperSwarmData.Definition(TextureName);
    internal ReaperSwarmDefinition[] Particles => particles ??= ReadParticles();
    private ReaperSwarmDefinition(JsonElement data)
    {
        Data = data; Name = data.S("name"); Kind = data.S("__kind");
        AppearanceName = data.S("appearance"); TextureName = data.S("texture");
        Asset = data.S("asset"); MonochromeAsset = Asset.Length > 0 ? Asset + "#monochrome" : Asset;
        Scale = data.V("scale", Vector2.One); Size = data.V("size", Vector2.One);
        Color = data.Tint(); RotationSpeed = data.F("rotation_speed"); Duration = data.F("duration", .1f);
        Acceleration = data.At("acceleration").ValueKind == JsonValueKind.Number ? new Vector2(0, data.F("acceleration")) : data.V("acceleration", Vector2.Zero);
        MotionDirection = data.V("direction", Vector2.Zero);
        MinimumSpeed = data.F("particle_min_speed", -100000); MaximumSpeed = data.F("particle_max_speed", 100000);
        History = data.F("time", .3f); Fade = data.F("fade_out_time"); Width = data.F("width", 2);
        Bloom = data.F("bloom"); Lensflare = data.F("lensflare"); Looping = data.B("looping");
        Additive = data.S("blending_mode") == "additive"; Invisible = data.S("shader") == "shader.invisible";
        FirstEmission = data.F("time_before_first_emission");
        int sourceCount = (int)data.F("max_particles", -1);
        MaximumParticles = Math.Min(sourceCount, MaximumBurstParticles);
        float sourceInterval = Math.Max(.00001f, data.F("time_between_each_emission", 1));
        if (Name == "emitter.player.95.beam") sourceInterval *= .5f;
        BurstDuration = FirstEmission + sourceInterval * Math.Max(0, sourceCount - 1) + .001f;
        EmissionInterval = sourceCount < 0
            ? Math.Max(MinimumContinuousInterval, sourceInterval)
            : sourceInterval * Math.Max(1f, (sourceCount - 1f) / (MaximumBurstParticles - 1));
        RandomEmission = data.B("exponential_distribution");
        HasAttachments = data.At("attachments").ValueKind == JsonValueKind.Array && data.At("attachments").GetArrayLength() > 0;
        HasDestruction = data.At("on_destruction").ValueKind == JsonValueKind.Object;
        ShakeAmplitude = data.F("amplitude"); ShakeFrequency = data.F("frequency", 60);
        alphaTimes = Floats(data.At("particle_alpha_animation_time_frames")); alphaValues = Floats(data.At("particle_alpha_animations"));
        JsonElement alpha = data.At("particle_alpha_animations");
        alphaFirst = alpha.At(0).Number(1);
        alphaLast = alpha.At(Math.Min(alphaTimes.Length, alphaValues.Length) - 1).Number(1);
        scaleTimes = Floats(data.At("particle_scale_animation_time_frames"));
        JsonElement values = data.At("particle_scale_animations");
        scaleValues = new Vector2[values.ValueKind == JsonValueKind.Array ? values.GetArrayLength() : 0];
        for (int i = 0; i < scaleValues.Length; i++) scaleValues[i] = values[i].V(Vector2.One);
    }
    private ReaperSwarmDefinition[] ReadParticles()
    {
        JsonElement variants = Data.At("particles");
        if (variants.ValueKind == JsonValueKind.Array)
        {
            var result = new ReaperSwarmDefinition[variants.GetArrayLength()];
            for (int i = 0; i < result.Length; i++) result[i] = ParticleDefinition(variants[i]);
            return result;
        }
        return new[] { ParticleDefinition(Data.At("particle")) };
    }
    private static ReaperSwarmDefinition ParticleDefinition(JsonElement data) =>
        data.ValueKind == JsonValueKind.String ? ReaperSwarmData.Definition(data.Text()) : Get(data);
    private static float[] Floats(JsonElement data, float fallback = 0)
    {
        if (data.ValueKind != JsonValueKind.Array) return Array.Empty<float>();
        var result = new float[data.GetArrayLength()];
        for (int i = 0; i < result.Length; i++) result[i] = data[i].Number(fallback);
        return result;
    }
    internal float Alpha(float time)
    {
        int count = Math.Min(alphaTimes.Length, alphaValues.Length);
        if (count == 0) return 1;
        if (time <= alphaTimes[0]) return alphaFirst;
        for (int i = 1; i < count; i++) if (time <= alphaTimes[i]) {
            float f = Math.Clamp((time - alphaTimes[i - 1]) / Math.Max(.00001f, alphaTimes[i] - alphaTimes[i - 1]), 0, 1);
            return MathHelper.Lerp(alphaValues[i - 1], alphaValues[i], f);
        }
        return alphaLast;
    }
    internal Vector2 ParticleScale(float time)
    {
        int count = Math.Min(scaleTimes.Length, scaleValues.Length);
        if (count == 0) return Vector2.One;
        if (time <= scaleTimes[0]) return scaleValues[0];
        for (int i = 1; i < count; i++) if (time <= scaleTimes[i])
            return Vector2.Lerp(scaleValues[i - 1], scaleValues[i], Math.Clamp((time - scaleTimes[i - 1]) / Math.Max(.00001f, scaleTimes[i] - scaleTimes[i - 1]), 0, 1));
        return scaleValues[count - 1];
    }
}
