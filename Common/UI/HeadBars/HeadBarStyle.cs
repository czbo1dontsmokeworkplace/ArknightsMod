using Microsoft.Xna.Framework;

namespace ArknightsMod.Common.UI.HeadBars;

/// <summary>Colors, size, placement and transition timing for the shared seven-segment head bar.</summary>
public readonly record struct HeadBarStyle(Color Background, Color Start, Color End)
{
    public float Scale { get; init; } = 1f;
    public float Opacity { get; init; } = .92f;
    public float OffsetY { get; init; } = 58f;
    public int FadeInTicks { get; init; } = 12;
    public int FadeOutTicks { get; init; } = 18;
    public float SlidePixels { get; init; } = 4f;
    public float FillSmoothing { get; init; } = .22f;
}

public readonly record struct HeadBarRequest(float Progress, HeadBarStyle Style);
