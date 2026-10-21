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
public sealed class IsharmlaDragon : ModNPC
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
    internal bool Contact => Boss is { } boss && Dying == 0 && boss.Transition == 0 && Visibility >= .7f &&
        (boss.Stage == TideStage.LastSong || LocalTimer >= 65 || boss.Stage == TideStage.Leviathan && boss.Timer < 48);
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
        if (Dying >= 210) return true;
        NPC.life = 1; if (Dying == 0) { Dying = 1; NPC.netUpdate = true; boss.Cleanup(false); }
        return false;
    }
    public override void SendExtraAI(BinaryWriter w)
    {
        w.Write(OwnerIndex); w.Write(Encounter); w.Write(Pattern); w.Write(LocalTimer); w.Write(Dying); w.Write(Serial);
        w.WriteVector2(Aim); w.WriteVector2(Direction); w.WriteVector2(Start);
        w.Write(AttackSide); w.Write(PortalOpen); w.Write(PortalCrossed); w.Write(PortalExitTimer); w.Write(PortalCrossTravel);
        w.WriteVector2(PortalEntrance); w.WriteVector2(PortalExit); w.WriteVector2(PortalIncoming); w.WriteVector2(PortalOutgoing);
        Spine.Write(w);
    }
    public override void ReceiveExtraAI(BinaryReader r)
    {
        OwnerIndex = r.ReadInt32(); Encounter = r.ReadInt32(); Pattern = r.ReadInt32(); LocalTimer = r.ReadInt32(); Dying = r.ReadInt32(); Serial = r.ReadInt32();
        Aim = r.ReadVector2(); Direction = r.ReadVector2(); Start = r.ReadVector2();
        AttackSide = r.ReadInt32(); PortalOpen = r.ReadBoolean(); PortalCrossed = r.ReadBoolean(); PortalExitTimer = r.ReadInt32(); PortalCrossTravel = r.ReadSingle();
        PortalEntrance = r.ReadVector2(); PortalExit = r.ReadVector2(); PortalIncoming = r.ReadVector2(); PortalOutgoing = r.ReadVector2();
        Spine.Read(r);
    }
    public void UpdateSpine() => Spine.Record(NeckJoint, Direction);
    public override void AI()
    {
        Isharmla boss = Boss;
        if (boss == null) { if (Authority) IsharmlaWorld.Deactivate(NPC); return; }
        UpdateSpine();
        if (PortalOpen && PortalCrossed && Spine.Travel - PortalCrossTravel > IsharmlaRules.SegmentDistance(18) + 180)
        { PortalOpen = false; if (Authority) NPC.netUpdate = true; }
        NPC.target = boss.NPC.target; Player p = Main.player[NPC.target]; NPC.timeLeft = 1800;
        if (previousStage != boss.Stage)
        {
            previousStage = boss.Stage; LocalTimer = 0;
            if (!PortalCrossed) PortalOpen = false;
            if (Authority && boss.Stage is TideStage.Covenant or TideStage.LastTide) SelectPattern(boss.Stage == TideStage.LastTide);
        }
        if (Dying > 0)
        {
            NPC.dontTakeDamage = true; NPC.damage = 0; NPC.velocity *= .94f; Dying++;
            if (!Main.dedServ && Dying % 12 == 0) IsharmlaVisuals.Burst(NPC.Center, 18);
            if (Dying >= 210 && Authority)
            {
                boss.DragonDefeated(); NPC.life = 0; NPC.checkDead(); NPC.netUpdate = true;
            }
            return;
        }
        int floor = IsharmlaRules.DragonFloor(NPC.lifeMax, boss.Stage);
        NPC.life = Math.Max(Math.Max(1, floor), NPC.life);
        NPC.dontTakeDamage = boss.Transition > 0 || !IsharmlaRules.DragonVulnerable(boss.Stage) || floor > 0 && NPC.life <= floor || Visibility < .65f;
        NPC.damage = boss.Stage == TideStage.LastTide ? 210 : 170;
        if (boss.Transition > 0)
        {
            NPC.damage = 0; NPC.velocity *= .94f;
            if (!Main.dedServ && boss.Transition % 5 == 0)
                IsharmlaVisuals.Mote(NPC.Center + Main.rand.NextVector2Circular(120, 100), Main.rand.NextVector2Circular(1, 2), 1.8f);
            if (boss.Stage == TideStage.Leviathan)
            { NPC.Center = boss.Portal + new Vector2(0, 650); Direction = -Vector2.UnitY; NPC.velocity = Vector2.Zero; }
            else if (boss.Stage == TideStage.Covenant)
            {
                Steer(boss.Anchor + new Vector2(AttackSide * 850, -600), 16, .032f);
                Face();
            }
            NPC.rotation = FacingAngle - IsharmlaRules.HeadArtAngle; return;
        }
        if (boss.Stage == TideStage.Leviathan && boss.Timer < 48)
        {
            NPC.damage = 9999; NPC.velocity = -Vector2.UnitY * 42; Direction = -Vector2.UnitY;
            NPC.rotation = FacingAngle - IsharmlaRules.HeadArtAngle; return;
        }
        if (boss.Stage == TideStage.LastSong)
        {
            LocalTimer++;
            Vector2 destination = boss.Anchor + new Vector2(MathF.Sin(LocalTimer * .006f) * 1050, -420 + MathF.Sin(LocalTimer * .011f) * 220);
            Steer(destination, 12, .035f); Face(); return;
        }
        if (boss.Stage == TideStage.Finale) { NPC.active = false; return; }
        LocalTimer++;
        bool final = boss.Stage == TideStage.LastTide;
        int duration = Pattern == 5 ? 330 : Pattern == 3 && boss.Stage != TideStage.Leviathan ? 320 : final ? 245 : 285;
        if (LocalTimer >= duration && Authority) SelectPattern(final);
        if (LocalTimer <= 1 && Authority)
        {
            Aim = p.Center + p.velocity * 12; Start = NPC.Center;
            AttackSide = NPC.Center.X >= p.Center.X ? 1 : -1; NPC.netUpdate = true;
        }
        switch (Pattern)
        {
            case 0: Horizontal(boss, p, final); break;
            case 1: Breach(boss, p, final); break;
            case 2: Arc(boss, p, final); break;
            case 3: GateCharge(boss, p, final); break;
            case 4: Dive(boss, p, final); break;
            case 5: Breath(boss, p); break;
        }
        Face(); WaterEvents(boss, p);
        if (!Main.dedServ) IsharmlaVisuals.DragonWake(NPC.Center, NPC.velocity, 85, 0, boss.Stage == TideStage.LastTide);
        if (Authority && LocalTimer % 60 == 0) NPC.netUpdate = true;
    }
    private void SelectPattern(bool final)
    {
        int count = final ? 6 : 5;
        if (bagPosition >= count || bagSize != count)
        {
            for (int i = 0; i < count; i++) patternBag[i] = i;
            for (int i = count - 1; i > 0; i--)
            {
                int j = Main.rand.Next(i + 1); (patternBag[i], patternBag[j]) = (patternBag[j], patternBag[i]);
            }
            if (patternBag[0] == previousPattern) (patternBag[0], patternBag[1]) = (patternBag[1], patternBag[0]);
            bagPosition = 0; bagSize = count;
        }
        previousPattern = Pattern = patternBag[bagPosition++]; LocalTimer = 0; Serial++; NPC.netUpdate = true;
    }
    private void Snapshot(Player p)
    { if (Authority) { Aim = p.Center + p.velocity * 12; NPC.netUpdate = true; } }
    private void Steer(Vector2 destination, float speed, float turn, bool stayAirborne = true)
    {
        if (stayAirborne && Boss is { Stage: TideStage.Covenant } boss)
        {
            destination.Y = Math.Min(destination.Y, boss.Anchor.Y - 240);
            if (NPC.Center.Y > boss.Anchor.Y - 200) destination.Y = boss.Anchor.Y - 500;
        }
        float current = NPC.velocity.Length();
        float heading = current > .2f ? NPC.velocity.ToRotation() : FacingAngle;
        float desired = (destination - NPC.Center).SafeNormalize(Direction).ToRotation();
        float angularLimit = Math.Min(turn, Boss?.Stage == TideStage.LastTide ? .046f : .033f);
        float error = Math.Abs(MathHelper.WrapAngle(desired - heading));
        float targetSpeed = speed * MathHelper.Lerp(1, .42f, MathHelper.Clamp(error / MathHelper.PiOver2, 0, 1));
        float nextSpeed = MathHelper.Lerp(current, targetSpeed, .055f);
        NPC.velocity = heading.AngleTowards(desired, angularLimit).ToRotationVector2() * Math.Max(3, nextSpeed);
    }
    private void Face()
    {
        if (NPC.velocity.LengthSquared() > 1 && Pattern != 5) Direction = NPC.velocity.SafeNormalize(Direction);
        NPC.rotation = FacingAngle - IsharmlaRules.HeadArtAngle;
    }
    private void Horizontal(Isharmla boss, Player p, bool final)
    {
        if (LocalTimer < 40) Steer(Aim + new Vector2(AttackSide * 850, -160), final ? 27 : 21, .032f);
        if (LocalTimer == 40) Snapshot(p);
        if (LocalTimer is >= 40 and < 120) Steer(Aim + (Aim - Start).SafeNormalize(Direction) * 650, final ? 39 : 29, LocalTimer < 65 ? .032f : .012f);
        if (LocalTimer >= 120) Steer(Aim + new Vector2(-AttackSide * 1000, -420), 20, .026f);
        if (LocalTimer == 150) boss.Fan(Muzzle, p.Center, final ? 7 : 5, 1.45f, 10);
    }
    private void Breach(Isharmla boss, Player p, bool final)
    {
        float bottom = boss.Stage == TideStage.Covenant ? -160 : 330;
        if (LocalTimer < 40) Steer(new Vector2(Aim.X - AttackSide * 430, boss.Anchor.Y + bottom), 22, .033f);
        if (LocalTimer == 40) Snapshot(p);
        if (LocalTimer is >= 40 and < 145) Steer(Aim - new Vector2(0, 700), final ? 37 : 27, .030f);
        if (LocalTimer >= 145) Steer(Aim + new Vector2(AttackSide * 850, -500), 20, .025f);
    }
    private void Arc(Isharmla boss, Player p, bool final)
    {
        float angle = LocalTimer * (final ? .023f : .017f) * AttackSide;
        Vector2 destination = Aim + new Vector2(MathF.Cos(angle) * 760, -420 + MathF.Sin(angle) * 290);
        Steer(destination, final ? 31 : 23, .029f);
    }
    private void GateCharge(Isharmla boss, Player p, bool final)
    {
        // The solo introduction teaches ordinary movement first; portals start with the duet.
        if (boss.Stage == TideStage.Leviathan)
        {
            // Fifth solo move: a gliding broadside with mouth-fired water fans.
            Steer(Aim + new Vector2(AttackSide * (760 - LocalTimer * 5), -280), 19, .026f);
            if (LocalTimer is >= 80 and <= 200 && LocalTimer % 40 == 0) boss.Fan(Muzzle, p.Center, 9, 1.7f, 11);
            return;
        }
        if (LocalTimer <= 1 && Authority)
        {
            PortalIncoming = NPC.velocity.SafeNormalize(Direction);
            PortalEntrance = NPC.Center + PortalIncoming * Math.Max(1900, NPC.velocity.Length() * 75 + 300);
            if (boss.Stage == TideStage.Covenant) PortalEntrance.Y = Math.Min(PortalEntrance.Y, boss.Anchor.Y - 270);
            PortalIncoming = (PortalEntrance - NPC.Center).SafeNormalize(Direction);
            PortalExit = Aim + new Vector2(-AttackSide * 900, final ? -320 : -210);
            PortalOutgoing = (Aim - PortalExit).SafeNormalize(Vector2.UnitX);
            PortalOpen = true; PortalCrossed = false; NPC.netUpdate = true;
        }
        if (!PortalOpen) { Steer(Aim + new Vector2(AttackSide * 900, -500), 23, .025f); return; }
        if (!PortalCrossed)
        {
            Steer(PortalEntrance, final ? 32 : 25, .03f, false);
            float remaining = Vector2.Distance(NPC.Center, PortalEntrance);
            // Dock into the actual aperture instead of orbiting a point inside the turning radius.
            if (LocalTimer >= 65 && remaining < 500)
                NPC.velocity = Vector2.Lerp(NPC.velocity, (PortalEntrance - NPC.Center).SafeNormalize(PortalIncoming) *
                    MathHelper.Clamp(remaining * .09f, 7, final ? 32 : 25), .10f);
            if (LocalTimer >= 65 && Vector2.Distance(NPC.Center, PortalEntrance) < NPC.velocity.Length() + 45)
            {
                Spine.CrossPortal(NeckAt(PortalEntrance, Direction), NeckAt(PortalExit, PortalOutgoing), Direction, PortalOutgoing);
                NPC.Center = PortalExit; Direction = PortalOutgoing; NPC.velocity = Direction * (final ? 43 : 34);
                PortalCrossed = true; PortalExitTimer = LocalTimer; PortalCrossTravel = Spine.Travel;
                NPC.netUpdate = Authority;
            }
        }
        else if (LocalTimer < PortalExitTimer + 60)
            NPC.velocity = PortalOutgoing * (final ? 43 : 34);
        else Steer(Aim + new Vector2(AttackSide * 1000, -520), 24, .026f);
    }
    private void Dive(Isharmla boss, Player p, bool final)
    {
        if (LocalTimer < 45) Steer(Aim + new Vector2(AttackSide * 550, -620), 22, .032f);
        if (LocalTimer == 45) Snapshot(p);
        if (LocalTimer is >= 45 and < 150) Steer(Aim + new Vector2(-AttackSide * 500, 150), final ? 39 : 25, .029f);
        if (LocalTimer is >= 95 and <= 155 && LocalTimer % 20 == 15)
            boss.SpawnShot(TideShot.Mine, NPC.Center - Direction * 160, Vector2.Zero, 100, 75, 0, 22, 150);
        if (LocalTimer >= 150)
        {
            Vector2 destination = final ? new Vector2(Aim.X - AttackSide * 600, boss.Anchor.Y + 300) :
                Aim + new Vector2(-AttackSide * 850, -330);
            Steer(destination, 19, .04f);
        }
    }
    private void Breath(Isharmla boss, Player p)
    {
        if (LocalTimer < 85)
        {
            Steer(Aim + new Vector2(AttackSide * 720, -180), 18, .026f);
            Direction = FacingAngle.AngleTowards((p.Center - Muzzle).ToRotation(), .022f).ToRotationVector2();
        }
        if (LocalTimer == 85)
        {
            NPC.velocity *= .1f;
            boss.SpawnShot(TideShot.Breath, Muzzle, Direction, 165, 70, 2500, 64, 135);
        }
        if (LocalTimer >= 85)
        {
            NPC.velocity *= .94f;
            float target = (p.Center - Muzzle).ToRotation();
            Direction = FacingAngle.AngleTowards(target, IsharmlaRules.BreathTurnRate(Vector2.Distance(p.Center, Muzzle))).ToRotationVector2();
        }
    }
    private void WaterEvents(Isharmla boss, Player p)
    {
        bool above = NPC.Center.Y < boss.Anchor.Y;
        if (waterCooldown > 0) waterCooldown--;
        if (above != wasAbove && waterCooldown == 0)
        {
            waterCooldown = 100;
            if (!Main.dedServ) TidalParticles.Splash(new Vector2(NPC.Center.X, boss.Anchor.Y), !above, 1.3f);
            if (above)
            {
                for (int i = -2; i <= 2; i++)
                {
                    if (i == 0) continue;
                    boss.SpawnShot(TideShot.Column, new Vector2(p.Center.X + i * 230, p.Center.Y - 900), Vector2.UnitY, 110, 80, 1600, 42, 32);
                }
            }
            else
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    boss.SpawnShot(TideShot.Wave, new Vector2(NPC.Center.X, boss.Anchor.Y), new Vector2(side * 12, 0), 110, 20);
                    boss.SpawnSeaborn(1, NPC.Center, new Vector2(side * 9, -13));
                }
            }
        }
        wasAbove = above;
    }
    public override bool PreDraw(SpriteBatch batch, Vector2 screenPos, Color drawColor)
    {
        if (Boss is not { } boss) return false;
        if (PortalOpen)
        {
            IsharmlaVisuals.PortalField(PortalEntrance, PortalIncoming, 145, Math.Min(1, LocalTimer / 45f), PortalCrossed);
            IsharmlaVisuals.PortalField(PortalExit, PortalOutgoing, 145, Math.Min(1, LocalTimer / 60f), PortalCrossed);
            if (!PortalCrossed) IsharmlaVisuals.Line(PortalExit, PortalExit + PortalOutgoing * 1600, IsharmlaVisuals.Gold * .4f, 2);
        }
        // One ordered draw pass: rear scales under front scales, head over the neck.
        Array.Clear(drawSegments);
        foreach (NPC n in Main.ActiveNPCs)
            if (n.ModNPC is IsharmlaSegment part && part.HeadIndex == NPC.whoAmI && part.Encounter == Encounter && part.Order is >= 0 and < IsharmlaRules.DragonSegments)
                drawSegments[part.Order] = part;
        for (int i = drawSegments.Length - 1; i >= 0; i--) drawSegments[i]?.DrawPart(batch, screenPos);
        IsharmlaVisuals.DrawDragonPart(batch, "Head", NPC.Center, new Vector2(220, 220), NPC.rotation,
            IsharmlaRules.DragonScale, boss.Stage, (Dying > 0 ? 1 - Dying / 215f : 1) * Visibility, screenPos);
        if (Dying == 0 && boss.Transition == 0 && LocalTimer is >= 40 and < 65 && Pattern is 0 or 1 or 4)
            IsharmlaVisuals.Line(Muzzle, Main.player[NPC.target].Center, IsharmlaVisuals.Gold * .35f, 2);
        if (Pattern == 5 && LocalTimer is >= 85 and < 155) IsharmlaVisuals.Ring(Muzzle, 35 + (155 - LocalTimer) * .45f, IsharmlaVisuals.Cyan * .8f, 3);
        return false;
    }
}

