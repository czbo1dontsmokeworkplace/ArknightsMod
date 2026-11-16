using System;
using System.IO;
using ArknightsMod.Common.Networking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.Chapter6.FrostNova;

public partial class FrostNova
{
    // A standing center, not NPC.NewNPC's bottom-center argument. Reject floors
    // without headroom and inspect the entire body, including wide phase-two shields.
    internal static bool TryStandingCenter(Vector2 seed, int width, int height, out Vector2 center)
    {
        center = seed;
        float best = float.MaxValue;
        int tileX = Math.Clamp((int)(seed.X / 16), 12, Main.maxTilesX - 13);
        int tileY = Math.Clamp((int)(seed.Y / 16), 12, Main.maxTilesY - 13);
        for (int radius = 0; radius <= 12; radius++)
            for (int side = -1; side <= 1; side += 2) {
                if (radius == 0 && side == 1) continue;
                int x = Math.Clamp(tileX + radius * side, 12, Main.maxTilesX - 13);
                for (int y = Math.Max(12, tileY - 12); y < Math.Min(Main.maxTilesY - 12, tileY + 65); y++) {
                    if (!FrostNovaBlackIceRegion.IsSupportTile(x, y)) continue;
                    float floor = FrostNovaBlackIceRegion.SurfaceY(x, y, x * 16 + 8);
                    Vector2 candidate = new(x * 16 + 8, floor - height * .5f - 2);
                    if (Math.Abs(candidate.Y - seed.Y) > 640
                        || Collision.SolidCollision(candidate - new Vector2(width, height) * .5f, width, height + 1)) continue;
                    float score = Vector2.DistanceSquared(candidate, seed);
                    if (score < best) { center = candidate; best = score; }
                }
            }
        return best < float.MaxValue;
    }

    private int PendingSupport()
    {
        int count = SupportCount();
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.ModProjectile is SnowSupportPortal portal && !portal.Released
                && p.GetGlobalProjectile<FrostNovaOwnedProjectile>().BelongsTo(NPC.whoAmI, encounterToken)) count++;
        return count;
    }

    private void AnnounceSupport()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        string key = "Mods.ArknightsMod.FrostNovaText." + (AirbornePhase ? "PhaseTwoSupport" : "Support");
        Color color = new(180, 225, 255);
        if (Main.netMode == NetmodeID.Server) ChatHelper.BroadcastChatMessage(NetworkText.FromKey(key), color);
        else Main.NewText(Language.GetTextValue(key), color);
    }

    private void QuietForSupport()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        foreach (Projectile p in Main.ActiveProjectiles) {
            if (!p.GetGlobalProjectile<FrostNovaOwnedProjectile>().BelongsTo(NPC.whoAmI, encounterToken)
                || p.ModProjectile is SnowSupportPortal or FrostNovaBlackIceRegion or BlizzardStorm) continue;
            // Retire without a death callback, so mines cannot spawn a new fan.
            p.GetGlobalProjectile<FrostNovaOwnedProjectile>().Retire(p);
        }
    }

    private void OpenSupportPortals(Player player)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        int[] types = { ModContent.NPCType<IceCleaver>(), ModContent.NPCType<SnowCaster>(),
            ModContent.NPCType<SnowSoldier>(), ModContent.NPCType<SnowSniper>(),
            ModContent.NPCType<SnowHound>(), ModContent.NPCType<Oneiros>() };
        int count = AirbornePhase ? 3 : 2;
        for (int i = 0; i < count && PendingSupport() < FrostNovaCombatRules.SupportCap; i++) {
            int side = i % 2 == 0 ? -1 : 1;
            Vector2 seed = player.Center + new Vector2(side * (360 + i * 65), -40);
            // The cleaver is deliberately present in every second-phase wave.
            int kind = AirbornePhase && i == 0 ? 0 : (supportTimer++ % types.Length);
            if (!TryStandingCenter(seed, 64, 80, out Vector2 at)) continue;
            SpawnHazard(at, Vector2.Zero, ModContent.ProjectileType<SnowSupportPortal>(), 0,
                types[kind], 66 + i * 18, 0);
        }
        NPC.netUpdate = true;
    }
}

// The red portal retains the artist's original 21 frames and palette. It opens
// before releasing an enemy; its lower edge is the eventual standing surface.
public sealed class SnowSupportPortal : ModProjectile
{
    public override string Texture => FrostNovaVisuals.Assets + "SnowSupportPortal";
    private int age;
    public bool Released { get; private set; }
    private int ReleaseAt => Math.Clamp((int)Projectile.ai[1], 60, 120);
    public override void SetDefaults() {
        Projectile.width = 64; Projectile.height = 80; Projectile.timeLeft = 160;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.netImportant = true; Projectile.penetrate = -1;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override void AI() {
        age++;
        if (age == 1 && !Main.dedServ)
            SoundEngine.PlaySound(SoundID.Item8 with { Pitch = -.5f, Volume = .65f }, Projectile.Center);
        if (age >= ReleaseAt && !Released && Main.netMode != NetmodeID.MultiplayerClient) {
            Released = true;
            var owner = Projectile.GetGlobalProjectile<FrostNovaOwnedProjectile>();
            if (FrostNovaOwnedProjectile.Resolve(owner.Host, owner.Token, out FrostNova boss)
                && boss.CombatHazardsActive && CountSupport(owner.Host, owner.Token) < FrostNovaCombatRules.SupportCap
                && FrostNova.TryStandingCenter(Projectile.Center, 64, 80, out Vector2 at)) {
                int slot = EnemyAuthority.NewNPC(Projectile.GetSource_FromThis(), (int)at.X, (int)(at.Y + 40), (int)Projectile.ai[0]);
                if (Main.npc.IndexInRange(slot)) {
                    NPC npc = Main.npc[slot];
                    npc.Bottom = at + new Vector2(0, 40);
                    npc.velocity = Vector2.Zero; npc.TargetClosest(); npc.netUpdate = true;
                }
            }
            Projectile.netUpdate = true;
        }
        if (age >= ReleaseAt + 36) Projectile.Kill();
    }
    private static int CountSupport(int host, int token) {
        int count = 0;
        foreach (NPC npc in Main.ActiveNPCs)
            if (npc.GetGlobalNPC<FrostNovaOwnedNPC>().BelongsTo(host, token)) count++;
        return count;
    }
    public override void SendExtraAI(BinaryWriter writer) { writer.Write(age); writer.Write(Released); }
    public override void ReceiveExtraAI(BinaryReader reader) { age = reader.ReadInt32(); Released = reader.ReadBoolean(); }
    public override bool PreDraw(ref Color lightColor) {
        Texture2D texture = FrostNovaVisuals.Texture("SnowSupportPortal");
        Rectangle frame = texture.Frame(1, 21, 0, Math.Min(20, age * 21 / (ReleaseAt + 36)));
        float opacity = Math.Min(1, age / 18f) * Math.Clamp((ReleaseAt + 36 - age) / 24f, 0, 1)
            * Projectile.GetGlobalProjectile<FrostNovaOwnedProjectile>().Visibility;
        Main.EntitySpriteDraw(texture, Projectile.Bottom - Main.screenPosition + new Vector2(0, 2), frame,
            Color.White * opacity, 0, new Vector2(frame.Width * .5f, frame.Height),
            new Vector2(70f / frame.Width, 112f / frame.Height), SpriteEffects.None);
        return false;
    }
}
