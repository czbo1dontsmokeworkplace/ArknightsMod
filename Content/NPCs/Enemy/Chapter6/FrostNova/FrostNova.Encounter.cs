using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.GameContent;

namespace ArknightsMod.Content.NPCs.Enemy.Chapter6.FrostNova;

// Source inheritance gives every child the encounter identity, including old ice effects.
public sealed class FrostNovaOwnedProjectile : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    internal int Host = -1, Token, Age;
    private bool ownerConfirmed;
    private int pendingTicks;
    public bool BelongsTo(int host, int token) => Token != 0 && Host == host && Token == token;
    internal static bool Resolve(int host, int token, out FrostNova boss) {
        boss = null;
        if (!Main.npc.IndexInRange(host)) return false;
        NPC npc = Main.npc[host];
        if (!npc.active || npc.ModNPC is not FrostNova found || found.EncounterToken != token || token == 0) return false;
        boss = found; return true;
    }
    internal static void FromSource(IEntitySource source, out int host, out int token) {
        host = -1; token = 0;
        if (source is not EntitySource_Parent parent) return;
        if (parent.Entity is NPC n) {
            if (n.ModNPC is FrostNova boss) { host = n.whoAmI; token = boss.EncounterToken; }
            else { var owner = n.GetGlobalNPC<FrostNovaOwnedNPC>(); host = owner.Host; token = owner.Token; }
        }
        else if (parent.Entity is Projectile p) {
            var owner = p.GetGlobalProjectile<FrostNovaOwnedProjectile>(); host = owner.Host; token = owner.Token;
        }
    }
    public override void OnSpawn(Projectile projectile, IEntitySource source) {
        FromSource(source, out Host, out Token);
        if (Token == 0) return;
        if (projectile.hostile && projectile.velocity.LengthSquared() > 12 * 12)
            projectile.velocity = projectile.velocity.SafeNormalize(Vector2.UnitY) * 12;
        if (projectile.ModProjectile is FNIceRingRound or FNIceRingChoice) projectile.hostile = false;
        if (projectile.hostile) projectile.damage = Math.Min(projectile.damage, FrostNovaCombatRules.HeavyDamage);
        int count = 0;
        if (Main.netMode != NetmodeID.MultiplayerClient && projectile.hostile) {
            foreach (Projectile p in Main.ActiveProjectiles)
                if (p.hostile && p.GetGlobalProjectile<FrostNovaOwnedProjectile>().BelongsTo(Host, Token)) count++;
            if (count > FrostNovaCombatRules.HazardCap) { projectile.damage = 0; projectile.hostile = false; projectile.active = false; }
        }
        projectile.netImportant = true;
    }
    public override bool PreAI(Projectile projectile) {
        if (Token == 0) return true;
        if (projectile.ModProjectile is FrostNovaSmoke) {
            projectile.hostile = false; projectile.damage = 0;
            if (++Age <= 180) return true;
            projectile.active = false; return false;
        }
        bool alive = Resolve(Host, Token, out FrostNova boss);
        bool visual = projectile.ModProjectile is FrostNovaJump or FrostNovaRevival or FrostNovaWhiteRing or FrostNovaSmoke or FNInvincibleEffect;
        bool valid = alive && (boss.CombatHazardsActive || visual);
        if (Main.netMode == NetmodeID.MultiplayerClient && !ownerConfirmed && !valid && ++pendingTicks < 90) return false;
        if (valid) ownerConfirmed = true;
        Age++;
        if (!alive || (!boss.CombatHazardsActive && !visual) || Age > (projectile.ModProjectile is BlizzardStorm ? 720 : 1200)) {
            projectile.damage = 0; projectile.hostile = false;
            // No death callback: retirement must not spawn a fresh damaging child.
            projectile.active = false;
            return false;
        }
        if (projectile.ModProjectile is FNIceRingRound or FNIceRingChoice) projectile.hostile = false;
        if (projectile.ModProjectile is Blizzard) projectile.hostile = false;
        return true;
    }
    public override void PostAI(Projectile projectile) {
        if (Token == 0) return;
        float absoluteCap = projectile.ModProjectile is FrostNovaBlackIceSpike ? 12 : FrostNovaCombatRules.LegacyShardSpeed;
        if (projectile.hostile && projectile.velocity.LengthSquared() > absoluteCap * absoluteCap)
            projectile.velocity = projectile.velocity.SafeNormalize(Vector2.UnitY) * absoluteCap;
        if (projectile.ModProjectile is Blizzard) {
            projectile.hostile = false;
            if (Age > 180) projectile.active = false;
            float cap = 8.8f;
            if (projectile.velocity.LengthSquared() > cap * cap) projectile.velocity = projectile.velocity.SafeNormalize(Vector2.UnitY) * cap;
        }
        if (projectile.ModProjectile is BlizzardStorm && Age % 120 == 0 && Main.netMode != NetmodeID.MultiplayerClient
            && Resolve(Host, Token, out FrostNova boss) && boss.CombatHazardsActive) {
            int count = 0;
            foreach (Projectile p in Main.ActiveProjectiles) if (p.hostile && p.GetGlobalProjectile<FrostNovaOwnedProjectile>().BelongsTo(Host, Token)) count++;
            if (count <= FrostNovaCombatRules.HazardCap - 3) {
                Player target = Main.player[boss.NPC.target];
                Vector2 aim = (target.Center - projectile.Center).SafeNormalize(Vector2.UnitY);
                for (int i = -1; i <= 1; i++) Projectile.NewProjectile(projectile.GetSource_FromThis(), projectile.Center,
                    aim.RotatedBy(i * .22f) * 4.8f, ModContent.ProjectileType<FrostNovaBarrage>(), 21, 0, Main.myPlayer, Host, 0, 48);
            }
        }
        if (projectile.ModProjectile is BlackIceProjectile or BlackIce or BlackIceIntro) {
            float max = FrostNovaCombatRules.LegacyShardSpeed;
            if (projectile.velocity.LengthSquared() > max * max)
                projectile.velocity = projectile.velocity.SafeNormalize(Vector2.UnitY) * max;
        }
    }
    public override bool CanHitPlayer(Projectile projectile, Player target) =>
        Token == 0 || (Resolve(Host, Token, out FrostNova boss) && boss.CombatHazardsActive);
    public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter writer) {
        writer.Write(Host); writer.Write(Token); writer.Write(Age);
    }
    public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader reader) {
        Host = reader.ReadInt32(); Token = reader.ReadInt32(); Age = reader.ReadInt32();
    }
}

