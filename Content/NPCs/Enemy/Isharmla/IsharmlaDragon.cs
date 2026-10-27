using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.Isharmla;

[AutoloadBossHead]
public sealed partial class IsharmlaDragon : ModNPC
{
    public int OwnerIndex = -1, Encounter, Pattern, LocalTimer, Dying, Serial;
    public Vector2 Aim, Direction = Vector2.UnitX, Start;
    public readonly IsharmlaSpine Spine = new();
    public Vector2 PortalEntrance, PortalExit, PortalIncoming, PortalOutgoing;
    public bool PortalOpen, PortalCrossed;
    public int PortalExitTimer, AttackSide = 1;
    public float PortalCrossTravel;
    public float Visibility => Spine.Initialized ? Spine.Visibility(0) : 1;
    private int waterCooldown, previousPattern = -1;
    private bool wasAbove;
    private TideStage previousStage;
    public float Jaw, Charge;
    public Vector2 SweepDirection;
    public bool ContactActive;
    private int motionSequence;
    private readonly int[] patternBag = new int[6];
    private readonly IsharmlaSegment[] drawSegments = new IsharmlaSegment[IsharmlaRules.DragonSegments];
    private int bagPosition = 6, bagSize;
    public float FacingAngle => Direction.ToRotation();
    public Vector2 NeckJoint => NeckAt(NPC.Center, Direction);
    private static Vector2 NeckAt(Vector2 center, Vector2 direction) => center + new Vector2(-108, -55).RotatedBy(direction.ToRotation() - IsharmlaRules.HeadArtAngle) * IsharmlaRules.DragonScale;
    public Vector2 Muzzle => NPC.Center + new Vector2(125, 20).RotatedBy(FacingAngle);
    public Isharmla Boss => OwnerIndex >= 0 && OwnerIndex < Main.maxNPCs && Main.npc[OwnerIndex].active &&
        Main.npc[OwnerIndex].ModNPC is Isharmla b && b.Encounter == Encounter ? b : null;
    internal bool Authority => Main.netMode != NetmodeID.MultiplayerClient;
    public override string Texture => Isharmla.Root + "Head";
    public override string BossHeadTexture => Isharmla.Root + "Human";
    public override void SetStaticDefaults()
    {
        NPCID.Sets.MPAllowedEnemies[Type] = true; NPCID.Sets.MustAlwaysDraw[Type] = true;
    }
    public override void SetDefaults()
    {
        NPC.width = 180; NPC.height = 160; NPC.lifeMax = 520000; NPC.defense = 80; NPC.damage = 180;
        NPC.boss = NPC.noGravity = NPC.noTileCollide = NPC.netAlways = true;
        NPC.aiStyle = -1; NPC.knockBackResist = 0; NPC.npcSlots = 10; NPC.value = 0;
        NPC.HitSound = SoundID.NPCHit4; NPC.DeathSound = SoundID.NPCDeath14;
        if (!Main.dedServ) Music = MusicID.Boss5;
    }
    public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment) =>
        NPC.lifeMax = (int)(NPC.lifeMax * .7f * balance * bossAdjustment);
    internal void Bind(Isharmla boss)
    {
        OwnerIndex = boss.NPC.whoAmI; Encounter = boss.Encounter; Start = NPC.Center;
        Direction = -Vector2.UnitY; NPC.rotation = FacingAngle - IsharmlaRules.HeadArtAngle;
        Spine.Reset(NeckJoint, Direction);
        int previous = NPC.whoAmI;
        for (int i = 0; i < IsharmlaRules.DragonSegments; i++)
        {
            int id = Terraria.NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y,
                ModContent.NPCType<IsharmlaSegment>(), NPC.whoAmI);
            if (id >= Main.maxNPCs) break;
            var part = (IsharmlaSegment)Main.npc[id].ModNPC;
            part.HeadIndex = NPC.whoAmI; part.OwnerIndex = OwnerIndex; part.Encounter = Encounter; part.PreviousIndex = previous; part.Order = i;
            Main.npc[id].realLife = NPC.whoAmI; Main.npc[id].lifeMax = NPC.lifeMax; Main.npc[id].life = NPC.life;
            part.Follow(this);
            previous = id; Main.npc[id].netUpdate = true;
        }
        wasAbove = NPC.Center.Y < boss.Anchor.Y;
    }
    public override bool CheckActive() => false;
    public override bool CanHitNPC(NPC target) => false;
    internal bool Contact => Boss is { Transition: 0 } && Dying == 0 && Visibility >= .7f && ContactActive;
    public override bool CanHitPlayer(Player target, ref int cooldownSlot)
    { cooldownSlot = ImmunityCooldownID.Bosses; return Contact; }
    public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
    {
        if (Boss is { } boss) modifiers.SetMaxDamage(IsharmlaRules.HitLimit(NPC.life, IsharmlaRules.DragonFloor(NPC.lifeMax, boss.Stage)));
    }
    public override bool CheckDead()
    {
        if (Boss is not { } boss) return true;
        int floor = IsharmlaRules.DragonFloor(NPC.lifeMax, boss.Stage);
        if (floor > 0) { NPC.life = floor; NPC.netUpdate = true; return false; }
        if (Dying >= 810) return true;
        NPC.life = 1;
        if (Dying == 0 && boss.Timer >= 600)
        { Dying = 1; NPC.netUpdate = true; boss.DragonDefeated(); }
        return false;
    }
    public override void SendExtraAI(BinaryWriter w)
    {
        w.Write(OwnerIndex); w.Write(Encounter); w.Write(Pattern); w.Write(LocalTimer); w.Write(Dying); w.Write(Serial);
        w.WriteVector2(Aim); w.WriteVector2(Direction); w.WriteVector2(Start);
        w.Write(AttackSide); w.Write(PortalOpen); w.Write(PortalCrossed); w.Write(PortalExitTimer); w.Write(PortalCrossTravel);
        w.WriteVector2(PortalEntrance); w.WriteVector2(PortalExit); w.WriteVector2(PortalIncoming); w.WriteVector2(PortalOutgoing);
        w.WriteVector2(SweepDirection); w.Write(motionSequence); w.Write(ContactActive);
        Spine.Write(w);
    }
    public override void ReceiveExtraAI(BinaryReader r)
    {
        OwnerIndex = r.ReadInt32(); Encounter = r.ReadInt32(); Pattern = r.ReadInt32(); LocalTimer = r.ReadInt32(); Dying = r.ReadInt32(); Serial = r.ReadInt32();
        Aim = r.ReadVector2(); Direction = r.ReadVector2(); Start = r.ReadVector2();
        AttackSide = r.ReadInt32(); PortalOpen = r.ReadBoolean(); PortalCrossed = r.ReadBoolean(); PortalExitTimer = r.ReadInt32(); PortalCrossTravel = r.ReadSingle();
        PortalEntrance = r.ReadVector2(); PortalExit = r.ReadVector2(); PortalIncoming = r.ReadVector2(); PortalOutgoing = r.ReadVector2();
        SweepDirection = r.ReadVector2(); motionSequence = r.ReadInt32(); ContactActive = r.ReadBoolean();
        Spine.Read(r);
    }
    public void UpdateSpine() => Spine.Record(NeckJoint, Direction);
    public override void AI() => EncounterAI();
    public override bool PreDraw(SpriteBatch batch, Vector2 screenPos, Color drawColor)
    {
        if (Boss is not { } boss) return false;
        if (PortalOpen)
        {
            IsharmlaVisuals.PortalField(PortalEntrance, PortalIncoming, 145, Math.Min(1, LocalTimer / 45f), PortalCrossed);
            IsharmlaVisuals.PortalField(PortalExit, PortalOutgoing, 145, Math.Min(1, LocalTimer / 60f), PortalCrossed);
        }
        // One ordered draw pass: rear scales under front scales, head over the neck.
        Array.Clear(drawSegments);
        foreach (NPC n in Main.ActiveNPCs)
            if (n.ModNPC is IsharmlaSegment part && part.HeadIndex == NPC.whoAmI && part.Encounter == Encounter && part.Order is >= 0 and < IsharmlaRules.DragonSegments)
                drawSegments[part.Order] = part;
        // Re-pose from one current trail snapshot; individual NPC position packets must not split the visible chain.
        for (int i = 0; i < drawSegments.Length; i++) drawSegments[i]?.Follow(this);
        for (int i = drawSegments.Length - 1; i >= 0; i--) drawSegments[i]?.DrawPart(batch, screenPos);
        IsharmlaPresentation.DrawDragonHead(batch, this, screenPos);
        IsharmlaPresentation.DrawDragonTell(this);
        return false;
    }
}

