using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ArknightsMod.Content.NPCs.Enemy.Isharmla;

public sealed class IsharmlaWorld : ModSystem
{
    public static bool Downed;
    public override void OnWorldLoad() => Downed = false;
    public override void OnWorldUnload() => Downed = false;
    public override void SaveWorldData(TagCompound tag) { if (Downed) tag["IsharmlaDowned"] = true; }
    public override void LoadWorldData(TagCompound tag) => Downed = tag.ContainsKey("IsharmlaDowned");
    public override void NetSend(BinaryWriter writer) => writer.Write(Downed);
    public override void NetReceive(BinaryReader reader) => Downed = reader.ReadBoolean();
    internal static void MarkDowned()
    { Downed = true; if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.WorldData); }
    internal static void Deactivate(NPC npc)
    {
        npc.active = false; npc.netUpdate = true;
        if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.SyncNPC, number: npc.whoAmI);
    }
    public override void PostSetupContent()
    {
        if (!ModLoader.TryGetMod("BossChecklist", out Mod checklist)) return;
        checklist.Call("LogBoss", Mod, "Isharmla", 19f, (Func<bool>)(() => Downed),
            new List<int> { ModContent.NPCType<Isharmla>(), ModContent.NPCType<IsharmlaDragon>() },
            new Dictionary<string, object> { ["spawnItems"] = new List<int> { ModContent.ItemType<CallOfTheTides>() },
                ["spawnInfo"] = Language.GetText("Mods.ArknightsMod.IsharmlaText.SpawnInfo") });
    }
    internal static Isharmla ActiveBoss()
    {
        int i = NPC.FindFirstNPC(ModContent.NPCType<Isharmla>());
        return i >= 0 ? Main.npc[i].ModNPC as Isharmla : null;
    }
}

public sealed class IsharmlaPlayer : ModPlayer
{
    public bool HeartEquipped;
    public int CalmTicks;
    public float Shake;
    public bool Membrane => HeartEquipped && CalmTicks >= 600;
    public override void ResetEffects() => HeartEquipped = false;
    public override void UpdateDead() { CalmTicks = 0; Shake = 0; }
    public override void PostUpdateEquips()
    {
        RestrictFlight();
        if (!HeartEquipped) { CalmTicks = 0; return; }
        Player.statLifeMax2 += 60; Player.GetDamage(DamageClass.Generic) += .06f;
        CalmTicks = Math.Min(600, CalmTicks + 1);
        if (!Main.dedServ && Membrane && Main.GameUpdateCount % 6 == 0)
            IsharmlaVisuals.Mote(Player.Center + Main.rand.NextVector2CircularEdge(26, 38), new Vector2(0, -.4f), .65f);
    }
    public override void ModifyHurt(ref Player.HurtModifiers modifiers)
    { if (Membrane) modifiers.FinalDamage *= .8f; }
    public override void PostHurt(Player.HurtInfo info)
    { if (HeartEquipped) { if (Membrane && !Main.dedServ) IsharmlaVisuals.Burst(Player.Center, 14); CalmTicks = 0; } }
    public override void PreUpdateMovement()
        => RestrictFlight();
    private void RestrictFlight()
    {
        if (IsharmlaWorld.ActiveBoss() is not { Stage: TideStage.Cantor, Initialized: true } boss) return;
        if (Vector2.DistanceSquared(Player.Center, boss.Anchor) > 4000 * 4000) return;
        SuppressFlight(Player);
    }
    public static void SuppressFlight(Player player)
    {
        // Do not touch jump state, jump boosts, gravity, grappling or player position.
        player.wingTime = 0; player.wingTimeMax = 0; player.wingsLogic = 0;
        player.rocketTime = 0; player.rocketTimeMax = 0; player.rocketBoots = 0;
        player.empressBrooch = false; player.carpet = false;
        if (player.mount.Active && (player.mount.CanFly() || player.mount.CanHover())) player.mount.Dismount(player);
    }
    public override void ModifyScreenPosition()
    {
        if (Shake <= .01f) return;
        Main.screenPosition += Main.rand.NextVector2Circular(Shake, Shake); Shake *= .85f;
    }
}

public sealed class TidalMembraneLayer : PlayerDrawLayer
{
    public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.BackAcc);
    public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => !Main.dedServ && drawInfo.drawPlayer.GetModPlayer<IsharmlaPlayer>().Membrane;
    protected override void Draw(ref PlayerDrawSet drawInfo)
    {
        var player = drawInfo.drawPlayer;
        if (player.dead || drawInfo.shadow != 0) return;
        drawInfo.DrawDataCache.Add(new DrawData(IsharmlaVisuals.RingTexture, player.Center - Main.screenPosition,
            null, IsharmlaVisuals.Cyan * .35f, 0, new Vector2(80), new Vector2(.40f, .58f), SpriteEffects.None, 0));
    }
}

public sealed class TideWeaponLayer : PlayerDrawLayer
{
    public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.HeldItem);
    protected override void Draw(ref PlayerDrawSet drawInfo)
    {
        Player p = drawInfo.drawPlayer;
        if (p.dead || drawInfo.shadow != 0 || p.itemAnimation <= 0 || p.HeldItem.ModItem is not (HeavensDeluge or FinalTideFlute or TidebornStaff)) return;
        bool cannon = p.HeldItem.ModItem is HeavensDeluge, staff = p.HeldItem.ModItem is TidebornStaff;
        Texture2D art = IsharmlaVisuals.Art(cannon ? "Head" : staff ? "Human" : "Fin");
        Rectangle? crop = staff ? new Rectangle(32, 4, 20, 48) : null;
        Vector2 origin = cannon ? new Vector2(190, 220) : staff ? new Vector2(10, 35) : new Vector2(80, 60);
        float rotation = p.itemRotation + (p.direction == -1 ? MathHelper.Pi : 0) - (cannon ? IsharmlaRules.HeadArtAngle : staff ? .8f : 0);
        drawInfo.DrawDataCache.Add(new DrawData(art, p.MountedCenter - Main.screenPosition + new Vector2(p.direction * 15, 0), crop,
            Lighting.GetColor((int)p.Center.X / 16, (int)p.Center.Y / 16), rotation, origin, cannon ? .17f : staff ? 1 : .22f,
            p.direction < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None, 0));
    }
}
