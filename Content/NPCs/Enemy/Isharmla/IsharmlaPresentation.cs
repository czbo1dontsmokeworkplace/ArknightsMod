using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.Isharmla;

// Shared visual vocabulary: pearl voices, cobalt water, short white impact crests.
// Client presentation never creates hostile entities.
public sealed class IsharmlaPresentation : ModSystem
{
    public static void Cue(int kind, Vector2 at)
    {
        if (Main.dedServ || IsharmlaVisuals.ClockOverride.HasValue) return;
        (string name, float pitch, float volume, int maximum) = kind switch
        {
            0 => ("Chant", -.08f, .65f, 2),
            1 => ("Answer", .05f, .40f, 3),
            2 => ("Rush", 0f, .65f, 2),
            3 => ("Release", 0f, .65f, 1),
            4 => ("Breach", -.10f, .85f, 1),
            5 => ("Pressure", 0f, .80f, 1),
            _ => ("Rush", .25f, .25f, 2)
        };
        SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Sounds/Isharmla/" + name)
            { Pitch = pitch, Volume = volume, MaxInstances = maximum }, at);
    }
    public static void Impact(Vector2 at, float power)
    {
        if (Main.dedServ) return;
        TidalParticles.Burst(at, (int)(24 * power));
        if (!IsharmlaVisuals.ClockOverride.HasValue)
            SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Sounds/Isharmla/Impact")
                { Volume = Math.Min(1, power * .45f), MaxInstances = 2 }, at);
        if (Main.LocalPlayer.active && Vector2.DistanceSquared(Main.LocalPlayer.Center, at) < 2400 * 2400)
            IsharmlaVisuals.Shake(Math.Min(6, 2.5f * power));
    }
    public override void ModifySunLightColor(ref Color tileColor, ref Color backgroundColor)
    {
        if (IsharmlaWorld.ActiveBoss() is not { Initialized: true } boss || Main.LocalPlayer.dead ||
            Vector2.DistanceSquared(Main.LocalPlayer.Center, boss.NPC.Center) > 5000 * 5000) return;
        float strength = boss.Stage == TideStage.Finale ? .32f * IsharmlaRules.FinaleOpacity(boss.Timer, boss.Transition) :
            boss.Stage >= TideStage.Covenant ? .30f : .16f;
        backgroundColor = Color.Lerp(backgroundColor, new Color(27, 49, 88), strength);
        tileColor = Color.Lerp(tileColor, new Color(139, 177, 203), strength * .5f);
    }
    public static void Arc(Vector2 centre, Vector2 radius, float start, float length, Color color, float width)
    {
        Vector2 previous = centre + new Vector2(MathF.Cos(start) * radius.X, MathF.Sin(start) * radius.Y);
        for (int i = 1; i <= 40; i++)
        {
            float a = start + length * i / 40;
            Vector2 next = centre + new Vector2(MathF.Cos(a) * radius.X, MathF.Sin(a) * radius.Y);
            IsharmlaVisuals.Line(previous, next, color, width); previous = next;
        }
    }
    public static void Chevron(Vector2 at, Vector2 forward, Color color, float size)
    {
        Vector2 axis = forward.SafeNormalize(Vector2.UnitX), normal = axis.RotatedBy(MathHelper.PiOver2);
        IsharmlaVisuals.Line(at - axis * size + normal * size * .6f, at, color, 2);
        IsharmlaVisuals.Line(at - axis * size - normal * size * .6f, at, color, 2);
    }
    public static void Note(Vector2 at, float angle, Color color, float scale)
    {
        // Authored with a shared silhouette; no friendly weapon or mixed boss projectile textures.
        Vector2 stem = new Vector2(0, -16 * scale).RotatedBy(angle * .18f);
        Vector2 head = at + new Vector2(-4 * scale, 5 * scale);
        IsharmlaVisuals.Orb(head, new Vector2(12, 9) * scale, IsharmlaVisuals.Abyss * (color.A / 255f));
        Main.spriteBatch.Draw(IsharmlaVisuals.Glow, head - Main.screenPosition, null, color, -.3f,
            new Vector2(32), new Vector2(.30f, .23f) * scale, SpriteEffects.None, 0);
        IsharmlaVisuals.Line(at, at + stem, color, 2.5f * scale);
        IsharmlaVisuals.Line(at + stem, at + stem + new Vector2(8, 4) * scale, color, 2.5f * scale);
        IsharmlaVisuals.Orb(head - new Vector2(1, 1), new Vector2(3) * scale, Color.White * (color.A / 255f * .8f));
    }

    public static void DrawSinger(SpriteBatch batch, Vector2 centre, float scale, int facing, float gesture, float lean, Color tint, bool echo = false)
    {
        Texture2D texture = IsharmlaVisuals.Art("Human");
        var flip = facing < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        float time = IsharmlaVisuals.Clock;
        // Three connected bands articulate the supplied sprite without rewriting its pixels.
        int torso = Math.Min(22, texture.Height), hem = Math.Min(39, texture.Height);
        float bodyAngle = lean + MathF.Sin(time * 2.1f) * .018f;
        Vector2 top = centre - new Vector2(0, texture.Height * scale * .5f);
        DrawBand(0, torso, top, bodyAngle - gesture * .055f * facing);
        Vector2 waist = top + new Vector2(0, torso * scale).RotatedBy(bodyAngle);
        DrawBand(torso, hem - torso, waist, bodyAngle);
        Vector2 skirt = waist + new Vector2(0, (hem - torso) * scale).RotatedBy(bodyAngle);
        DrawBand(hem, texture.Height - hem, skirt, bodyAngle + MathF.Sin(time * 3.2f) * (.035f + gesture * .035f));
        void DrawBand(int y, int height, Vector2 at, float rotation)
        {
            if (height <= 0) return;
            batch.Draw(texture, at - Main.screenPosition, new Rectangle(0, y, texture.Width, height), tint,
                rotation, new Vector2(texture.Width * .5f, 0), scale, flip, 0);
        }
        if (gesture > .05f)
        {
            Color water = (echo ? new Color(136, 190, 255) : IsharmlaVisuals.Crest) * (gesture * .65f * tint.A / 255f);
            Vector2 hand = centre + new Vector2(facing * 33, -13 - gesture * 9);
            Arc(hand, new Vector2(22, 32), time * .8f, 4.1f, water, 1.5f);
            Note(hand + new Vector2(facing * 22, -18), 0, water, .7f);
        }
    }
    public static void DrawHuman(SpriteBatch batch, Isharmla boss, Vector2 screen)
    {
        float visibility = boss.Stage == TideStage.Finale ? IsharmlaRules.FinaleOpacity(boss.Timer, boss.Transition) : 1;
        if (boss.Mounted && boss.Dragon is { } dragon) visibility *= dragon.Visibility;
        if (visibility < .01f) return;
        Vector2 at = boss.NPC.Center;
        Color water = (boss.NPC.dontTakeDamage ? new Color(153, 188, 221) : IsharmlaVisuals.Crest) * visibility;
        float gesture = boss.Transition > 0 ? .8f : boss.Gesture;
        bool dash = boss.Stage == TideStage.LastSong && boss.Attack == 2 &&
            (boss.Cast is >= 90 and < 113 || boss.Cast is >= 174 and < 197);
        if (dash)
            for (int i = 4; i > 0; i--)
                DrawSinger(batch, at - boss.NPC.velocity * i * 1.2f, 1.7f, boss.NPC.spriteDirection, gesture, boss.Lean,
                    water * (.06f * (5 - i)), true);
        Arc(at + new Vector2(0, 48), new Vector2(54, 12), IsharmlaVisuals.Clock * .25f, 5.3f, water * .45f, 2);
        DrawSinger(batch, at, 1.7f, boss.NPC.spriteDirection, gesture, boss.Lean, Color.White * visibility);
        if (boss.Stage == TideStage.Leviathan || boss.NPC.dontTakeDamage && boss.Stage != TideStage.Finale)
            Arc(at, new Vector2(52, 60), -1.2f, 5f, water * .30f, 1.5f);
        if (boss.Attack == 2 && boss.Stage is TideStage.Cantor or TideStage.LastSong && boss.Cast is >= 62 and < 90)
        {
            Vector2 forward = (boss.Aim - at).SafeNormalize(Vector2.UnitX);
            for (int i = 1; i <= 3; i++) Chevron(at + forward * (65 + i * 40), forward, water * .65f, 11);
        }
    }

    public static void DrawDragonHead(SpriteBatch batch, IsharmlaDragon dragon, Vector2 screen)
    {
        Texture2D texture = IsharmlaVisuals.Art("Head");
        float opacity = dragon.Visibility * (dragon.Dying > 0 ? 1 - IsharmlaRules.Smooth((dragon.Dying - 730) / 100f) : 1);
        if (opacity < .001f) return;
        Vector2 origin = new(220, 220), hinge = new(68, 310);
        float scale = IsharmlaRules.DragonScale, rotation = dragon.NPC.rotation;
        float jaw = -.035f + dragon.Jaw;
        Color tint = Color.Lerp(Color.White, new Color(169, 222, 244), dragon.Charge * .22f) * opacity;
        // A sloping jaw seam follows the original illustration. Both halves retain their native pixel scale.
        for (int x = 0; x < texture.Width; x += 8)
        {
            int width = Math.Min(8, texture.Width - x);
            int seam = Math.Clamp((int)(300 + x * .12f), 0, texture.Height);
            Vector2 upper = new Vector2(x, 0);
            batch.Draw(texture, dragon.NPC.Center - screen + ((upper - origin) * scale).RotatedBy(rotation),
                new Rectangle(x, 0, width, seam), tint, rotation, Vector2.Zero, scale, SpriteEffects.None, 0);
            Vector2 lower = hinge + (new Vector2(x, seam) - hinge).RotatedBy(jaw);
            batch.Draw(texture, dragon.NPC.Center - screen + ((lower - origin) * scale).RotatedBy(rotation),
                new Rectangle(x, seam, width, texture.Height - seam), tint, rotation + jaw, Vector2.Zero, scale, SpriteEffects.None, 0);
        }
        if (dragon.Charge > .1f)
            IsharmlaVisuals.Orb(dragon.Muzzle, new Vector2(22 + dragon.Charge * 22), IsharmlaVisuals.Crest * (dragon.Charge * .65f * opacity));
    }
    public static void DrawDragonTell(IsharmlaDragon dragon)
    {
        if (dragon.Boss is not { Transition: 0 } boss || dragon.Dying > 0) return;
        int t = dragon.LocalTimer;
        if (dragon.Pattern == 0 && t < 90)
        {
            Vector2 at = new(dragon.Aim.X, boss.Anchor.Y);
            for (int i = 0; i < 3; i++) Arc(at, new Vector2(80 + i * 55, 13 + i * 8), t * .012f + i, 4.8f,
                IsharmlaVisuals.Crest * (.15f + dragon.Charge * .30f), 2);
            for (int i = 0; i < 3; i++) Chevron(at - new Vector2(0, 25 + i * 25), -Vector2.UnitY,
                IsharmlaVisuals.Crest * dragon.Charge * .55f, 9);
        }
        if (dragon.Pattern == 1 && (t is >= 70 and < 110 || t is >= 230 and < 270))
        {
            Vector2 direction = new(t < 200 ? -dragon.AttackSide : dragon.AttackSide, 0);
            for (int i = 0; i < 4; i++) Chevron(dragon.NPC.Center + direction * (180 + i * 70), direction,
                IsharmlaVisuals.Crest * (.25f + dragon.Charge * .35f), 14);
        }
        if (dragon.Pattern == 5 && t is >= 100 and < 205)
        {
            for (int i = 0; i < 3; i++) Arc(dragon.Muzzle, new Vector2(45 + i * 24), -t * .025f + i * 1.7f, 4.2f,
                IsharmlaVisuals.Crest * (.25f + dragon.Charge * .45f), 2 + i * .3f);
        }
    }
    public static void DragonParticles(IsharmlaDragon dragon)
    {
        if (Main.dedServ || dragon.Charge < .08f || dragon.LocalTimer % 3 != 0) return;
        Vector2 edge = Main.rand.NextVector2CircularEdge(100, 70);
        TidalParticles.Emit(TideParticle.Prism, dragon.Muzzle + edge, -edge * .04f,
            new Vector2(4 + dragon.Charge * 5), IsharmlaVisuals.Crest, 25);
    }
}
