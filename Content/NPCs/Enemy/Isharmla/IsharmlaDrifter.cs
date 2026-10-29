using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.Isharmla;

public sealed class IsharmlaDrifter : ModNPC
{
    public const int LaunchTime = 20, TrackingEnd = 110, Lifetime = 210, PopulationLimit = 12;
    public const float MaximumTurn = .018f, TurnBudget = 1.15f, CommitDistance = 280;
    public int OwnerIndex = -1, Encounter, Identity, Age, CurveSide = 1;
    public float TurnSpent, CruiseSpeed = 13.8f;
    public bool Committed;
    private static int nextIdentity;
    private bool impactReported;
    public override string Texture => Isharmla.Root + "BoneSeaDrifter";
    internal Isharmla Boss => OwnerIndex >= 0 && OwnerIndex < Main.maxNPCs && Main.npc[OwnerIndex].active &&
        Main.npc[OwnerIndex].ModNPC is Isharmla b && b.Encounter == Encounter ? b : null;
    private bool Authority => Main.netMode != NetmodeID.MultiplayerClient;

    internal void Bind(Isharmla boss, Vector2 velocity, int curveSide)
    {
        OwnerIndex = boss.NPC.whoAmI; Encounter = boss.Encounter; Identity = unchecked(++nextIdentity);
        CurveSide = curveSide < 0 ? -1 : 1; NPC.velocity = velocity; NPC.target = boss.NPC.target;
        bool fierce = boss.Stage is TideStage.LastSong or TideStage.LastTide;
        CruiseSpeed = fierce ? 15 : 13.8f; NPC.damage = fierce ? 110 : 95; NPC.netUpdate = true;
    }
    public override void SetStaticDefaults()
    {
        NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new NPCID.Sets.NPCBestiaryDrawModifiers { Hide = true });
        NPCID.Sets.TrailCacheLength[Type] = 7; NPCID.Sets.TrailingMode[Type] = 1;
    }
    public override void SetDefaults()
    {
        NPC.width = 32; NPC.height = 24; NPC.lifeMax = 850; NPC.defense = 18; NPC.damage = 95;
        NPC.aiStyle = -1; NPC.noGravity = true; NPC.noTileCollide = true; NPC.knockBackResist = .1f;
        NPC.npcSlots = 0; NPC.value = 0; NPC.netAlways = true; NPC.scale = 1.25f;
        NPC.HitSound = SoundID.NPCHit2; NPC.DeathSound = SoundID.NPCDeath19;
    }
    public override bool CheckActive() => false;
    public override bool CanHitPlayer(Player target, ref int cooldownSlot)
    {
        cooldownSlot = ImmunityCooldownID.Bosses;
        return !impactReported && Boss is { Transition: 0 } boss && boss.Stage != TideStage.Finale && Age > LaunchTime && Age <= Lifetime - 24;
    }
    public override void AI()
    {
        var boss = Boss;
        if (boss == null || boss.Transition > 0 || boss.Stage == TideStage.Finale || ++Age > Lifetime)
        { if (Authority) IsharmlaWorld.Deactivate(NPC); return; }
        NPC.timeLeft = 1800;
        NPC.damage = CruiseSpeed > 14 ? 110 : 95;
        Player target = Main.player[boss.NPC.target];
        if (!target.active || target.dead)
        { if (Authority) IsharmlaWorld.Deactivate(NPC); return; }
        NPC.target = target.whoAmI;
        Vector2 offset = target.Center - NPC.Center;
        float heading = NPC.velocity.SafeNormalize(Vector2.UnitY).ToRotation();
        float speed = Math.Min(CruiseSpeed, NPC.velocity.Length() + .16f);
        if (Age <= LaunchTime)
            heading -= CurveSide * .012f * (1 - Age / (float)LaunchTime);
        else if (!Committed)
        {
            // No leading the player. Near the target, or once passed, this becomes a dodgeable straight shot.
            bool passed = Age > LaunchTime + 12 && Vector2.Dot(offset, heading.ToRotationVector2()) <= 0;
            if (offset.LengthSquared() < CommitDistance * CommitDistance || Age >= TrackingEnd || TurnSpent >= TurnBudget || passed)
            {
                Committed = true;
                if (Authority) NPC.netUpdate = true;
            }
            else
            {
                float turn = MaximumTurn * MathHelper.Lerp(.35f, 1, MathHelper.Clamp((offset.Length() - CommitDistance) / 500, 0, 1));
                turn = Math.Min(turn, TurnBudget - TurnSpent);
                float next = heading.AngleTowards(offset.ToRotation(), turn);
                TurnSpent += Math.Abs(MathHelper.WrapAngle(next - heading)); heading = next;
            }
        }
        NPC.velocity = heading.ToRotationVector2() * speed;
        NPC.spriteDirection = NPC.velocity.X >= 0 ? 1 : -1;
        NPC.rotation = heading + (NPC.spriteDirection == -1 ? MathHelper.Pi : 0);
        if (!Main.dedServ && Age % 5 == 0)
            TidalParticles.Emit(TideParticle.Bubble, NPC.Center - NPC.velocity * 1.6f, -NPC.velocity * .08f,
                new Vector2(2.5f), IsharmlaVisuals.Cyan, 22);
    }
    public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
    {
        if (impactReported) return;
        impactReported = true;
        if (Authority) SelfDestruct();
        else
        {
            // OnHitPlayer only runs on the injured player's client. The server owns the actual NPC death.
            var packet = Mod.GetPacket();
            packet.Write((short)global::ArknightsMod.ArknightsMod.ArkMessageID.IsharmlaDrifterImpact);
            packet.Write((short)NPC.whoAmI); packet.Write(Encounter); packet.Write(Identity); packet.Send();
        }
    }
    internal static void ReceiveImpact(BinaryReader reader, int sender)
    {
        int slot = reader.ReadInt16(), encounter = reader.ReadInt32(), identity = reader.ReadInt32();
        if (Main.netMode != NetmodeID.Server || (uint)sender >= Main.maxPlayers || (uint)slot >= Main.maxNPCs) return;
        Player player = Main.player[sender]; NPC npc = Main.npc[slot];
        if (!player.active || player.dead || !npc.active || npc.ModNPC is not IsharmlaDrifter d ||
            d.Encounter != encounter || d.Identity != identity || d.Boss is not { Transition: 0 } boss || boss.Stage == TideStage.Finale ||
            d.Age <= LaunchTime || d.Age > Lifetime || Vector2.DistanceSquared(player.Center, npc.Center) > 160 * 160) return;
        d.SelfDestruct();
    }
    private void SelfDestruct()
    {
        var hit = new NPC.HitInfo { InstantKill = true, Damage = 1, HitDirection = 0 };
        NPC.StrikeNPC(hit);
        if (Main.netMode == NetmodeID.Server) NetMessage.SendStrikeNPC(NPC, hit);
    }
    public override void HitEffect(NPC.HitInfo hit)
    {
        if (Main.dedServ || NPC.life > 0) return;
        for (int i = 0; i < 10; i++)
        {
            var dust = Dust.NewDustPerfect(NPC.Center, DustID.Water, Main.rand.NextVector2Circular(3, 3), 100, default, .9f);
            dust.noGravity = true;
        }
    }
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(OwnerIndex); writer.Write(Encounter); writer.Write(Identity); writer.Write(Age); writer.Write(CurveSide);
        writer.Write(TurnSpent); writer.Write(CruiseSpeed); writer.Write(Committed);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        OwnerIndex = reader.ReadInt32(); Encounter = reader.ReadInt32(); Identity = reader.ReadInt32(); Age = reader.ReadInt32(); CurveSide = reader.ReadInt32();
        TurnSpent = reader.ReadSingle(); CruiseSpeed = reader.ReadSingle(); Committed = reader.ReadBoolean();
    }
    public override bool PreDraw(SpriteBatch batch, Vector2 screenPos, Color drawColor)
    {
        Texture2D art = IsharmlaVisuals.Art("BoneSeaDrifter");
        SpriteEffects flip = NPC.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        float opacity = IsharmlaRules.Smooth(Age / 12f) * (1 - IsharmlaRules.Smooth((Age - Lifetime + 24) / 24f));
        for (int i = NPC.oldPos.Length - 1; i >= 1; i -= 2)
            if (NPC.oldPos[i] != Vector2.Zero)
                batch.Draw(art, NPC.oldPos[i] + NPC.Size * .5f - screenPos, null, new Color(125, 200, 230) * (opacity * .20f * (1 - i / (float)NPC.oldPos.Length)),
                    NPC.rotation, art.Size() * .5f, NPC.scale, flip, 0);
        batch.Draw(art, NPC.Center - screenPos, null, Color.Lerp(drawColor, Color.White, .65f) * opacity,
            NPC.rotation, art.Size() * .5f, NPC.scale, flip, 0);
        return false;
    }
}
