using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

public sealed partial class MaterialistInstrument : ModProjectile
{
    internal Vector2 Start, End, Mark;
    internal int Age, Delay, Seed;
    private int retirement = -1;
    private bool flashed;
    private Vector2[] nodes;
    internal bool Lightning => Projectile.ai[2] == 1;
    internal float Opacity => retirement >= 0 ? Math.Max(0, 1 - retirement / 18f) :
        Age < Delay ? 1 : MathF.Pow(Math.Max(0, 1 - (Age - Delay) / 14f), 1.5f);
    private MaterialistAntagonizer Carrier => MaterialistAntagonizer.Owner((int)Projectile.ai[0] - 1, (int)Projectile.ai[1]);
    public override string Texture => "Terraria/Images/MagicPixel";
    public override string LocalizationCategory => "Projectiles.MaterialistArsenal";
    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 150000;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 4;
        Projectile.hostile = true; Projectile.friendly = false;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.penetrate = -1; Projectile.timeLeft = 180;
        Projectile.netImportant = true; CooldownSlot = ImmunityCooldownID.Bosses;
    }
    public override bool ShouldUpdatePosition() => false;
    internal bool BelongsTo(MaterialistAntagonizer boss) => Projectile.ai[0] == boss.NPC.whoAmI + 1 && Projectile.ai[1] == boss.Encounter;
    internal void Retire()
    {
        if (retirement >= 0) return;
        retirement = 0;
        Projectile.hostile = false; Projectile.damage = 0;
        Projectile.timeLeft = 20;
        if (MaterialistAntagonizer.Authority) Projectile.netUpdate = true;
    }
    public override void AI()
    {
        var owner = Carrier;
        if (owner == null || owner.Order is FlightOrder.Arrival or FlightOrder.Deploy or FlightOrder.Recover or FlightOrder.Transition or FlightOrder.Death)
        {
            if (MaterialistAntagonizer.Authority) Retire();
            else if (retirement < 0) return;
        }
        if (retirement >= 0)
        {
            Projectile.hostile = false; Projectile.damage = 0;
            if (++retirement >= 18) Projectile.Kill();
            return;
        }
        Age++;
        if (Age >= Delay + 14) { Projectile.Kill(); return; }
        if (Main.dedServ) return;
        Vector2 marker = Lightning ? End : Start;
        if (Age < Delay && Age % (MaterialistVisuals.Reduced ? 8 : 4) == 0)
        {
            float a = Age * .25f + Seed % 17;
            Vector2 offset = new(MathF.Cos(a) * 21, MathF.Sin(a * 2) * 17);
            MaterialistVisuals.Nano(marker + offset, -offset * .09f,
                Lightning ? new Color(161, 196, 255) : Color.LightCyan, .65f, 14);
        }
        if (!flashed && Age >= Delay)
        {
            flashed = true;
            if (Age - Delay <= 3)
            {
                SoundEngine.PlaySound((Lightning ? SoundID.Item122 : SoundID.Item75) with
                    { Volume = Lightning ? .42f : .65f, Pitch = Lightning ? -.2f : .15f, MaxInstances = Lightning ? 4 : 3 }, Mark);
                MaterialistEffects.Pulse(Mark, Lightning ? 54 : 42, Color.LightCyan, .25f);
                for (int i = 0; i < (MaterialistVisuals.Reduced ? 3 : 9); i++)
                    MaterialistVisuals.Nano(Mark, Main.rand.NextVector2Circular(4, 3), Color.LightCyan, .75f, 16);
            }
        }
        Lighting.AddLight(marker, new Vector3(.10f, .23f, .30f) * Opacity);
    }
    public override bool? CanDamage() => retirement < 0 && Age >= Delay && Age < Delay + 2 &&
        Carrier is { Order: FlightOrder.Command } ? null : false;
    private Vector2[] Path
    {
        get
        {
            if (nodes != null) return nodes;
            int steps = Math.Clamp((int)(Vector2.Distance(Start, End) / 20), 2, 100);
            nodes = new Vector2[steps + 1];
            Vector2 perpendicular = (End - Start).SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2);
            uint state = (uint)Math.Max(1, Seed);
            for (int i = 0; i <= steps; i++)
            {
                state ^= state << 13; state ^= state >> 17; state ^= state << 5;
                float jitter = i == 0 || i == steps ? 0 : ((state & 65535) / 65535f * 2 - 1) * 10;
                nodes[i] = Vector2.Lerp(Start, End, i / (float)steps) + perpendicular * jitter;
            }
            return nodes;
        }
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (CanDamage() == false) return false;
        float distance = 0;
        if (!Lightning) return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Start, End, 14, ref distance);
        Vector2[] path = Path;
        for (int i = 1; i < path.Length; i++)
            if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), path[i - 1], path[i], 10, ref distance)) return true;
        return false;
    }
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(Age); writer.Write(Delay); writer.Write(Seed); writer.Write(retirement);
        writer.Write(Start.X); writer.Write(Start.Y); writer.Write(End.X); writer.Write(End.Y); writer.Write(Mark.X); writer.Write(Mark.Y);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Age = reader.ReadInt32(); Delay = reader.ReadInt32(); Seed = reader.ReadInt32(); retirement = reader.ReadInt32();
        Start = new(reader.ReadSingle(), reader.ReadSingle()); End = new(reader.ReadSingle(), reader.ReadSingle());
        Mark = new(reader.ReadSingle(), reader.ReadSingle()); nodes = null; stormArcs = null;
        if (retirement >= 0) { Projectile.hostile = false; Projectile.damage = 0; }
    }
}

public sealed partial class MaterialistAntagonizer
{
    private void SpawnInstrument(bool lightning, Vector2 origin, Vector2 mark, int delay, Vector2 direction)
    {
        if (!Authority) return;
        int active = 0, instruments = 0;
        foreach (Projectile projectile in Main.ActiveProjectiles)
        {
            if (projectile.ModProjectile is MaterialistHazard hazard && hazard.BelongsTo(this) && !hazard.Retiring) active++;
            if (projectile.ModProjectile is MaterialistInstrument instrument && instrument.BelongsTo(this)) { active++; instruments++; }
        }
        if (active >= MaterialistRules.HazardCap || instruments >= 18) return;
        Vector2 end = lightning ? mark : origin + direction * Math.Min(2600, MaterialistHazard.TraceBeam(origin, direction, 14));
        int index = Projectile.NewProjectile(NPC.GetSource_FromAI(), lightning ? mark : origin, Vector2.Zero,
            ModContent.ProjectileType<MaterialistInstrument>(), lightning ? 34 : 36, 0, Main.myPlayer,
            NPC.whoAmI + 1, Encounter, lightning ? 1 : 0);
        if (index < Main.maxProjectiles && Main.projectile[index].ModProjectile is MaterialistInstrument shot)
        {
            shot.Start = origin; shot.End = end; shot.Mark = mark;
            shot.Delay = delay; shot.Seed = Main.rand.Next(1, int.MaxValue);
            shot.Projectile.timeLeft = delay + 16;
            shot.Projectile.netUpdate = true;
        }
    }
}
