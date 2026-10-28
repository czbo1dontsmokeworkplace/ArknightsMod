using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.Isharmla;

public sealed partial class IsharmlaHazard : ModProjectile
{
    public int OwnerIndex = -1, Encounter, Age, Delay, Duration;
    public TideShot Kind;
    public Vector2 Direction, Origin;
    public float Length, Width;
    public Vector2 Target;
    public int Style;
    public bool Modern => Kind >= TideShot.Canticle;
    public override string Texture => "ArknightsMod/Assets/null";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 18; Projectile.hostile = true; Projectile.penetrate = -1;
        Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.timeLeft = 3600;
        CooldownSlot = ImmunityCooldownID.Bosses;
    }
    internal void Bind(Isharmla boss, TideShot kind, Vector2 velocity, int delay, float length, float width, int duration)
    {
        OwnerIndex = boss.NPC.whoAmI; Encounter = boss.Encounter; Kind = kind; Direction = velocity;
        Origin = Projectile.Center; Delay = delay; Length = length; Width = width; Duration = duration;
        Projectile.velocity = Vector2.Zero;
        SetHitbox();
    }
    private void SetHitbox()
    {
        Vector2 center = Projectile.Center;
        Projectile.width = Kind == TideShot.Rain ? 8 : Kind == TideShot.Seeker ? 20 : 18;
        Projectile.height = Kind == TideShot.Rain ? 18 : Projectile.width;
        if (Kind == TideShot.Bubble) Projectile.width = Projectile.height = 22;
        if (Kind == TideShot.Typhoon) Projectile.width = Projectile.height = 42;
        if (Kind == TideShot.Shark) { Projectile.width = 34; Projectile.height = 18; }
        if (Kind == TideShot.Tornado) { Projectile.width = 64; Projectile.height = 260; }
        if (Kind is TideShot.Note or TideShot.CharmNote) { Projectile.width = 18; Projectile.height = 22; }
        if (Kind == TideShot.StormSpark) Projectile.width = Projectile.height = 22;
        if (Kind == TideShot.Canticle) { Projectile.width = 14; Projectile.height = 18; }
        if (Kind == TideShot.FoamArc) Projectile.width = Projectile.height = 16;
        if (Kind == TideShot.Current) { Projectile.width = 22; Projectile.height = 44; }
        if (Kind == TideShot.TideHeart) Projectile.width = Projectile.height = 100;
        Projectile.Center = center;
    }
    public override void SendExtraAI(BinaryWriter w)
    {
        w.Write(OwnerIndex); w.Write(Encounter); w.Write(Age); w.Write((byte)Kind); w.WriteVector2(Direction); w.WriteVector2(Origin);
        w.Write(Delay); w.Write(Duration); w.Write(Length); w.Write(Width);
        w.WriteVector2(Target); w.Write(Style);
    }
    public override void ReceiveExtraAI(BinaryReader r)
    {
        OwnerIndex = r.ReadInt32(); Encounter = r.ReadInt32(); Age = r.ReadInt32(); Kind = (TideShot)r.ReadByte();
        Direction = r.ReadVector2(); Origin = r.ReadVector2(); Delay = r.ReadInt32(); Duration = r.ReadInt32(); Length = r.ReadSingle(); Width = r.ReadSingle();
        Target = r.ReadVector2(); Style = r.ReadInt32();
        SetHitbox();
    }
    internal Isharmla Boss => OwnerIndex >= 0 && OwnerIndex < Main.maxNPCs && Main.npc[OwnerIndex].active &&
        Main.npc[OwnerIndex].ModNPC is Isharmla b && b.Encounter == Encounter ? b : null;
    private bool Ray => IsharmlaRules.IsLineShot(Kind);
    private bool Area => Kind is TideShot.Bomb or TideShot.MistField;
    private bool Armed => Age >= Delay + (Ray ? 6 : 0) && (!(Ray || Area || Modern) || Age < Delay + Duration - 12) &&
        Kind is not (TideShot.RainCloud or TideShot.EchoSinger or TideShot.WakeMark);
    public override bool ShouldUpdatePosition() => false;
    public override bool CanHitPlayer(Player target) => Armed && Boss != null;
    public override bool? CanDamage() => Armed && Boss != null ? null : false;
    public override void AI()
    {
        var boss = Boss;
        if (boss == null) { if (Main.netMode != NetmodeID.MultiplayerClient) Projectile.Kill(); return; }
        Age++;
        if (Modern) { ModernAI(boss); return; }
        if (!Main.dedServ) IsharmlaVisuals.HazardParticles(this);
        if (Kind == TideShot.Breath && boss.Dragon is { } dragon)
        { Projectile.Center = dragon.Muzzle; Direction = dragon.FacingAngle.ToRotationVector2(); }
        if (Age == Delay && !Main.dedServ)
            SoundEngine.PlaySound(Kind == TideShot.Breath ? new SoundStyle("ArknightsMod/Sounds/Isharmla/Torrent") { Volume = .75f, MaxInstances = 1 } :
                Ray ? SoundID.Item122 : SoundID.Item21, Projectile.Center);
        if (Ray)
        {
            if (Age >= Delay + Duration) Projectile.Kill();
            return;
        }
        if (Area)
        {
            if (Kind == TideShot.Bomb && Age == Delay)
            {
                if (!Main.dedServ) IsharmlaVisuals.Burst(Projectile.Center, 20);
                for (int i = 0; i < 8; i++)
                    boss.SpawnShot(i % 2 == 0 ? TideShot.StormSpark : TideShot.Bubble, Projectile.Center,
                        (i * MathHelper.TwoPi / 8 + Projectile.identity * .17f).ToRotationVector2() * 7, Projectile.damage, 22);
            }
            if (Age >= Delay + Duration) Projectile.Kill();
            return;
        }
        if (Kind == TideShot.RainCloud)
        {
            if (Age >= Delay && Age % 12 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                float x = MathF.Sin(Age * 1.71f) * Width * .45f;
                boss.SpawnShot(TideShot.Rain, Projectile.Center + new Vector2(x, 12), new Vector2(0, 14), Projectile.damage, 8);
            }
            if (Age >= Delay + Duration) Projectile.Kill();
            return;
        }
        if (Kind == TideShot.Mine)
        {
            if (Age >= Delay + Duration)
            {
                for (int i = 0; i < 8; i++)
                    boss.SpawnShot(TideShot.Pearl, Projectile.Center, (i * MathHelper.TwoPi / 8).ToRotationVector2() * 8, Projectile.damage, 24);
                Projectile.Kill();
            }
            return;
        }
        if (Age < Delay) return;
        if (Kind == TideShot.Tornado)
        {
            if ((Age - Delay) % 45 == 0 && Age < Delay + Duration)
            {
                Vector2 aim = Main.player[boss.NPC.target].Center;
                boss.SpawnShot(TideShot.Shark, Projectile.Center + new Vector2(0, MathF.Sin(Age) * 95),
                    (aim - Projectile.Center).SafeNormalize(Vector2.UnitX) * 11, Projectile.damage, 24);
            }
            if (Age >= Delay + Duration) Projectile.Kill();
            return;
        }
        if (Kind == TideShot.Bubble) { Direction *= .995f; Direction.Y -= .012f; }
        if (Kind == TideShot.Typhoon) Direction = Direction.RotatedBy(.013f);
        if (Kind == TideShot.Seeker && Age < Delay + 45)
        {
            Player p = Main.player[boss.NPC.target];
            Direction = Vector2.Lerp(Direction, (p.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 9, .025f);
        }
        if (Kind == TideShot.CharmNote && Age < Delay + 55)
        {
            Player p = Main.player[boss.NPC.target];
            Direction = Vector2.Lerp(Direction, (p.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 10, .035f);
        }
        Projectile.Center += Direction;
        if (Kind is TideShot.Note or TideShot.CharmNote or TideShot.StormSpark)
        {
            float sway = Kind == TideShot.StormSpark ? 2.7f : 1.3f;
            Projectile.Center += Direction.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2) *
                MathF.Sin((Age - Delay) * (Kind == TideShot.StormSpark ? .34f : .13f) + Projectile.identity) * sway;
        }
        if (Kind == TideShot.Wave) Projectile.Center = new Vector2(Projectile.Center.X, Origin.Y + MathF.Sin(Age * .09f) * 15);
        if (Age > Delay + (Kind == TideShot.Wave ? 160 : 230)) Projectile.Kill();
        if (!Main.dedServ && Age % 3 == 0) IsharmlaVisuals.Mote(Projectile.Center, -Direction * .08f, .7f);
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (!Armed) return false;
        if (Area)
        {
            Vector2 nearest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
            return Vector2.DistanceSquared(nearest, Projectile.Center) < Width * Width;
        }
        if (Ray)
        {
            float point = 0;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center,
                Projectile.Center + Direction.SafeNormalize(Vector2.UnitY) * Length,
                Width * Math.Min(1, Math.Max(0, (Delay + Duration - Age) / 9f)), ref point);
        }
        if (Kind == TideShot.Wave)
            return new Rectangle((int)Projectile.Center.X - 38, (int)Projectile.Center.Y - 105, 76, 150).Intersects(targetHitbox);
        return projHitbox.Intersects(targetHitbox);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        if (Boss == null) return false;
        if (Modern) { DrawModern(); return false; }
        Color color = Kind == TideShot.Gate ? new Color(255, 194, 100) : IsharmlaVisuals.Cyan;
        if (Area) { DrawArea(); return false; }
        if (Kind is TideShot.Bubble or TideShot.Typhoon or TideShot.Tornado or TideShot.Shark or TideShot.Note or TideShot.CharmNote or TideShot.StormSpark)
        {
            int art = Kind == TideShot.Bubble ? ProjectileID.Bubble : Kind == TideShot.Typhoon ? ProjectileID.Typhoon :
                Kind == TideShot.Tornado ? ProjectileID.Sharknado : Kind == TideShot.Shark ? ProjectileID.MiniSharkron :
                Kind == TideShot.StormSpark ? ProjectileID.Electrosphere : ProjectileID.QuarterNote + Projectile.identity % 3;
            if (!TextureAssets.Projectile[art].IsLoaded) Main.instance.LoadProjectile(art);
            var texture = TextureAssets.Projectile[art].Value;
            if (Kind == TideShot.Tornado) { DrawTornado(texture); return false; }
            int frames = NativeFrames(art); int frameHeight = texture.Height / frames;
            var frame = new Rectangle(0, Age / 5 % frames * frameHeight, texture.Width, frameHeight);
            Vector2 size = Kind == TideShot.Tornado ? new Vector2(110, 320) : Kind == TideShot.Typhoon ? new Vector2(56) :
                Kind == TideShot.Shark ? new Vector2(50, 26) : Kind == TideShot.StormSpark ? new Vector2(36) :
                Kind is TideShot.Note or TideShot.CharmNote ? new Vector2(28, 32) : new Vector2(28);
            float opacity = Age < Delay ? .35f + .35f * Age / Math.Max(1f, Delay) : 1;
            float rotation = Kind == TideShot.Typhoon ? Age * .13f : Kind == TideShot.Shark ? Direction.ToRotation() :
                Kind is TideShot.Note or TideShot.CharmNote ? MathF.Sin(Age * .09f) * .15f : 0;
            Color tint = Kind == TideShot.CharmNote ? new Color(190, 150, 255) : Color.White;
            if (Kind is TideShot.Note or TideShot.CharmNote or TideShot.StormSpark)
                IsharmlaVisuals.Orb(Projectile.Center, new Vector2(24), (Kind == TideShot.CharmNote ? new Color(115, 40, 235) : color) * (.45f * opacity));
            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, frame, tint * opacity, rotation,
                frame.Size() * .5f, size / frame.Size(), Kind == TideShot.Shark && Direction.X < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None, 0);
            return false;
        }
        if (Ray)
        {
            IsharmlaVisuals.RayField(this, color);
        }
        else if (Kind == TideShot.RainCloud)
        {
            IsharmlaVisuals.DrawField(Projectile.Center, new Vector2(Width * 1.45f, 95), 0, color * .85f, "Cloud");
        }
        else if (Kind == TideShot.Wave)
        {
            IsharmlaVisuals.DrawField(Projectile.Center - new Vector2(0, 38), new Vector2(130, 155), 0, color * .9f, "Surge");
        }
        else if (Kind == TideShot.Mine)
        {
            float progress = MathHelper.Clamp(Age / (float)Math.Max(1, Delay), 0, 1);
            IsharmlaVisuals.DrawField(Projectile.Center, new Vector2(22), 0, color * (.35f + progress * .65f), "Lens");
            IsharmlaVisuals.Ring(Projectile.Center, 16 + progress * 9, color * .6f, 1.5f);
        }
        else
        {
            IsharmlaVisuals.OpticalBolt(Projectile.Center, Direction, Kind, Age < Delay ? .5f : 1);
        }
        return false;
    }
    private void DrawArea()
    {
        bool mist = Kind == TideShot.MistField;
        float warning = MathHelper.Clamp(Age / (float)Math.Max(1, Delay), 0, 1);
        bool active = Age >= Delay;
        float fade = active ? Math.Min(1, (Delay + Duration - Age) / (mist ? 25f : 8f)) : .25f + warning * .5f;
        Color color = mist ? new Color(35, 94, 230) : new Color(70, 220, 255);
        // The round footprint, including the charging outline, matches the damaging radius.
        IsharmlaVisuals.Ring(Projectile.Center, Width, color * (fade * .8f), active ? 3 : 2);
        if (!active) IsharmlaVisuals.Ring(Projectile.Center, Width * warning, color * .45f, 2);
        int art = mist ? ProjectileID.ToxicCloud : ProjectileID.Electrosphere;
        if (!TextureAssets.Projectile[art].IsLoaded) Main.instance.LoadProjectile(art);
        Texture2D texture = TextureAssets.Projectile[art].Value;
        int frames = NativeFrames(art); int height = texture.Height / frames;
        Rectangle frame = new(0, Age / 5 % frames * height, texture.Width, height);
        if (mist)
        {
            for (int i = 0; i < 5; i++)
            {
                Vector2 offset = i == 0 ? Vector2.Zero : (i * MathHelper.PiOver2 + Age * .006f).ToRotationVector2() * Width * .38f;
                float scale = Width * (i == 0 ? 1.45f : 1.1f);
                Main.spriteBatch.Draw(texture, Projectile.Center + offset - Main.screenPosition, frame, color * (fade * .7f),
                    i + Age * .003f, frame.Size() * .5f, new Vector2(scale) / frame.Size(), SpriteEffects.None, 0);
            }
        }
        else
        {
            float size = active ? Width * 2 : 32 + warning * 28;
            IsharmlaVisuals.Orb(Projectile.Center, new Vector2(active ? Width : size), color * (.6f * fade));
            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, frame, color * fade, 0,
                frame.Size() * .5f, new Vector2(size) / frame.Size(), SpriteEffects.None, 0);
        }
    }
    private static int NativeFrames(int art) => art switch
    {
        ProjectileID.Typhoon => 3, ProjectileID.Electrosphere => 4, ProjectileID.MiniSharkron => 2,
        _ => Math.Max(1, Main.projFrames[art])
    };
    private void DrawTornado(Texture2D texture)
    {
        // Sharknado's sheet contains animated horizontal slices, not one tall projectile image.
        int sliceHeight = texture.Height / 6;
        Rectangle frame = new(0, Age / 4 % 6 * sliceHeight, texture.Width, sliceHeight);
        float opacity = Age < Delay ? .3f + .4f * Age / Math.Max(1f, Delay) : 1;
        for (int i = 0; i < 14; i++)
        {
            float u = i / 13f;
            Vector2 at = Projectile.Center + new Vector2(MathF.Sin(Age * .13f + i * .55f) * 6, (u - .5f) * 280);
            Main.spriteBatch.Draw(texture, at - Main.screenPosition, frame, Color.White * opacity, 0, frame.Size() * .5f,
                new Vector2(MathHelper.Lerp(120, 36, u), 45) / frame.Size(), SpriteEffects.None, 0);
        }
    }
}
