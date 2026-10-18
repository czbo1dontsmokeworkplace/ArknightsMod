using System;
using System.Collections.Generic;

namespace ArknightsMod.Content.Items.Weapons.Medic;

// Pure rules: all times are real ticks, independent of projectile extraUpdates.
internal sealed class MedicationResistance
{
    public int Ticks;
    public float Reduction;
    public int Scale(int amount) => Math.Max(0, (int)MathF.Floor(amount * (1f - Math.Clamp(Reduction, 0, 1))));
    public void Commit(int interval, float duration, float reduction)
    {
        Ticks = Math.Max(Ticks, Math.Max(1, (int)MathF.Ceiling(interval * duration)));
        Reduction = Math.Max(Reduction, Math.Clamp(reduction, 0, 1));
    }
    public void Tick() { if (Ticks > 0 && --Ticks == 0) Reduction = 0; }
    public void Clear() { Ticks = 0; Reduction = 0; }
}

internal sealed class MedicalRegenLayers
{
    internal sealed class Layer(int ticks, float rate, int profile)
    {
        public int Ticks = ticks;
        public float Rate = rate;
        public int Profile = profile;
    }
    public readonly List<Layer> Layers = [];
    public float Rate { get { float total = 0; foreach (var layer in Layers) total += layer.Rate; return total; } }
    public bool Add(int ticks, float rate, int limit, int profile)
    {
        if (Layers.Count >= Math.Clamp(limit, 1, 20)) return false;
        Layers.Add(new Layer(ticks, rate, profile)); return true;
    }
    public void Tick()
    {
        for (int i = Layers.Count - 1; i >= 0; i--)
            if (--Layers[i].Ticks <= 0) Layers.RemoveAt(i);
    }
}
