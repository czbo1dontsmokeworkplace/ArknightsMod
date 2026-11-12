using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ArknightsMod.Content.NPCs.Enemy.RoaringFlare.ImperialArtilleyCoreTargeteer;

// Native markers spawn their own corners, missile, region and ripple. Inherit ownership
// through the complete chain so transitions/death cannot leave an unowned attack behind.
public sealed class HarbingerOwnedProjectile : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    internal int Host = -1, Token, Age, SourceDamage;
    private bool confirmed;
    internal bool Owned => Token != 0;
    internal bool BelongsTo(AACT boss) => Owned && Host == boss.NPC.whoAmI && Token == boss.EncounterToken;
    internal bool Resolve(out AACT boss) {
        boss = null;
        if (Host < 0 || Host >= Main.maxNPCs || !Main.npc[Host].active || Main.npc[Host].ModNPC is not AACT found
            || !BelongsTo(found)) return false;
        boss = found; return true;
    }
    private static bool DeathVisual(Projectile p) => p.ModProjectile is AACTEndShow or AACTDeathdust or IACTScreenWave;
    public override void OnSpawn(Projectile p, IEntitySource source) {
        if (source is not EntitySource_Parent parent) return;
        if (parent.Entity is NPC npc && npc.ModNPC is AACT boss) {
            Host = npc.whoAmI; Token = boss.EncounterToken; SourceDamage = boss.Damage;
        }
        else if (parent.Entity is Projectile ancestor) {
            var owner = ancestor.GetGlobalProjectile<HarbingerOwnedProjectile>();
            Host = owner.Host; Token = owner.Token; SourceDamage = owner.SourceDamage;
        }
        if (!Owned) return;
        p.netImportant = true;
        if (p.hostile && p.ModProjectile is not HarbingerHazard) p.damage = SourceDamage;
        if (p.ModProjectile is AACTEndShow) p.timeLeft = HarbingerRules.DeathTicks;
        // A mirrored decorative battery only lives for its own spell.
        if (p.ModProjectile is ISTG3InCore or ISTG3InRing or ISTG3OutRing or ISTG3OutPrism) p.timeLeft = 300;
        int count = 0;
        foreach (Projectile other in Main.ActiveProjectiles)
            if (other.GetGlobalProjectile<HarbingerOwnedProjectile>().Token == Token) count++;
        if (count > HarbingerRules.HazardCap) { p.damage = 0; p.hostile = false; p.active = false; }
    }
    public override bool PreAI(Projectile p) {
        if (!Owned) return true;
        Age++;
        bool valid = Resolve(out AACT boss) && (boss.Phase < 4 || DeathVisual(p));
        if (Main.netMode == NetmodeID.MultiplayerClient && !confirmed && !valid && Age < 90) return false;
        if (!valid || Age > (DeathVisual(p) ? 200 : 600)) {
            p.damage = 0; p.hostile = false; p.active = false; return false;
        }
        confirmed = true;
        if (!Main.dedServ && Age == 1 && p.ModProjectile is ExplodeAim or ExplodeAimPro or HitboxblueCore or CollapsedHitboxblueCore) {
            string alert = p.ModProjectile is HitboxblueCore or CollapsedHitboxblueCore ? "AlertPro" : "Alert";
            SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Assets/Sound/ImperialArtilleyCoreTargeteer/" + alert) with { Volume = .6f }, p.Center);
        }
        if (Main.dedServ && p.ModProjectile is IACTScreenWave or ExplodeWave) return false;
        return true;
    }
    public override bool CanHitPlayer(Projectile p, Player target) => !Owned || Resolve(out AACT boss) && boss.Phase < 4 && boss.NPC.ai[3] <= 0;
    internal void Collapse(Projectile source, float[,] corners) {
        if (Main.netMode == NetmodeID.MultiplayerClient || !Resolve(out AACT boss)) return;
        int slot = Projectile.NewProjectile(source.GetSource_FromThis(), source.Center, Vector2.Zero,
            ModContent.ProjectileType<HarbingerCollapseArea>(), SourceDamage, 0, Main.myPlayer);
        if (slot >= Main.maxProjectiles) return;
        var area = (HarbingerCollapseArea)Main.projectile[slot].ModProjectile;
        for (int i = 0; i < 4; i++) area.Corners[i] = new Vector2(corners[i, 0], corners[i, 1]);
        Main.projectile[slot].netUpdate = true;
    }
    public override void SendExtraAI(Projectile p, BitWriter bits, BinaryWriter writer) {
        writer.Write(Host); writer.Write(Token); writer.Write(Age); writer.Write(SourceDamage);
    }
    public override void ReceiveExtraAI(Projectile p, BitReader bits, BinaryReader reader) {
        Host = reader.ReadInt32(); Token = reader.ReadInt32(); Age = reader.ReadInt32(); SourceDamage = reader.ReadInt32();
    }
}

public sealed class HarbingerCollapseArea : ModProjectile
{
    public override string Texture => global::ArknightsMod.ArknightsMod.noTexture;
    internal readonly Vector2[] Corners = new Vector2[4];
    public override void SetDefaults() {
        Projectile.width = Projectile.height = 2; Projectile.timeLeft = 3;
        Projectile.hostile = true; Projectile.friendly = false; Projectile.hide = true;
        Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.penetrate = -1;
        CooldownSlot = ImmunityCooldownID.Bosses;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? Colliding(Rectangle hitbox, Rectangle target) {
        // The original shaders fill x=[left,right], y=[top,bottom] and cut four holes.
        // Inset damage from that visible area; retain ordinary defense and immunity frames.
        int left = (int)MathF.Ceiling(Corners[2].X + 16), top = (int)MathF.Ceiling(Corners[3].Y + 16);
        int right = (int)MathF.Floor(Corners[0].X - 16), bottom = (int)MathF.Floor(Corners[1].Y - 16);
        if (right <= left || bottom <= top || !target.Intersects(new Rectangle(left, top, right - left, bottom - top))) return false;
        foreach (Vector2 corner in Corners) if (Vector2.DistanceSquared(corner, target.ClosestPointInRect(corner)) < 36 * 36) return false;
        return true;
    }
    public override void SendExtraAI(BinaryWriter writer) { foreach (Vector2 corner in Corners) { writer.Write(corner.X); writer.Write(corner.Y); } }
    public override void ReceiveExtraAI(BinaryReader reader) { for (int i = 0; i < 4; i++) Corners[i] = new(reader.ReadSingle(), reader.ReadSingle()); }
    public override bool PreDraw(ref Color lightColor) => false;
}
