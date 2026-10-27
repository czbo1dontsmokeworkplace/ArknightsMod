using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.Isharmla;

public sealed partial class IsharmlaHazard
{
    private void ModernAI(Isharmla boss)
    {
        int time = Age - Delay;
        if (Age >= Delay + Duration) { Projectile.Kill(); return; }
        if (Kind == TideShot.EchoSinger)
        {
            if (time is 0 or 38 or 76)
            {
                boss.Score(Origin + new Vector2(0, -22), Direction.SafeNormalize(Vector2.UnitY), 5, .65f, Direction.Length(), 1, 18);
                IsharmlaPresentation.Cue(1, Origin);
            }
            return;
        }
        if (Kind == TideShot.WakeMark)
        {
            if (time == 0)
            {
                Vector2 normal = Direction.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
                for (int side = -1; side <= 1; side += 2)
                    for (int layer = 0; layer < (Style == 1 ? 2 : 1); layer++)
                        boss.SpawnShot(TideShot.FoamArc, Origin + normal * side * 20, normal * side * (6 + layer * 2),
                            Projectile.damage, 18, duration: 155, style: layer);
                if (!Main.dedServ) TidalParticles.Burst(Origin, 9);
            }
            return;
        }
        if (Kind == TideShot.TideHeart)
        {
            if (time == -48 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Target = IsharmlaRules.BoundedAim(Main.player[boss.NPC.target].Center + Main.player[boss.NPC.target].velocity * 12);
                Direction = (Target - Projectile.Center).SafeNormalize(Vector2.UnitY) * (Style == 1 ? 10 : 8);
                Projectile.netUpdate = true;
            }
            if (time < 0)
            {
                if (!Main.dedServ && Age % 4 == 0)
                {
                    Vector2 offset = Main.rand.NextVector2CircularEdge(160, 120);
                    TidalParticles.Emit(TideParticle.Foam, Origin + offset, -offset * .035f, new Vector2(9), IsharmlaVisuals.Crest, 30);
                }
                return;
            }
            if (time == 0)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient && Direction.LengthSquared() < 1)
                {
                    Target = IsharmlaRules.BoundedAim(Main.player[boss.NPC.target].Center + Main.player[boss.NPC.target].velocity * 12);
                    Direction = (Target - Projectile.Center).SafeNormalize(Vector2.UnitY) * (Style == 1 ? 10 : 8);
                    Projectile.netUpdate = true;
                }
                IsharmlaPresentation.Impact(Projectile.Center, 1.4f);
            }
            Projectile.Center += Direction;
            if (time > 0 && time < 108 && time % 27 == 0) boss.BubbleRing(Projectile.Center, Style == 1 ? 24 : 18, 4.8f, time * .015f);
            if (time == 112)
            {
                boss.BubbleRing(Projectile.Center, Style == 1 ? 40 : 32, 7.5f, .13f);
                IsharmlaPresentation.Impact(Projectile.Center, 1.5f);
                Projectile.Kill();
            }
            return;
        }
        if (time < 0) return;
        if (Kind == TideShot.Canticle)
        {
            Vector2 normal = Direction.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
            Projectile.Center = Origin + Direction * time + normal * MathF.Sin(time * .065f + Style * MathHelper.Pi) * 12;
        }
        else if (Kind == TideShot.FoamArc)
        {
            if (Style == 2) Direction.Y += .075f;
            Projectile.Center += Direction;
        }
        else if (Kind == TideShot.Current) Projectile.Center += Direction;
        if (!Main.dedServ && Age % 8 == 0 && Kind != TideShot.Canticle)
            TidalParticles.Emit(TideParticle.Foam, Projectile.Center, -Direction * .06f, new Vector2(4), IsharmlaVisuals.Crest, 15);
    }

    public void DrawModern()
    {
        float opacity = Age < Delay ? .22f + .48f * IsharmlaRules.Smooth(Age / (float)Math.Max(1, Delay)) :
            Math.Min(1, Math.Max(0, (Delay + Duration - Age) / 20f));
        Vector2 at = Projectile.Center;
        Color color = Style % 2 == 0 ? IsharmlaVisuals.Crest : new Color(159, 201, 255);
        if (Kind == TideShot.EchoSinger)
        {
            float fade = IsharmlaRules.Envelope(Age, 0, Delay + Duration, 24);
            IsharmlaPresentation.DrawSinger(Main.spriteBatch, Origin, 1.7f, Direction.X >= 0 ? 1 : -1,
                .85f, -.06f, color * (.52f * fade), true);
            IsharmlaPresentation.Arc(Origin, new Vector2(48, 65), -.5f, 3.8f, color * (.5f * fade), 2);
            return;
        }
        if (Kind == TideShot.WakeMark)
        {
            Vector2 axis = Direction.SafeNormalize(Vector2.UnitX);
            float charge = IsharmlaRules.Smooth(Age / (float)Math.Max(1, Delay));
            IsharmlaVisuals.Line(at - axis * Length * .5f, at + axis * Length * .5f,
                color * (opacity * .7f), 2 + charge * 3);
            for (int side = -1; side <= 1; side += 2)
                IsharmlaPresentation.Chevron(at + axis.RotatedBy(MathHelper.PiOver2) * side * (12 + charge * 13),
                    axis.RotatedBy(MathHelper.PiOver2) * side, color * opacity, 6);
            return;
        }
        if (Kind == TideShot.TideHeart)
        {
            float progress = IsharmlaRules.Smooth(Age / (float)Math.Max(1, Delay));
            float radius = 16 + progress * 64;
            IsharmlaVisuals.DrawField(at, new Vector2(radius * 2.4f), Age * .014f, IsharmlaVisuals.Azure * (.9f * opacity), "Lens");
            IsharmlaPresentation.Arc(at, new Vector2(radius * 1.28f, radius * .55f), Age * .018f, 5.2f, color * opacity, 3);
            IsharmlaPresentation.Arc(at, new Vector2(radius * .65f, radius * 1.3f), -Age * .02f, 4.6f, color * (.7f * opacity), 2);
            IsharmlaVisuals.Orb(at, new Vector2(radius * .65f), color * (.65f * opacity));
            if (Age >= Delay - 48 && Age < Delay)
                for (int i = 1; i <= 4; i++) IsharmlaPresentation.Chevron(at + Direction.SafeNormalize(Vector2.UnitY) * (100 + i * 50),
                    Direction, color * .75f, 12);
            return;
        }
        if (Kind == TideShot.Canticle)
        {
            for (int i = 3; i > 0; i--)
                IsharmlaPresentation.Note(at - Direction * i * 1.4f, Direction.ToRotation(), color * (opacity * (4 - i) * .07f), 1);
            IsharmlaPresentation.Note(at, Direction.ToRotation(), color * opacity, 1);
        }
        else
        {
            Vector2 size = Kind == TideShot.Current ? new Vector2(31, 67) : new Vector2(26, 19);
            float rotation = Direction.ToRotation() + (Kind == TideShot.Current ? MathHelper.PiOver2 : 0);
            Texture2D atlas = IsharmlaVisuals.Art("ParticleAtlasV2");
            Rectangle frame = new(atlas.Width / 3, 0, atlas.Width / 3, atlas.Height / 3);
            var batch = Main.spriteBatch;
            for (int i = 3; i > 0; i--)
                batch.Draw(atlas, at - Direction * i * 1.6f - Main.screenPosition, frame, color * (opacity * .10f), rotation,
                    frame.Size() * .5f, size / frame.Size() * (1 - i * .12f), SpriteEffects.None, 0);
            batch.Draw(atlas, at - Main.screenPosition, frame, IsharmlaVisuals.Azure * opacity, rotation, frame.Size() * .5f,
                size / frame.Size() * 1.15f, SpriteEffects.None, 0);
            batch.Draw(atlas, at - Main.screenPosition, frame, color * opacity, rotation, frame.Size() * .5f, size / frame.Size(), SpriteEffects.None, 0);
            IsharmlaVisuals.Orb(at, size * .32f, Color.White * (opacity * .65f));
        }
    }
}
