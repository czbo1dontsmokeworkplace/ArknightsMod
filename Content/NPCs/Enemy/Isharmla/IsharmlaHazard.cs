using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.Isharmla;

public sealed class IsharmlaHazard : ModProjectile
{
    public int OwnerIndex = -1, Encounter, Age, Delay, Duration;
    public TideShot Kind;
    public Vector2 Direction, Origin;
    public float Length, Width;
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
        Projectile.Center = center;
    }
    public override void SendExtraAI(BinaryWriter w)
    {
        w.Write(OwnerIndex); w.Write(Encounter); w.Write(Age); w.Write((byte)Kind); w.WriteVector2(Direction); w.WriteVector2(Origin);
        w.Write(Delay); w.Write(Duration); w.Write(Length); w.Write(Width);
    }
    public override void ReceiveExtraAI(BinaryReader r)
    {
        OwnerIndex = r.ReadInt32(); Encounter = r.ReadInt32(); Age = r.ReadInt32(); Kind = (TideShot)r.ReadByte();
        Direction = r.ReadVector2(); Origin = r.ReadVector2(); Delay = r.ReadInt32(); Duration = r.ReadInt32(); Length = r.ReadSingle(); Width = r.ReadSingle();
        SetHitbox();
    }
    internal Isharmla Boss => OwnerIndex >= 0 && OwnerIndex < Main.maxNPCs && Main.npc[OwnerIndex].active &&
        Main.npc[OwnerIndex].ModNPC is Isharmla b && b.Encounter == Encounter ? b : null;
    private bool Ray => Kind is TideShot.Column or TideShot.Breath or TideShot.Gate or TideShot.Lattice;
    private bool Armed => Age >= Delay && (!Ray || Age < Delay + Duration) && Kind != TideShot.RainCloud;
    public override bool ShouldUpdatePosition() => false;
    public override bool CanHitPlayer(Player target) => Armed && Boss != null;
    public override bool? CanDamage() => Armed && Boss != null ? null : false;
    public override void AI()
    {
        var boss = Boss;
        if (boss == null) { if (Main.netMode != NetmodeID.MultiplayerClient) Projectile.Kill(); return; }
        Age++;
        if (!Main.dedServ) IsharmlaVisuals.HazardParticles(this);
        if (Kind == TideShot.Breath && boss.Dragon is { } dragon)
        { Projectile.Center = dragon.Muzzle; Direction = dragon.FacingAngle.ToRotationVector2(); }
        if (Age == Delay && !Main.dedServ) SoundEngine.PlaySound(Ray ? SoundID.Item122 : SoundID.Item21, Projectile.Center);
        if (Ray)
        {
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
        if (Kind == TideShot.Seeker && Age < Delay + 45)
        {
            Player p = Main.player[boss.NPC.target];
            Direction = Vector2.Lerp(Direction, (p.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 9, .025f);
        }
        Projectile.Center += Direction;
        if (Kind == TideShot.Wave) Projectile.Center = new Vector2(Projectile.Center.X, Origin.Y + MathF.Sin(Age * .09f) * 15);
        if (Age > Delay + (Kind == TideShot.Wave ? 160 : 230)) Projectile.Kill();
        if (!Main.dedServ && Age % 3 == 0) IsharmlaVisuals.Mote(Projectile.Center, -Direction * .08f, .7f);
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (!Armed) return false;
        if (Ray)
        {
            float point = 0;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center,
                Projectile.Center + Direction.SafeNormalize(Vector2.UnitY) * Length, Width, ref point);
        }
        if (Kind == TideShot.Wave)
            return new Rectangle((int)Projectile.Center.X - 38, (int)Projectile.Center.Y - 105, 76, 150).Intersects(targetHitbox);
        return projHitbox.Intersects(targetHitbox);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        if (Boss == null) return false;
        Color color = Kind == TideShot.Gate ? new Color(255, 194, 100) : IsharmlaVisuals.Cyan;
        if (Ray)
        {
            IsharmlaVisuals.RayField(this, color);
        }
        else if (Kind == TideShot.RainCloud)
        {
            IsharmlaVisuals.DrawField(Projectile.Center, new Vector2(Width * 1.45f, 95), 0, color * .85f, "Cloud");
            if (Age < Delay) IsharmlaVisuals.Line(Projectile.Center, Projectile.Center + new Vector2(0, 700), color * .12f, Width);
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
            if (Age < Delay) IsharmlaVisuals.Line(Projectile.Center, Projectile.Center + Direction * 12, color * .22f, 2);
            IsharmlaVisuals.OpticalBolt(Projectile.Center, Direction, Kind, Age < Delay ? .5f : 1);
        }
        return false;
    }
}