public sealed class FrostNovaOwnedNPC : GlobalNPC
{
    public override bool InstancePerEntity => true;
    internal int Host = -1, Token, Age;
    private bool ownerConfirmed;
    private int pendingTicks;
    public bool BelongsTo(int host, int token) => Token != 0 && Host == host && Token == token;
    public override void OnSpawn(NPC npc, IEntitySource source) {
        FrostNovaOwnedProjectile.FromSource(source, out Host, out Token);
        if (Token == 0) return;
        npc.damage = Math.Min(npc.damage, 24);
        if (npc.ModNPC is IceCrystalAltar) { npc.lifeMax = npc.life = 1600; npc.defense = 8; }
        npc.netUpdate = true;
    }
    public override bool PreAI(NPC npc) {
        if (Token == 0) return true;
        bool alive = FrostNovaOwnedProjectile.Resolve(Host, Token, out FrostNova boss);
        if (Main.netMode == NetmodeID.MultiplayerClient && !ownerConfirmed && (!alive || !boss.CombatHazardsActive) && ++pendingTicks < 90) return false;
        if (alive && boss.CombatHazardsActive) ownerConfirmed = true;
        Age++;
        if (!alive || !boss.CombatHazardsActive || Age > 1080) {
            npc.damage = 0; npc.active = false; npc.netUpdate = true; return false;
        }
        npc.damage = Math.Min(npc.damage, 24);
        return true;
    }
    public override void PostAI(NPC npc) {
        if (Token != 0) npc.damage = Math.Min(npc.damage, 24);
    }
    public override bool CanHitPlayer(NPC npc, Player target, ref int cooldownSlot) =>
        Token == 0 || (FrostNovaOwnedProjectile.Resolve(Host, Token, out FrostNova boss) && boss.CombatHazardsActive);
    public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter writer) {
        writer.Write(Host); writer.Write(Token); writer.Write(Age);
    }
    public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader reader) {
        Host = reader.ReadInt32(); Token = reader.ReadInt32(); Age = reader.ReadInt32();
    }
}

