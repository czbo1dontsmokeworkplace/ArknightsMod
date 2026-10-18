using System;

namespace ArknightsMod.Common.UI.HeadBars;

/// <summary>Per-player animation, preserved across a hide/show reversal so switching never pops.</summary>
public sealed class HeadBarAnimation
{
    public float Visibility { get; private set; }
    public float Fill { get; private set; }
    public float Flash { get; private set; }
    public float Opacity => Visibility * Visibility * (3f - 2f * Visibility);
    private float previousProgress;

    public void Update(bool visible, float progress, HeadBarStyle style)
    {
        progress = float.IsFinite(progress) ? Math.Clamp(progress, 0, 1) : 0;
        Visibility = Math.Clamp(Visibility + (visible ? 1f / Math.Max(1, style.FadeInTicks)
            : -1f / Math.Max(1, style.FadeOutTicks)), 0, 1);
        if (visible && progress > previousProgress + .001f) Flash = .65f;
        else Flash = Math.Max(0, Flash - 1f / 18f);
        Fill += (progress - Fill) * Math.Clamp(style.FillSmoothing, .01f, 1f);
        previousProgress = progress;
    }

    public void Clear() => Visibility = Fill = Flash = previousProgress = 0;
}
