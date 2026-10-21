using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.Isharmla;

public sealed class IsharmlaSeaborn : ModNPC
{
    public int OwnerIndex = -1, Encounter, Kind, Age;
    public Vector2 Origin, Launch;
    public static readonly Rectangle Sprite = new(332, 472, 355, 405);
    public override string Texture => Isharmla.Root + "SeabornAtlas";
    internal Isharmla Boss => OwnerIndex >= 0 && OwnerIndex < Main.maxNPCs && Main.npc[OwnerIndex].active &&
        Main.npc[OwnerIndex].ModNPC is Isharmla b && b.Encounter == Encounter ? b : null;
    internal void Bind(Isharmla b, int kind, Vector2 origin, Vector2 velocity)
    { OwnerIndex = b.NPC.whoAmI; Encounter = b.Encounter; Kind = kind; Origin = origin; Launch = velocity; }
    public override void SetStaticDefaults() => NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new NPCID.Sets.NPCBestiaryDrawModifiers { Hide = true });
    public override void SetDefaults()
    {
        NPC.width = 42; NPC.height = 54; NPC.lifeMax = 1900; NPC.defense = 35; NPC.damage = 100;
        NPC.aiStyle = -1; NPC.noGravity = true; NPC.noTileCollide = true; NPC.knockBackResist = .2f; NPC.npcSlots = 0;
        NPC.HitSound = SoundID.NPCHit13; NPC.DeathSound = SoundID.NPCDeath19; NPC.netAlways = true;
    }
    public override bool CheckActive() => false;
    public override bool CanHitPlayer(Player target, ref int cooldownSlot)
    { cooldownSlot = ImmunityCooldownID.Bosses; return Boss != null && Age > (Kind == 0 ? 120 : 65); }
    public override void AI()
    {
        var boss = Boss;
        if (boss == null || Age > 750)
        { if (Main.netMode != NetmodeID.MultiplayerClient) IsharmlaWorld.Deactivate(NPC); return; }
        Age++; NPC.timeLeft = 1800; Player p = Main.player[boss.NPC.target];
        if (!Main.dedServ && Age == 1 && Kind == 0) TidalParticles.Splash(Origin, false, .42f);
        if (Kind == 0 && Age <= 120)
        {
            float angle = Age * .13f * Math.Sign(Launch.X == 0 ? 1 : Launch.X);
            NPC.Center = Origin + new Vector2(MathF.Cos(angle) * 80, -Age * 3.4f + MathF.Sin(angle) * 35);
            NPC.velocity = Vector2.Zero;
        }
        else if (Kind == 1)
        {
            if (Age < 55) { NPC.velocity = new Vector2(Launch.X * .9f, Launch.Y + Age * .35f); }
            else
            {
                NPC.noTileCollide = false; NPC.noGravity = false;
                NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, Math.Sign(p.Center.X - NPC.Center.X) * 5, .04f);
                if (NPC.collideY && Age % 65 == 0) NPC.velocity.Y = -9;
            }
        }
        else
        {
            int cycle = (Age - 120) % 130;
            Vector2 destination = p.Center + new Vector2(Math.Sign(Origin.X - p.Center.X) * 240, -170);
            if (cycle < 65) NPC.velocity = Vector2.Lerp(NPC.velocity, (destination - NPC.Center).SafeNormalize(Vector2.UnitY) * 7, .05f);
            else if (cycle == 65) NPC.velocity = (p.Center - NPC.Center).SafeNormalize(Vector2.UnitY) * 13;
            else NPC.velocity *= .985f;
        }
        NPC.rotation = NPC.velocity.X * .015f;
        if (!Main.dedServ && Age % 6 == 0)
            TidalParticles.Emit(TideParticle.Bubble, NPC.Center, -NPC.velocity * .1f, new Vector2(4), IsharmlaVisuals.Cyan, 45);
    }
    public override void SendExtraAI(BinaryWriter w)
    { w.Write(OwnerIndex); w.Write(Encounter); w.Write(Kind); w.Write(Age); w.WriteVector2(Origin); w.WriteVector2(Launch); }
    public override void ReceiveExtraAI(BinaryReader r)
    { OwnerIndex = r.ReadInt32(); Encounter = r.ReadInt32(); Kind = r.ReadInt32(); Age = r.ReadInt32(); Origin = r.ReadVector2(); Launch = r.ReadVector2(); }
    public override bool PreDraw(SpriteBatch batch, Vector2 screenPos, Color drawColor)
    {
        batch.Draw(IsharmlaVisuals.Art("SeabornAtlas"), NPC.Center - screenPos, Sprite, Kind == 1 ? IsharmlaVisuals.Gold : Color.White,
            NPC.rotation, Sprite.Size() * .5f, Kind == 1 ? .14f : .18f, SpriteEffects.None, 0); return false;
    }
}
