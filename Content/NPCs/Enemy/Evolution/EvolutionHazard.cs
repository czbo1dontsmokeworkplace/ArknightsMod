using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

public sealed partial class EvolutionHazard : ModProjectile
{
    internal EvolutionShot Kind => (EvolutionShot)(int)Projectile.ai[0];
    internal int Age => (int)Projectile.ai[1];
    internal int FireAge => Delay + 4;
    internal int Encounter, Serial, Target, Delay = 42, Lifetime = 240;
    internal float Parameter;
    private bool impact;
    public override string Texture => EvolutionVisuals.Root + "BloodClot";
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 15;
        ProjectileID.Sets.TrailingMode[Type] = 2;
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2600;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 18;
        Projectile.hostile = true;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 600;
        Projectile.netImportant = true;
        CooldownSlot = ImmunityCooldownID.Bosses;
    }
    internal Evolution Parent => Main.npc.IndexInRange((int)Projectile.ai[2]) && Main.npc[(int)Projectile.ai[2]].active &&
        Main.npc[(int)Projectile.ai[2]].ModNPC is Evolution boss && boss.Encounter == Encounter ? boss : null;
    public override void SendExtraAI(BinaryWriter w)
    {
        w.Write(Encounter); w.Write(Serial); w.Write(Target); w.Write(Delay); w.Write(Lifetime); w.Write(Parameter); w.Write(impact);
    }
    public override void ReceiveExtraAI(BinaryReader r)
    {
        Encounter = r.ReadInt32(); Serial = r.ReadInt32(); Target = r.ReadInt32(); Delay = r.ReadInt32(); Lifetime = r.ReadInt32(); Parameter = r.ReadSingle();
        impact = r.ReadBoolean();
    }
    public override bool ShouldUpdatePosition() => Kind is EvolutionShot.Blood or EvolutionShot.Spirit or EvolutionShot.Rock or EvolutionShot.Core or EvolutionShot.Fragment ||
        Kind == EvolutionShot.Lance && Age >= FireAge || Kind == EvolutionShot.CrimsonBomb && Age < FireAge;
    public override bool? CanCutTiles() => false;
    public override bool? CanDamage()
    {
        if (Parent == null || Parent.Serial != Serial || Age >= Lifetime - 12) return false;
        return Kind switch
        {
            EvolutionShot.DashMarker or EvolutionShot.Core => false,
            EvolutionShot.Beam or EvolutionShot.Spike => Age >= FireAge && Age < FireAge + 10 ? null : false,
            EvolutionShot.Tentacle => Age >= FireAge + 5 && Age < FireAge + 24 ? null : false,
            EvolutionShot.Lance => Age >= FireAge ? null : false,
            EvolutionShot.Spirit => Age >= Delay ? null : false,
            EvolutionShot.CrimsonBomb => Parameter >= 0 && Age >= FireAge && Age < FireAge + 8 ? null : false,
            EvolutionShot.Eruption => Age >= FireAge + 6 && Age < FireAge + 26 ? null : false,
            EvolutionShot.Pulse => Age >= 30 ? null : false,
            _ => Age >= 18 ? null : false
        };
    }
    public override void AI()
    {
        Evolution boss = Parent;
        if (boss == null || boss.Serial != Serial || Age >= Lifetime)
        {
            Projectile.hostile = false;
            Projectile.Kill();
            return;
        }
        Projectile.ai[1]++;
        if (Kind == EvolutionShot.Lance && Age >= FireAge && (Age - FireAge + 1) * Projectile.velocity.Length() + 24 > Parameter)
        { Projectile.hostile = false; Projectile.Kill(); return; }
        Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
        switch (Kind)
        {
            case EvolutionShot.Blood:
                if (Age > 7) Projectile.velocity.Y = Math.Min(17, Projectile.velocity.Y + .21f);
                Projectile.tileCollide = Age > 24 && !Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height);
                break;
            case EvolutionShot.Spirit:
                if (Age < Delay) Projectile.velocity *= .97f;
                else if (Age < Delay + 75 && Main.player.IndexInRange(Target) && Main.player[Target].active && !Main.player[Target].dead)
                {
                    float angle = direction.ToRotation();
                    float wanted = (Main.player[Target].Center - Projectile.Center).ToRotation();
                    Projectile.velocity = angle.AngleTowards(wanted, boss.Phase == 5 ? .03f : .033f).ToRotationVector2() * Math.Min(boss.Phase == 5 ? 20 : 16, Projectile.velocity.Length() + .4f);
                }
                if (Age == Delay) EvolutionVisuals.Burst(Projectile.Center, .35f, false);
                break;
            case EvolutionShot.Rock:
                if (Projectile.width != 48) Projectile.Resize(48, 48);
                Projectile.velocity.Y = Math.Min(19, Projectile.velocity.Y + .3f);
                Projectile.tileCollide = !Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height);
                if (impact) Projectile.velocity.X = Math.Sign(Projectile.velocity.X) * Math.Min(13, Math.Abs(Projectile.velocity.X) + .045f);
                Projectile.rotation += Projectile.velocity.X * .018f;
                break;
            case EvolutionShot.CrimsonBomb: UpdateCrimsonBomb(boss); break;
            case EvolutionShot.Eruption:
                if (Age == FireAge)
                {
                    EvolutionImpactSystem.Emit(Projectile.Center, 200, 2.6f);
                    EvolutionVisuals.Burst(Projectile.Center, 1, false);
                }
                break;
            case EvolutionShot.Fragment:
                Projectile.velocity = Projectile.velocity.RotatedBy(.008f);
                Projectile.rotation += .055f;
                break;
            case EvolutionShot.Core:
                Projectile.velocity *= .982f;
                if (Age == Delay && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 2; i++) boss.Shoot(EvolutionShot.Spirit, Projectile.Center,
                        direction.RotatedBy(i == 0 ? -.55f : .55f) * 4, delay: 30, lifetime: 145);
                    Projectile.Kill();
                }
                break;
            case EvolutionShot.Beam:
            case EvolutionShot.Tentacle:
            case EvolutionShot.Spike:
            case EvolutionShot.Lance:
                if (Age == FireAge)
                {
                    EvolutionImpactSystem.Emit(Projectile.Center, 140, Kind == EvolutionShot.Beam ? 2.8f : 1.2f);
                    EvolutionVisuals.Burst(Projectile.Center, Kind == EvolutionShot.Beam ? 1.1f : .65f, false);
                    if (!Main.dedServ) SoundEngine.PlaySound(SoundID.Item33 with { Volume = .55f, Pitch = -.5f, MaxInstances = 4 }, Projectile.Center);
                }
                break;
        }
        if (Kind != EvolutionShot.Rock && Kind != EvolutionShot.Fragment) Projectile.rotation = direction.ToRotation();
        if (!Main.dedServ)
        {
            Lighting.AddLight(Projectile.Center, .5f, .025f, .06f);
            if (Age % 6 == 0 && Kind is EvolutionShot.Blood or EvolutionShot.Spirit or EvolutionShot.Core)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Blood, -Projectile.velocity * .05f, 50, default, .9f);
                d.noGravity = Kind != EvolutionShot.Blood;
            }
        }
    }
    public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
    {
        if (Kind == EvolutionShot.Rock) fallThrough = false;
        return true;
    }
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (Kind != EvolutionShot.Rock) return true;
        if (oldVelocity.Y > 0 && Projectile.velocity.Y != oldVelocity.Y && !impact)
        {
            impact = true;
            Projectile.netUpdate = true;
            EvolutionVisuals.Burst(Projectile.Center, 1f);
            EvolutionImpactSystem.Emit(Projectile.Center, 240, 4);
        }
        if (Projectile.velocity.Y != oldVelocity.Y) Projectile.velocity.Y = 0;
        if (Projectile.velocity.X != oldVelocity.X) { Projectile.velocity.X = -oldVelocity.X * .85f; Projectile.netUpdate = true; }
        if (Math.Abs(Projectile.velocity.X) < 4) Projectile.velocity.X = oldVelocity.X < 0 ? -6 : 6;
        return false;
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle box)
    {
        Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
        if (Kind == EvolutionShot.CrimsonBomb)
        {
            Vector2 nearest = new(MathHelper.Clamp(Projectile.Center.X, box.Left, box.Right), MathHelper.Clamp(Projectile.Center.Y, box.Top, box.Bottom));
            return Vector2.DistanceSquared(nearest, Projectile.Center) <= BombRadius * BombRadius;
        }
        if (Kind == EvolutionShot.Pulse)
        {
            float radius = EvolutionRules.RingRadius(Age);
            if (Vector2.Distance(box.Center.ToVector2(), Projectile.Center) > radius + box.Width + box.Height + 20) return false;
            for (int i = 0; i < 120; i++)
            {
                float angle = i * MathHelper.TwoPi / 120;
                if (EvolutionRules.InGap(angle + MathHelper.Pi / 120, Parameter)) continue;
                float collision = 0;
                if (Collision.CheckAABBvLineCollision(box.TopLeft(), box.Size(), Projectile.Center + angle.ToRotationVector2() * radius,
                    Projectile.Center + (angle + MathHelper.TwoPi / 120).ToRotationVector2() * radius, 12, ref collision)) return true;
            }
            return false;
        }
        if (Kind == EvolutionShot.Lance && Age >= FireAge)
        {
            float collision = 0;
            return Collision.CheckAABBvLineCollision(box.TopLeft(), box.Size(), Projectile.Center - Projectile.velocity, Projectile.Center + direction * 24, 10, ref collision);
        }
        if (Kind is EvolutionShot.Beam or EvolutionShot.Tentacle or EvolutionShot.Spike or EvolutionShot.Eruption)
        {
            float collision = 0;
            float length = Parameter > 0 ? Parameter : Kind == EvolutionShot.Beam ? 1900 : 340;
            float width = Kind == EvolutionShot.Eruption ? 28 : Kind == EvolutionShot.Tentacle ? 22 : 12;
            return Collision.CheckAABBvLineCollision(box.TopLeft(), box.Size(), Projectile.Center, Projectile.Center + direction * length, width, ref collision);
        }
        return null;
    }
    public override void OnKill(int timeLeft)
    {
        if (Kind is EvolutionShot.Core or EvolutionShot.Rock) EvolutionVisuals.Burst(Projectile.Center, .65f, false);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        if (Kind == EvolutionShot.CrimsonBomb) { DrawCrimsonBomb(); return false; }
        if (Kind == EvolutionShot.Eruption) { DrawEruption(); return false; }
        Vector2 center = Projectile.Center;
        Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
        float fade = MathHelper.Clamp(Age / 12f, 0, 1) * MathHelper.Clamp((Lifetime - Age) / 15f, 0, 1);
        Color blood = EvolutionVisuals.Blood * fade;
        if (Kind == EvolutionShot.Pulse)
        {
            float radius = EvolutionRules.RingRadius(Age);
            EvolutionVisuals.Ring(center, radius, Parameter, blood * (Age < 30 ? .3f : .95f), Age < 30 ? 3 : 12);
            // Illuminate the two openings without drawing a damaging bridge across them.
            for (int i = 0; i < 2; i++) foreach (float edge in new[] { -.77f, .77f })
                EvolutionVisuals.Glow(center + (Parameter + i * MathHelper.Pi + edge).ToRotationVector2() * radius,
                    EvolutionVisuals.Core * fade, new Vector2(30));
            return false;
        }
        if (Kind == EvolutionShot.Lance && Age >= FireAge)
        {
            EvolutionVisuals.Trail(Projectile, 8, blood);
            EvolutionVisuals.Line(center - direction * 45, center + direction * 25, blood, 11);
            EvolutionVisuals.Line(center - direction * 30, center + direction * 24, EvolutionVisuals.Core * fade, 3);
            return false;
        }
        if (Kind is EvolutionShot.Beam or EvolutionShot.Tentacle or EvolutionShot.Spike or EvolutionShot.DashMarker or EvolutionShot.Lance)
        {
            float length = Parameter > 0 ? Parameter : Kind == EvolutionShot.Beam ? 1900 : 340;
            Vector2 end = center + direction * length;
            if (Age < Delay)
            {
                EvolutionVisuals.AimLine(center, end, (Age + 1f) / Math.Max(1, Delay));
            }
            else if (Age < FireAge || Kind == EvolutionShot.DashMarker) return false;
            else if (Kind == EvolutionShot.Tentacle)
            {
                float amount = Math.Min(1, (Age - FireAge + 1) / 5f);
                EvolutionVisuals.Tendril(center, Vector2.Lerp(center, end, amount), 0, Age, 30, fade);
            }
            else
            {
                float flash = MathHelper.Clamp(1 - (Age - FireAge) / 18f, 0, 1);
                EvolutionVisuals.Line(center, end, new Color(52, 0, 13) * flash, 32);
                EvolutionVisuals.Line(center, end, blood * flash, 18);
                EvolutionVisuals.Line(center, end, EvolutionVisuals.Core * flash, 5);
                EvolutionVisuals.Glow(center, blood * flash, new Vector2(120, 60), direction.ToRotation());
            }
            return false;
        }
        EvolutionVisuals.Trail(Projectile, Kind == EvolutionShot.Spirit ? 9 : 14, blood);
        string name = Kind switch { EvolutionShot.Rock => "BloodRock", EvolutionShot.Core => "Heart", EvolutionShot.Fragment => "ShellFragment", _ => "BloodClot" };
        if (Kind == EvolutionShot.Spirit)
        {
            EvolutionVisuals.Glow(center, blood, new Vector2(52, 30), Projectile.rotation);
            EvolutionVisuals.Glow(center, EvolutionVisuals.Core * fade, new Vector2(21, 10), Projectile.rotation);
            EvolutionVisuals.Line(center - direction * 13, center + direction * 9, EvolutionVisuals.Core * fade, 3);
        }
        else
        {
            Texture2D texture = EvolutionVisuals.Asset(name);
            float size = Kind == EvolutionShot.Rock ? 64 : Kind == EvolutionShot.Core ? 42 : Kind == EvolutionShot.Fragment ? 28 : 23;
            float scale = size / Math.Max(texture.Width, texture.Height);
            EvolutionVisuals.Glow(center, blood * .6f, new Vector2(size * 1.6f));
            Main.spriteBatch.Draw(texture, center - Main.screenPosition, null, Color.White * fade, Projectile.rotation,
                texture.Size() * .5f, scale, SpriteEffects.None, 0);
        }
        return false;
    }
}
