using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.FaustAndMephisto;

internal static class DuoEncounter
{
    private static int nextToken;
    internal static bool Authority => Main.netMode != NetmodeID.MultiplayerClient;
    internal static bool Alive(Player p) => p != null && p.active && !p.dead && !p.ghost;
    internal static Faust Owner(float slot, float token)
    {
        int index = (int)slot - 1;
        return index >= 0 && index < Main.maxNPCs && Main.npc[index].active &&
            Main.npc[index].ModNPC is Faust f && f.NPC.ai[3] == token && token > 0 ? f : null;
    }
    internal static Mephisto Partner(Faust f)
    {
        if (f == null) return null;
        int index = (int)f.NPC.ai[0] - 1;
        return index >= 0 && index < Main.maxNPCs && Main.npc[index].active &&
            Main.npc[index].ModNPC is Mephisto m && m.NPC.ai[3] == f.NPC.ai[3] ? m : null;
    }
    internal static Vector2 GroundNear(Vector2 wanted, int width, int height)
    {
        wanted.X = MathHelper.Clamp(wanted.X, 160, Main.maxTilesX * 16 - 160);
        wanted.Y = MathHelper.Clamp(wanted.Y, 160, Main.maxTilesY * 16 - 200);
        int x = (int)(wanted.X / 16), y = (int)(wanted.Y / 16);
        for (int offset = -10; offset <= 42; offset++)
        {
            int ty = y + offset;
            if (!WorldGen.InWorld(x, ty, 10) || !WorldGen.SolidTile(x, ty)) continue;
            Vector2 point = new(wanted.X, ty * 16 - height * .5f - 2);
            if (!Collision.SolidCollision(point - new Vector2(width, height) * .5f, width, height)) return point;
        }
        for (int offset = 0; offset <= 40; offset++)
        {
            Vector2 point = wanted - new Vector2(0, offset * 16);
            if (!Collision.SolidCollision(point - new Vector2(width, height) * .5f, width, height)) return point;
        }
        return wanted;
    }
    internal static void Initialize(NPC seed)
    {
        if (!Authority || seed.ai[3] != 0) return;
        seed.TargetClosest();
        if (seed.target < 0 || seed.target >= Main.maxPlayers || !Alive(Main.player[seed.target])) { Remove(seed); return; }
        Player target = Main.player[seed.target];
        int token = nextToken = nextToken % 8000000 + 1; // Exact when carried in float NPC AI slots.
        seed.ai[3] = token;
        int partnerType = seed.ModNPC is Faust ? ModContent.NPCType<Mephisto>() : ModContent.NPCType<Faust>();
        int index = NPC.NewNPC(seed.GetSource_FromAI(), (int)seed.Center.X, (int)seed.Center.Y, partnerType,
            ai0: seed.whoAmI + 1, ai3: token);
        if (index >= Main.maxNPCs) { Remove(seed); return; }
        NPC other = Main.npc[index]; seed.ai[0] = index + 1;
        Faust faust = (seed.ModNPC as Faust) ?? (Faust)other.ModNPC;
        faust.IncludeParticipant(target.whoAmI);
        Mephisto mephisto = (seed.ModNPC as Mephisto) ?? (Mephisto)other.ModNPC;
        mephisto.NPC.Center = GroundNear(target.Center + new Vector2(320, 0), 32, 52);
        faust.Arena = mephisto.NPC.Center;
        faust.NPC.Center = GroundNear(target.Center + new Vector2(-1000, -80), 32, 52);
        faust.NPC.ai[2] = -90;
        faust.NPC.dontTakeDamage = true;
        seed.netUpdate = other.netUpdate = true;
        for (int i = 0; i < 5; i++)
            SpawnSupport(faust, i < 3 ? ModContent.NPCType<ReunionCrusher>() : ModContent.NPCType<ReunionCaster>(),
                mephisto.NPC.Center + new Vector2((i - 2) * 76, 0));
    }
    internal static void SpawnSupport(Faust f, int type, Vector2 position)
    {
        if (!Authority) return;
        Vector2 point = GroundNear(position, 38, 52);
        int i = NPC.NewNPC(f.NPC.GetSource_FromAI(), (int)point.X, (int)point.Y, type,
            ai0: f.NPC.whoAmI + 1, ai1: f.NPC.ai[3]);
        if (i < Main.maxNPCs) { Main.npc[i].Center = point; Main.npc[i].netUpdate = true; }
    }
    internal static int Turrets(Faust f)
    {
        int count = 0;
        foreach (NPC npc in Main.ActiveNPCs)
            if (npc.ModNPC is ReunionBallista && npc.ai[0] == f.NPC.whoAmI + 1 && npc.ai[1] == f.NPC.ai[3]) count++;
        return count;
    }
    internal static bool HealTarget(NPC n, NPC healer) => n.active && !n.friendly && !n.dontTakeDamage &&
        n.life > 0 && n.life < n.lifeMax && n.lifeMax > 5 && n.whoAmI != healer.whoAmI &&
        n.type != NPCID.TargetDummy && n.Distance(healer.Center) < 1500 &&
        (n.ModNPC is ReunionSupport support ? support.NPC.ai[0] == healer.ai[0] && support.NPC.ai[1] == healer.ai[3] : !n.boss);
    internal static void Walk(NPC npc, Vector2 destination, float speed)
    {
        float dx = destination.X - npc.Center.X;
        npc.direction = npc.spriteDirection = dx < 0 ? -1 : 1;
        npc.velocity.X = MathHelper.Lerp(npc.velocity.X, Math.Abs(dx) > 55 ? Math.Sign(dx) * speed : 0, .06f);
        if (npc.collideX && npc.velocity.Y == 0) npc.velocity.Y = -7;
        if (destination.Y < npc.Center.Y - 110 && npc.velocity.Y == 0) npc.velocity.Y = -8;
    }
    internal static void Remove(NPC npc)
    {
        if (!Authority) return;
        npc.active = false;
        if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
    }
    internal static void Cleanup(Faust f)
    {
        if (!Authority) return;
        foreach (NPC npc in Main.ActiveNPCs)
            if ((npc.ModNPC is ReunionSupport && npc.ai[0] == f.NPC.whoAmI + 1 && npc.ai[1] == f.NPC.ai[3]) ||
                (npc.ModNPC is Mephisto && npc.ai[0] == f.NPC.whoAmI + 1 && npc.ai[3] == f.NPC.ai[3])) Remove(npc);
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.ModProjectile is DuoProjectile && p.ai[0] == f.NPC.whoAmI + 1 && p.ai[1] == f.NPC.ai[3]) p.Kill();
    }
}