public sealed class IsharmlaSegment : ModNPC
{
    public int HeadIndex = -1, OwnerIndex = -1, Encounter, PreviousIndex = -1, Order;
    private bool deathBurst;
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
        NPC.damage = head.NPC.damage == 9999 ? 0 : Math.Max(0, head.NPC.damage - 30);
        NPC.life = Math.Max(1, head.NPC.life); NPC.lifeMax = head.NPC.lifeMax;
        Follow(head);
        NPC.dontTakeDamage |= Visibility < .65f;
        if (Visibility < .7f) NPC.damage = 0;
        if (head.Boss is { Transition: 0 } boss && head.Dying == 0 && head.Pattern == 2 &&
            boss.Stage is TideStage.Leviathan or TideStage.Covenant or TideStage.LastTide &&
            Order < 16 && Order % 3 == 1 && head.LocalTimer == 80 + Order * 5)
        {
            Vector2 normal = (NPC.rotation + NativeAngle + MathHelper.PiOver2).ToRotationVector2();
            Player target = Main.player[head.NPC.target];
            if (Vector2.Dot(normal, target.Center - NPC.Center) < 0) normal = -normal;
            boss.Fan(NPC.Center + normal * 64, target.Center, boss.Stage == TideStage.LastTide ? 5 : 3, .7f, 10);
        }
        if (!Main.dedServ && Visibility > .7f && Order % 3 == 0) IsharmlaVisuals.DragonWake(NPC.Center, head.NPC.velocity, 63, Order, head.Boss?.Stage == TideStage.LastTide);
        if (!Main.dedServ && head.Boss is { Transition: > 0 } && Order % 3 == 0)
            IsharmlaVisuals.Mote(NPC.Center + Main.rand.NextVector2Circular(90, 65), new Vector2(0, -1), 1.1f);
        if (head.Dying > 0)
        {
            NPC.damage = 0; NPC.dontTakeDamage = true;
            int disappear = 12 + (IsharmlaRules.DragonSegments - 1 - Order) * 9;
            if (head.Dying >= disappear && !deathBurst)
            { deathBurst = true; if (!Main.dedServ) IsharmlaVisuals.Burst(NPC.Center, 22); }
        }
    }
    public override void SendExtraAI(BinaryWriter w)
    { w.Write(HeadIndex); w.Write(OwnerIndex); w.Write(Encounter); w.Write(PreviousIndex); w.Write(Order); }
    public override void ReceiveExtraAI(BinaryReader r)
    { HeadIndex = r.ReadInt32(); OwnerIndex = r.ReadInt32(); Encounter = r.ReadInt32(); PreviousIndex = r.ReadInt32(); Order = r.ReadInt32(); }
    public override bool PreDraw(SpriteBatch batch, Vector2 screenPos, Color drawColor)
        => false; // The head draws the complete worm in a stable overlap order.
    internal void DrawPart(SpriteBatch batch, Vector2 screenPos)
    {
        if (Head is not { } head || head.Boss is not { } boss || deathBurst || Visibility <= .001f) return;
        IsharmlaVisuals.DrawDragonPart(batch, Art, NPC.Center, Origin, NPC.rotation, IsharmlaRules.DragonScale, boss.Stage, Visibility, screenPos);
        if (head.Pattern == 2 && Order < 16 && Order % 3 == 1 && head.LocalTimer >= 58 + Order * 5 && head.LocalTimer < 80 + Order * 5)
            IsharmlaVisuals.Orb(NPC.Center, new Vector2(12), IsharmlaVisuals.Gold * .8f);
    }
}
