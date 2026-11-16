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
        if(Kind==TideShot.Harpoon) {
            Direction=Direction.SafeNormalize(Vector2.UnitX)*Math.Min(15,Direction.Length()+.045f);
            Projectile.Center+=Direction;
        }
        else if(Kind==TideShot.TideSeed) {
            if(time<40)Projectile.Center+=Direction*MathHelper.Lerp(1,.08f,time/40f);
            else if(time==76){Direction=(Target-Projectile.Center).SafeNormalize(Vector2.UnitY)*8.8f;Projectile.netUpdate=Main.netMode!=NetmodeID.MultiplayerClient;}
            if(time>=76){Direction*=1.003f;Projectile.Center+=Direction;}
        }
        else if(Kind==TideShot.TideRay) {
            if(time<42)Direction=Direction.ToRotation().AngleTowards((Target-Projectile.Center).ToRotation(),.006f).ToRotationVector2()*Direction.Length();
            Vector2 normal=Direction.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
            float wave=MathF.Sin(time*.065f)-MathF.Sin((time-1)*.065f);
            Projectile.Center+=Direction+normal*wave*28;
        }
        else if (Kind == TideShot.Canticle)
        {
            float speed = Math.Min(18, Direction.Length() + .065f);
            float angle = Direction.ToRotation();
            if (Style == 1 && time < 34)
                angle = angle.AngleTowards((Target - Projectile.Center).ToRotation(), .012f);
            else if (Style == 0 && time < 65) angle += .0028f * (Projectile.identity % 2 == 0 ? 1 : -1);
            Direction = angle.ToRotationVector2() * speed;
            Vector2 normal = Direction.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
            float sway = MathF.Sin(time * .1f) - MathF.Sin((time - 1) * .1f);
            Projectile.Center += Direction + normal * sway * 14;
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

    public void DrawModern() => DrawCombatArt();
}