public sealed class IsharmlaSegment : ModNPC
{
    public int HeadIndex = -1, OwnerIndex = -1, Encounter, PreviousIndex = -1, Order;
    private bool deathBurst;
    private bool waterKnown, aboveWater;
    private int splashCooldown;
    public float Visibility = 1;
    public override string Texture => Isharmla.Root + "Body";
    public IsharmlaDragon Head => HeadIndex >= 0 && HeadIndex < Main.maxNPCs && Main.npc[HeadIndex].active &&
        Main.npc[HeadIndex].ModNPC is IsharmlaDragon d && d.Encounter == Encounter ? d : null;
    public string Art => Order >= 18 ? "TailTip" : Order == 17 ? "TailMid" : Order == 16 ? "TailBase" : Order % 3 == 1 ? "BodyFin" : "Body";
    public Vector2 Origin => Order >= 18 ? new Vector2(190, 120) : Order == 17 ? new Vector2(125, 65) : Order == 16 ? new Vector2(135, 90) : new Vector2(154, 155);
    public Vector2 Front => Order >= 18 ? new Vector2(330, 125) : Order == 17 ? new Vector2(212, 66) : Order == 16 ? new Vector2(238, 96) : new Vector2(267, 187);
    public Vector2 Back => Order >= 18 ? new Vector2(100, 100) : Order == 17 ? new Vector2(28, 67) : Order == 16 ? new Vector2(32, 79) : new Vector2(43, 125);
    private float NativeAngle => Order >= 16 ? .08f : IsharmlaRules.BodyArtAngle;
    public Vector2 RearJoint => NPC.Center + (Back - Origin).RotatedBy(NPC.rotation) * IsharmlaRules.DragonScale;
    public override void SetStaticDefaults() => NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new NPCID.Sets.NPCBestiaryDrawModifiers { Hide = true });
    public override void SetDefaults()
    {
        NPC.width = 120; NPC.height = 106; NPC.lifeMax = 520000; NPC.defense = 80; NPC.damage = 145;
        NPC.noGravity = NPC.noTileCollide = NPC.netAlways = true; NPC.aiStyle = -1; NPC.knockBackResist = 0;
        NPC.npcSlots = 0; NPC.dontCountMe = true; NPC.HitSound = SoundID.NPCHit4;
    }
    public void Follow(IsharmlaDragon head)
    {
        head.UpdateSpine();
        float behind = IsharmlaRules.SegmentDistance(Order);
        IsharmlaSpine.Pose pose = head.Spine.Sample(behind);
        var front = head.Spine.Sample(behind - 58);
        var rear = head.Spine.Sample(behind + 58);
        Vector2 tangent = front.Leg == pose.Leg && rear.Leg == pose.Leg ?
            (front.Position - rear.Position).SafeNormalize(pose.Forward) : pose.Forward;
        NPC.rotation = tangent.ToRotation() - NativeAngle;
        NPC.Center = pose.Position;
        if (Order > 0 && PreviousIndex >= 0 && PreviousIndex < Main.maxNPCs && Main.npc[PreviousIndex].ModNPC is IsharmlaSegment previous &&
            head.Spine.Sample(IsharmlaRules.SegmentDistance(Order - 1)).Leg == pose.Leg)
        {
            float spacing = IsharmlaRules.SegmentDistance(Order) - IsharmlaRules.SegmentDistance(Order - 1);
            Vector2 delta = NPC.Center - previous.NPC.Center;
            if (delta.LengthSquared() > spacing * spacing) NPC.Center = previous.NPC.Center + delta.SafeNormalize(-tangent) * spacing;
        }
        Visibility = head.Spine.Visibility(behind);
        NPC.velocity = Vector2.Zero;
    }
    public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position) => false;
    public override bool CheckActive() => false;
    public override bool CheckDead()
    {
        if (Head is not { } head) return true;
        head.NPC.checkDead(); NPC.life = Math.Max(1, head.NPC.life); return false;
    }
    public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
    {
        if (Head is { } head && head.Boss is { } boss)
        {
            modifiers.FinalDamage *= .62f;
            modifiers.SetMaxDamage(IsharmlaRules.HitLimit(head.NPC.life, IsharmlaRules.DragonFloor(head.NPC.lifeMax, boss.Stage)));
        }
    }
    public override bool CanHitNPC(NPC target) => false;
    public override bool CanHitPlayer(Player target, ref int cooldownSlot)
    { cooldownSlot = ImmunityCooldownID.Bosses; return Visibility >= .7f && Head is { } head && head.Contact && head.NPC.damage > 0; }
    public override void AI()
    {
        var head = Head;
        if (head == null || PreviousIndex < 0 || PreviousIndex >= Main.maxNPCs || !Main.npc[PreviousIndex].active)
        { if (Main.netMode != NetmodeID.MultiplayerClient) IsharmlaWorld.Deactivate(NPC); return; }
        NPC.realLife = HeadIndex; NPC.timeLeft = 1800; NPC.dontTakeDamage = head.NPC.dontTakeDamage;
        NPC.damage = Math.Max(0, head.NPC.damage - 30);
        NPC.life = Math.Max(1, head.NPC.life); NPC.lifeMax = head.NPC.lifeMax;
        Follow(head);
        SegmentWater(head);
        NPC.dontTakeDamage |= Visibility < .65f;
        if (Visibility < .7f) NPC.damage = 0;
        head.FireSegment(this);
        if (!Main.dedServ && Visibility > .7f && Order % 3 == 0) IsharmlaVisuals.DragonWake(NPC.Center, head.NPC.velocity, 63, Order, head.Boss?.Stage == TideStage.LastTide);
        if (!Main.dedServ && head.Boss is { Transition: > 0 } && Order % 3 == 0)
            IsharmlaVisuals.Mote(NPC.Center + Main.rand.NextVector2Circular(90, 65), new Vector2(0, -1), 1.1f);
        if (head.Dying > 0)
        {
            NPC.damage = 0; NPC.dontTakeDamage = true;
            int disappear = 550 + (IsharmlaRules.DragonSegments - 1 - Order) * 12;
            if (head.Dying >= disappear && !deathBurst)
            { deathBurst = true; if (!Main.dedServ) IsharmlaVisuals.Burst(NPC.Center, 22); }
        }
    }
    public override void SendExtraAI(BinaryWriter w)
    { w.Write(HeadIndex); w.Write(OwnerIndex); w.Write(Encounter); w.Write(PreviousIndex); w.Write(Order); }
    public override void ReceiveExtraAI(BinaryReader r)
    { HeadIndex = r.ReadInt32(); OwnerIndex = r.ReadInt32(); Encounter = r.ReadInt32(); PreviousIndex = r.ReadInt32(); Order = r.ReadInt32(); }
    private void SegmentWater(IsharmlaDragon head)
    {
        if (head.Boss is not { } boss) return;
        bool above = NPC.Center.Y < boss.Anchor.Y;
        if (splashCooldown > 0) splashCooldown--;
        if (waterKnown && above != aboveWater && splashCooldown == 0 && Order % 6 == 0 && Visibility > .7f)
        {
            splashCooldown = 90;
            Vector2 at = new(NPC.Center.X, boss.Anchor.Y);
            if (!Main.dedServ) TidalParticles.Splash(at, !above, Order == 18 ? .85f : .55f);
            if (head.Authority && above && head.Dying == 0 && head.Pattern == 0 && head.LocalTimer is >= 90 and < 380 && boss.Transition == 0)
                for (int i = -3; i <= 3; i++)
                    if (i != 0) boss.SpawnShot(TideShot.FoamArc, at, new Vector2(i * 2, -6 - Math.Abs(i)),
                        90, 48, duration: 145, style: 2);
        }
        aboveWater = above; waterKnown = true;
    }
    public override bool PreDraw(SpriteBatch batch, Vector2 screenPos, Color drawColor)
        => false; // The head draws the complete worm in a stable overlap order.
    internal void DrawPart(SpriteBatch batch, Vector2 screenPos)
    {
        if (Head is not { } head || head.Boss is not { } boss || deathBurst || Visibility <= .001f) return;
        IsharmlaVisuals.DrawDragonPart(batch, Art, NPC.Center, Origin, NPC.rotation, IsharmlaRules.DragonScale, boss.Stage, Visibility, screenPos);
        float pulse = head.SegmentCharge(Order);
        if (pulse > 0) IsharmlaVisuals.Orb(NPC.Center, new Vector2(22 + pulse * 18, 15 + pulse * 10), IsharmlaVisuals.Crest * (pulse * .65f));
    }
}
