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
        if(Kind==TideShot.Harpoon){Projectile.width=24;Projectile.height=12;}
        if(Kind==TideShot.TideSeed)Projectile.width=Projectile.height=22;
        if(Kind==TideShot.TideRay){Projectile.width=24;Projectile.height=18;}
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
        (Kind!=TideShot.TideSeed || Age>=Delay+76) &&
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
        if (Boss != null) DrawCombatArt();
        return false;
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
