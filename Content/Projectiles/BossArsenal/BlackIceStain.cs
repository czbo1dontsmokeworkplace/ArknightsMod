using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;

namespace ArknightsMod.Content.Projectiles.BossArsenal;

// A networked, temporary overlay; never writes paint, frames, or world tiles.
public sealed class BlackIceStain : ModProjectile
{
    internal const int SolidTicks = 180, FadeTicks = 90, TotalTicks = SolidTicks + FadeTicks;
    public override string Texture => "Terraria/Images/MagicPixel";
    public override void SetDefaults() {
        Projectile.width = Projectile.height = 2; Projectile.timeLeft = TotalTicks;
        Projectile.tileCollide = false; Projectile.netImportant = true; Projectile.hide = true;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override bool PreDraw(ref Color lightColor) => false;
    public override void SendExtraAI(BinaryWriter writer) => writer.Write(Projectile.timeLeft);
    public override void ReceiveExtraAI(BinaryReader reader) => Projectile.timeLeft = Math.Clamp(reader.ReadInt32(), 1, TotalTicks);
    internal static float Opacity(int remaining) => Math.Clamp(remaining / (float)FadeTicks, 0, 1);
    internal static void Spawn(Projectile source, Vector2 at) {
        Point tile = at.ToTileCoordinates();
        // Grounded enemies are struck above the ground: anchor at the closest surface
        // immediately beneath their feet, keeping the affected region exactly 3 by 3.
        for (int dy = 0; dy <= 3; dy++) {
            int y = tile.Y + dy;
            if (!WorldGen.InWorld(tile.X, y, 2)) return;
            if (Main.tile[tile.X, y].HasUnactuatedTile) { tile.Y = y; break; }
        }
        Projectile.NewProjectile(source.GetSource_FromThis(), tile.ToWorldCoordinates(), Vector2.Zero,
            ModContent.ProjectileType<BlackIceStain>(), 0, 0, source.owner, 0, tile.X, tile.Y);
    }
}

public sealed class BlackIceFrost : ModBuff
{
    public override string Texture => "Terraria/Images/Buff_" + BuffID.Frostburn;
    public override void SetStaticDefaults() { Main.debuff[Type] = true; Main.buffNoSave[Type] = true; }
    // The contact system owns the exact 2 HP/tick loss; this debuff adds no second DoT.
    public override void Update(Player player, ref int buffIndex) { player.moveSpeed -= .15f; }
}

public sealed class BlackIceStainSystem : ModSystem
{
    internal static readonly Dictionary<Point, float> Tiles = new();
    private readonly HashSet<int> damagedRoots = new();
    public override void OnWorldLoad() => Tiles.Clear();
    public override void OnWorldUnload() => Tiles.Clear();
    public override void Unload() => Tiles.Clear();
    public override void PostUpdateEverything() {
        Tiles.Clear();
        foreach (Projectile patch in Main.ActiveProjectiles) {
            if (patch.ModProjectile is not BlackIceStain) continue;
            for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++) {
                var point = new Point((int)patch.ai[1] + dx, (int)patch.ai[2] + dy);
                if (!WorldGen.InWorld(point.X, point.Y, 1) || !Main.tile[point.X, point.Y].HasUnactuatedTile) continue;
                float fade = BlackIceStain.Opacity(patch.timeLeft);
                Tiles[point] = Math.Max(Tiles.GetValueOrDefault(point), fade);
            }
        }
        if (Tiles.Count == 0) return;
        // NPCs are server-owned. Segment and overlapping-area hits share one root per tick.
        if (Main.netMode != NetmodeID.MultiplayerClient) {
            damagedRoots.Clear();
            foreach (NPC part in Main.ActiveNPCs) {
                if (!Touches(part.Hitbox)) continue;
                int rootIndex = part.realLife >= 0 ? part.realLife : part.whoAmI;
                if (!Main.npc.IndexInRange(rootIndex) || !damagedRoots.Add(rootIndex)) continue;
                NPC npc = Main.npc[rootIndex];
                if (!npc.active || npc.life <= 0) continue;
                npc.AddBuff(ModContent.BuffType<BlackIceFrost>(), 30);
                npc.life -= 2; // Raw life removal bypasses defense, DR and hit immunity.
                if (npc.life <= 0) { npc.life = 0; npc.checkDead(); }
                if (Main.netMode == NetmodeID.Server && (Main.GameUpdateCount % 15 == 0 || npc.life <= 0))
                    NetMessage.SendData(MessageID.SyncNPC, number: npc.whoAmI);
            }
        }
        // Vanilla player health is owner-authoritative; never also subtract on the server.
        if (Main.netMode != NetmodeID.Server) {
            Player player = Main.LocalPlayer;
            if (player.active && !player.dead && Touches(player.Hitbox)) {
                player.AddBuff(ModContent.BuffType<BlackIceFrost>(), 30);
                player.statLife -= 2;
                if (player.statLife <= 0) player.KillMe(PlayerDeathReason.ByCustomReason(player.name + "被黑霜吞没了。"), 2, 0);
                if (Main.netMode == NetmodeID.MultiplayerClient && (Main.GameUpdateCount % 15 == 0 || player.dead))
                    NetMessage.SendData(MessageID.PlayerLifeMana, number: player.whoAmI);
            }
        }
    }
    internal static bool Touches(Rectangle hitbox) {
        hitbox.Inflate(2, 2); // Includes feet exactly resting on a tile surface.
        int left = Math.Max(1, hitbox.Left / 16), right = Math.Min(Main.maxTilesX - 2, hitbox.Right / 16);
        int top = Math.Max(1, hitbox.Top / 16), bottom = Math.Min(Main.maxTilesY - 2, hitbox.Bottom / 16);
        // Iterate whichever side is smaller: a giant boss must not multiply damage or scan the world.
        if ((right - left + 1) * (bottom - top + 1) < Tiles.Count) {
            for (int x = left; x <= right; x++) for (int y = top; y <= bottom; y++)
                if (Tiles.ContainsKey(new Point(x, y))) return true;
        }
        else foreach (Point tile in Tiles.Keys)
            if (tile.X >= left && tile.X <= right && tile.Y >= top && tile.Y <= bottom) return true;
        return false;
    }
}

public sealed class BlackIceTileTint : GlobalTile
{
    public override void DrawEffects(int i, int j, int type, SpriteBatch spriteBatch, ref TileDrawInfo drawData) {
        if (BlackIceStainSystem.Tiles.TryGetValue(new Point(i, j), out float fade)) {
            drawData.tileLight = Color.Lerp(drawData.tileLight, new Color(3, 4, 8), fade);
            drawData.finalColor = Color.Lerp(drawData.finalColor, new Color(3, 4, 8), fade);
            drawData.colorTint = Color.Lerp(drawData.colorTint, new Color(3, 4, 8), fade);
            drawData.glowColor *= 1 - fade;
        }
    }
}

public sealed class BlackIceFrostVisuals : GlobalNPC
{
    public override void DrawEffects(NPC npc, ref Color drawColor) {
        if (npc.HasBuff<BlackIceFrost>()) drawColor = Color.Lerp(drawColor, new Color(130, 175, 205), .35f);
    }
}