public sealed class FrostNovaChill : ModPlayer
{
    internal int FreezeImmunity;
    public override void PostUpdate() { if (FreezeImmunity > 0) FreezeImmunity--; }
    public override void UpdateDead() => FreezeImmunity = 0;
    public static void Apply(Player target, int chilledTicks = 120, int frozenTicks = 30) {
        var state = target.GetModPlayer<FrostNovaChill>();
        bool chilled = target.HasBuff(BuffID.Chilled);
        if (chilled && frozenTicks > 0 && state.FreezeImmunity == 0 && !target.buffImmune[BuffID.Frozen]) {
            target.AddBuff(BuffID.Frozen, Math.Min(30, frozenTicks));
            state.FreezeImmunity = FrostNovaCombatRules.FreezeCooldown;
        }
        if (!target.buffImmune[BuffID.Chilled]) target.AddBuff(BuffID.Chilled, chilledTicks);
    }
}

// Only the airborne phase owns the weather. The previous world's weather is restored.
public sealed class FrostNovaWeather : ModSystem
{
    private bool holding, wasRaining;
    private float savedRain, savedCloud, savedWind, savedCurrentWind;
    private double savedRainTime;
    private static bool StormWanted() {
        foreach (NPC npc in Main.ActiveNPCs)
            if (npc.ModNPC is FrostNova boss && boss.AirbornePhase && boss.CombatHazardsActive) return true;
        return false;
    }
    public override void PostUpdateWorld() {
        bool wanted = StormWanted();
        if (wanted) {
            if (!holding) {
                holding = true; wasRaining = Main.raining; savedRain = Main.maxRaining; savedCloud = Main.cloudAlpha;
                savedWind = Main.windSpeedTarget; savedCurrentWind = Main.windSpeedCurrent; savedRainTime = Main.rainTime;
            }
            bool changed = !Main.raining || Main.maxRaining < .9f;
            Main.raining = true; Main.rainTime = Math.Max(Main.rainTime, 3600);
            Main.maxRaining = .95f; Main.cloudAlpha = .95f;
            Main.windSpeedTarget = .85f; Main.windSpeedCurrent = .8f;
            if (changed && Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.WorldData);
        }
        else if (holding) Restore();
    }
    private void Restore() {
        Main.raining = wasRaining; Main.maxRaining = savedRain; Main.cloudAlpha = savedCloud;
        Main.windSpeedTarget = savedWind; Main.windSpeedCurrent = savedCurrentWind; Main.rainTime = savedRainTime;
        holding = false;
        if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.WorldData);
    }
    public override void OnWorldUnload() { if (holding) Restore(); }
    public override void Unload() { if (holding) Restore(); }
    public override void PostDrawTiles() {
        if (Main.dedServ || !StormWanted()) return;
        // World-independent snowfall: leaving the snow biome never turns this fight into rain.
        // This layer sits behind hostile projectiles and never hides their warnings.
        float time = (float)Main.GlobalTimeWrappedHourly;
        int count = Math.Clamp(Main.screenWidth * Main.screenHeight / 15000, 55, 170);
        Texture2D pixel = TextureAssets.MagicPixel.Value;
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);
        try {
        for (int i = 0; i < count; i++) {
            float layer = .65f + (i % 5) * .09f;
            float x = ((i * 317.3f + time * 170 * layer - Main.screenPosition.X * .16f) % (Main.screenWidth + 100) + Main.screenWidth + 100) % (Main.screenWidth + 100) - 50;
            float y = ((i * 193.7f + time * 115 * layer - Main.screenPosition.Y * .12f) % (Main.screenHeight + 80) + Main.screenHeight + 80) % (Main.screenHeight + 80) - 40;
            Main.spriteBatch.Draw(pixel, new Vector2(x, y), new Rectangle(0, 0, 1, 1),
                new Color(172, 211, 230) * (.16f + (i % 3) * .035f), -.82f, Vector2.Zero,
                new Vector2(1.1f, 5.5f) * layer, SpriteEffects.None, 0);
        }
        }
        finally { Main.spriteBatch.End(); }
    }
}
