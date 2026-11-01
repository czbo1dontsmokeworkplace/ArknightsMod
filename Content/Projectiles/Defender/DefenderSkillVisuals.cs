using ArknightsMod.Common.Particle;
using ArknightsMod.Common.Particle.T;
using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.Projectiles.Defender;

internal static class DefenderSkillVisuals
{
    internal static void Burst(Vector2 center, Color color, int count = 10)
    {
        if (Main.dedServ || ParticleManager.activeParticles.Count >= 1000) return;
        for (int i = 0; i < count; i++)
            new T_Barrier_Shard_Particle(center, Main.rand.NextVector2CircularEdge(3f, 3f), color, 22, .4f).Spawn();
    }

    internal static void Swing(Player owner, Vector2 tip, int age)
    {
        if (Main.dedServ || age % (DefenderRules.SkillActive(owner) ? 3 : 6) != 0 || ParticleManager.activeParticles.Count >= 1000) return;
        string name = owner.HeldItem.ModItem?.Name;
        Color color = DefenderRules.Color(owner.HeldItem);
        Vector2 drift = Main.rand.NextVector2Circular(1.2f, 1.2f);
        switch (name)
        {
            case "CzernyBlade": case "BasslineBlade":
                new T_Music_Note_Particle(tip, drift - Vector2.UnitY, age % 4, color).Spawn(); break;
            case "AsbestosBlade": case "YuBlade": case "HoshigumaBlazingBlade":
                new T_Ember_Particle(tip, drift, 18, color, 7f).Spawn(); break;
            case "MudrockHammer": case "Vulcan_Weapon": case "LutonHammer": case "CementMaul":
                new T_Facet_Particle(tip, drift, color, 8f, 20, 35f, true).Spawn(); break;
            case "HeavyrainBlade": case "PhilaeBlade":
                OperatorTextureEffects.Puff(tip, drift, color * .55f, 10f, 18); break;
            default:
                new T_Barrier_Shard_Particle(tip, drift, color, 16, .18f).Spawn(); break;
        }
    }

    internal static void Field(Vector2 center, float radius, DefenderAreaKind kind, Color color, int age)
    {
        if (Main.dedServ || age % 5 != 0 || ParticleManager.activeParticles.Count >= 1000) return;
        Vector2 edge = Main.rand.NextVector2CircularEdge(radius, radius * .45f);
        if (kind == DefenderAreaKind.Flame)
        {
            new T_Ember_Particle(center + edge, new Vector2(0, -1.2f), 24, color, 11f).Spawn();
            if (age % 10 == 0) OperatorTextureEffects.Puff(center + edge, -Vector2.UnitY, new Color(80, 65, 60), 20f, 28);
        }
        else if (kind is DefenderAreaKind.Electric or DefenderAreaKind.Magnet)
        {
            new T_Electric_Pulse_Particle(center + edge * .3f, .55f, true).Spawn();
            new T_Barrier_Shard_Particle(center + edge, -edge.SafeNormalize(Vector2.UnitY) * 2f, color, 20, .25f).Spawn();
        }
        else if (kind is DefenderAreaKind.Nervous or DefenderAreaKind.Necrosis)
            new T_Music_Note_Particle(center + edge, -Vector2.UnitY, age % 4, color).Spawn();
        else if (kind is DefenderAreaKind.Glue or DefenderAreaKind.Corrosion)
            OperatorTextureEffects.Puff(center + edge, new Vector2(0, -.15f), color * .6f, 17f, 20);
        else new T_Facet_Particle(center + edge * .4f, edge.SafeNormalize(Vector2.UnitY) * 2f,
            color, 9f, 22, 45f, true).Spawn();
    }
}
