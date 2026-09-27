using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

public abstract partial class EvolutionBroodNPC : ModNPC
{
    internal abstract EvolutionBrood Kind { get; }
    internal int Encounter, Fuse = 360;
    internal float Gap;
    internal Vector2 FlightAnchor, FlightDirection;
    internal EvolutionBroodPreset Preset;
    internal int Formation;
    private bool charging;
    private float warning;
    internal int Age => (int)NPC.ai[1];
    internal bool Retiring => NPC.ai[3] < 0;
    internal bool SupportOnly => Parent?.Phase >= 3;
    internal Evolution Parent => Main.npc.IndexInRange((int)NPC.ai[0]) && Main.npc[(int)NPC.ai[0]].active &&
        Main.npc[(int)NPC.ai[0]].ModNPC is Evolution boss && boss.Encounter == Encounter ? boss : null;
    public override string Texture => "ArknightsMod/Common/Particle/DefaultParticle";
    public override void SetStaticDefaults()
    {
        Main.npcFrameCount[Type] = 1;
        NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new NPCID.Sets.NPCBestiaryDrawModifiers { Hide = true });
    }
    public override void SetDefaults()
    {
        NPC.width = Kind == EvolutionBrood.Abomination ? 86 : 40;
        NPC.height = Kind == EvolutionBrood.Abomination ? 58 : 36;
        NPC.lifeMax = Kind switch { EvolutionBrood.Spider => 380, EvolutionBrood.GiantSpider => 1500, EvolutionBrood.Puppet => 800, EvolutionBrood.Abomination => 2300, _ => 450 };
        NPC.defense = Kind == EvolutionBrood.GiantSpider ? 24 : 10;
        NPC.damage = 0;
        NPC.knockBackResist = Kind == EvolutionBrood.Spider ? .35f : .1f;
        NPC.aiStyle = -1;
        NPC.noGravity = NPC.noTileCollide = true;
        NPC.npcSlots = 0;
        NPC.HitSound = SoundID.NPCHit8;
        NPC.DeathSound = SoundID.NPCDeath1;
        NPC.netAlways = true;
    }
    public override bool CheckActive() => false;
    public override bool? CanFallThroughPlatforms() => Parent != null && Main.player[Parent.NPC.target].Center.Y > NPC.Bottom.Y + 40;
    public override bool CanHitPlayer(Player target, ref int cooldownSlot)
    {
        cooldownSlot = ImmunityCooldownID.Bosses;
        return !SupportOnly && !Retiring && charging;
    }
    public override bool? CanBeHitByItem(Player player, Item item) => SupportOnly || Retiring ? false : null;
    public override bool? CanBeHitByProjectile(Projectile projectile) => SupportOnly || Retiring ? false : null;
    public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position) => SupportOnly ? false : null;
    public override bool CanHitNPC(NPC target) => false;
    public override bool PreKill() => !Retiring;
    public override void SendExtraAI(BinaryWriter w) { w.Write(Encounter); w.Write(Fuse); w.Write(Gap); w.WriteVector2(FlightAnchor); w.WriteVector2(FlightDirection); w.Write((byte)Preset); w.Write(Formation); }
    public override void ReceiveExtraAI(BinaryReader r) { Encounter = r.ReadInt32(); Fuse = r.ReadInt32(); Gap = r.ReadSingle(); FlightAnchor = r.ReadVector2(); FlightDirection = r.ReadVector2(); Preset = (EvolutionBroodPreset)r.ReadByte(); Formation = r.ReadInt32(); }
    public override void AI()
    {
        Evolution boss = Parent;
        if (boss == null) { NPC.active = false; return; }
        charging = false; warning = 0;
        NPC.noGravity = NPC.noTileCollide = true;
        NPC.damage = 0;
        NPC.chaseable = !SupportOnly;
        NPC.target = boss.NPC.target;
        Player player = Main.player[NPC.target];
        NPC.ai[1]++;
        NPC.timeLeft = 750;
        if (Retiring)
        {
            NPC.dontTakeDamage = true;
            NPC.noGravity = NPC.noTileCollide = true;
            NPC.damage = 0;
            NPC.velocity = Vector2.Lerp(NPC.velocity, (boss.NPC.Center - NPC.Center).SafeNormalize(Vector2.UnitY) * 24, .18f);
            NPC.alpha = Math.Min(255, NPC.alpha + 7);
            NPC.ai[3]++;
            if (NPC.ai[3] >= 0 || NPC.Distance(boss.NPC.Center) < 28) NPC.active = false;
            return;
        }
        NPC.dontTakeDamage = SupportOnly || Age < 30;
        if (Age < 60) { NPC.velocity *= .9f; return; }
        if (SupportOnly && Kind == EvolutionBrood.Puppet)
        {
            DoFixedBeamBattery(boss, player);
            return;
        }
        if (SupportOnly && Kind == EvolutionBrood.Spider)
        {
            // Phase two's spiders are invulnerable firing satellites, never contact-damage chargers.
            float flank = NPC.ai[2] < 0 ? -1 : 1;
            FlyTo(boss.PatternCenter + new Vector2(flank * (490 + Formation * 30), -190 + MathF.Sin(Age * .025f + Formation) * 135), 20);
            if (!boss.Transitioning && !boss.Desperate && boss.Attack != 0 && Beat(106, 80))
                for (int i = 0; i < 3; i++) boss.Lob(EvolutionShot.Blood, NPC.Center, boss.FormationLanding(i, 3), 74);
            NPC.spriteDirection = NPC.direction = player.Center.X > NPC.Center.X ? 1 : -1;
            NPC.rotation = MathHelper.Lerp(NPC.rotation, NPC.velocity.X * .012f, .12f);
            if ((Age > 900 || NPC.Distance(player.Center) > 1900) && Main.netMode != NetmodeID.MultiplayerClient)
            { NPC.ai[3] = -35; NPC.netUpdate = true; }
            return;
        }
        if (Preset != EvolutionBroodPreset.Hunter)
        {
            DoPreset(boss, player);
            NPC.direction = NPC.spriteDirection = player.Center.X > NPC.Center.X ? 1 : -1;
            NPC.rotation = MathHelper.Lerp(NPC.rotation, NPC.velocity.X * .012f, .12f);
            if ((Age > 900 || NPC.Distance(player.Center) > 1900) && Main.netMode != NetmodeID.MultiplayerClient)
            { NPC.ai[3] = -35; NPC.netUpdate = true; }
            return;
        }
        if (Kind is EvolutionBrood.Tumor or EvolutionBrood.Bomb)
        {
            NPC.velocity = (FlightAnchor + new Vector2(0, MathF.Sin(Age * .035f) * 18) - NPC.Center) * .08f;
            if (Age == Fuse - 100 && Kind == EvolutionBrood.Bomb) boss.Shoot(EvolutionShot.Spirit, NPC.Center, -Vector2.UnitY * 3, delay: 35, lifetime: 140);
            if (Age == Fuse) EvolutionImpactSystem.Emit(NPC.Center, 280, 4);
            if (Age >= Fuse && Main.netMode != NetmodeID.MultiplayerClient)
            {
                boss.Shoot(EvolutionShot.Pulse, NPC.Center, Vector2.Zero, parameter: Gap, lifetime: 220);
                if (Kind == EvolutionBrood.Bomb)
                    for (int i = 0; i < 4; i++) boss.Shoot(EvolutionShot.Spike, NPC.Center,
                        (Gap + MathHelper.PiOver4 + i * MathHelper.PiOver2).ToRotationVector2(), parameter: 420, delay: 48, lifetime: 88);
                EvolutionVisuals.Burst(NPC.Center, 1.2f);
                NPC.active = false;
                NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI);
            }
            return;
        }
        float aggression = EvolutionRules.Aggression(boss.Phase);
        float side = NPC.ai[2] < 0 ? -1 : 1;
        if (Kind == EvolutionBrood.Spider)
        {
            int cycle = EvolutionRules.ChargeStride(Math.Max(105, (int)(180 / aggression)), 94);
            int beat = (Age - 60 + NPC.whoAmI * 23) % cycle;
            if (beat < 42) FlyTo(player.Center + new Vector2(side * 500, 0), 15 * aggression);
            if (beat == 42)
            {
                NPC.velocity = Vector2.Zero;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                { FlightDirection = new Vector2(player.Center.X > NPC.Center.X ? 1 : -1, 0); NPC.netUpdate = true; }
            }
            if (beat >= 42 && beat < 70) { NPC.velocity = Vector2.Zero; if (beat < 66) warning = (beat - 41f) / 24; }
            if (beat >= 70 && beat < 94) { charging = true; NPC.damage = EvolutionDamageCockpit.BroodContactDamage(Kind); NPC.velocity = FlightDirection * 18 * aggression; }
            if (beat >= 94) FlyTo(player.Center + new Vector2(-side * 540, -240), 16 * aggression);
        }
        else
        {
            float orbit = Age * (Kind == EvolutionBrood.Puppet ? .028f : .015f) + NPC.whoAmI * 1.7f;
            Vector2 hover = boss.PatternCenter + new Vector2(side * (480 + MathF.Sin(orbit) * 150), -230 + MathF.Cos(orbit * .7f) * 220);
            FlyTo(hover, (Kind == EvolutionBrood.Puppet ? 19 : 12) * aggression);
        }
        NPC.direction = player.Center.X > NPC.Center.X ? 1 : -1;
        NPC.rotation = MathHelper.Lerp(NPC.rotation, NPC.velocity.X * .012f, .12f);
        NPC.spriteDirection = NPC.direction;
        if ((NPC.Distance(player.Center) > 1450 || Age > 900) && Main.netMode != NetmodeID.MultiplayerClient)
        { NPC.ai[3] = -35; NPC.netUpdate = true; return; }
        int interval = (int)(140 / aggression);
        if (boss.Transitioning || boss.Desperate || (Age + NPC.whoAmI * 17) % interval != 0 || boss.Attack == 0 && boss.Phase != 1) return;
        switch (Kind)
        {
            case EvolutionBrood.GiantSpider:
                boss.Lob(EvolutionShot.Rock, NPC.Center, boss.PatternCenter + new Vector2(side * 340, 20), 80);
                for (int i = 0; i < 3; i++) boss.Lob(EvolutionShot.Blood, NPC.Center, boss.FormationLanding(i, 3), 75);
                break;
            case EvolutionBrood.Puppet:
                for (int i = 0; i < 2; i++) boss.Shoot(EvolutionShot.Spirit, NPC.Center,
                    boss.FormationDirection(NPC.Center, i, 2) * 4, delay: 32, lifetime: 160);
                break;
            case EvolutionBrood.Abomination:
                boss.TentacleAt(boss.PatternCenter + new Vector2(side * 220, 0), 48);
                boss.Shoot(EvolutionShot.Lance, NPC.Center, (boss.PatternCenter - NPC.Center).SafeNormalize(Vector2.UnitX) * 30, 1500, 42, 118);
                break;
        }
    }
    private void FlyTo(Vector2 point, float speed)
    {
        Vector2 delta = point - NPC.Center;
        NPC.velocity = Vector2.Lerp(NPC.velocity, delta.SafeNormalize(Vector2.UnitY) * Math.Min(speed, delta.Length() * .1f), .15f);
    }
    public override void OnKill()
    {
        EvolutionVisuals.Burst(NPC.Center, .7f);
        if (Kind == EvolutionBrood.Spider && Parent is Evolution boss && !boss.Transitioning)
            for (int i = -1; i <= 1; i += 2) boss.Shoot(EvolutionShot.Blood, NPC.Center, new Vector2(i * 4, -5), lifetime: 80);
    }
    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        float appear = MathHelper.Clamp(Age / 60f, 0, 1) * NPC.Opacity;
        EvolutionVisuals.BroodOrb(NPC.Center, Kind, Age, appear, SupportOnly);
        if (warning > 0) EvolutionVisuals.AimLine(NPC.Center, NPC.Center + FlightDirection * (18 * EvolutionRules.Aggression(Parent?.Phase ?? 1) * 24 + 50), warning);
        if (Age < 60) EvolutionVisuals.Ring(NPC.Center, 65 * (1 - appear) + 25, 0, EvolutionVisuals.Core * appear, 2, false);
        if (Kind is EvolutionBrood.Tumor or EvolutionBrood.Bomb)
        {
            float charge = MathHelper.Clamp((Age - 60f) / (Fuse - 60), 0, 1);
            bool mine = Preset == EvolutionBroodPreset.Minefield;
            EvolutionVisuals.Ring(NPC.Center, 56, Gap, EvolutionVisuals.Core * (.3f + charge * .65f), 2, !mine);
            if (Age > Fuse - 90 && (mine || Preset == EvolutionBroodPreset.Hunter))
                EvolutionVisuals.Ring(NPC.Center, mine ? 150 : 145, Gap, EvolutionVisuals.Blood * .55f, 2, !mine);
        }
        return false;
    }
}
public sealed class EvolutionSpider : EvolutionBroodNPC { internal override EvolutionBrood Kind => EvolutionBrood.Spider; }
public sealed class EvolutionGiantSpider : EvolutionBroodNPC { internal override EvolutionBrood Kind => EvolutionBrood.GiantSpider; }
public sealed class EvolutionPuppet : EvolutionBroodNPC { internal override EvolutionBrood Kind => EvolutionBrood.Puppet; }
public sealed class EvolutionAbomination : EvolutionBroodNPC { internal override EvolutionBrood Kind => EvolutionBrood.Abomination; }
public sealed class EvolutionTumor : EvolutionBroodNPC { internal override EvolutionBrood Kind => EvolutionBrood.Tumor; }
public sealed class EvolutionBomb : EvolutionBroodNPC { internal override EvolutionBrood Kind => EvolutionBrood.Bomb; }
