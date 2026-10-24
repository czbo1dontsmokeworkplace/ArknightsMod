using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Friendly;

// Independent encounter enemy using the existing complete sprite sheet. No Evolution parent required.
public sealed class CannotOriginiutant : ModNPC
{
    public override string Texture => "ArknightsMod/Content/NPCs/Enemy/Evolution/Assets/Abomination";
    private int attackTimer;
    private int frame;
    public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 19;
    public override void SetDefaults() {
        NPC.width = 42;
        NPC.height = 58;
        NPC.lifeMax = 1800;
        NPC.damage = 45;
        NPC.defense = 20;
        NPC.knockBackResist = .15f;
        NPC.aiStyle = -1;
        NPC.HitSound = SoundID.NPCHit1;
        NPC.DeathSound = SoundID.NPCDeath1;
        NPC.value = 0;
    }
    public override void AI() {
        NPC.TargetClosest(true);
        Player target = Main.player[NPC.target];
        if (!target.active || target.dead) { NPC.velocity.X *= .9f; return; }
        NPC.direction = NPC.spriteDirection = target.Center.X >= NPC.Center.X ? 1 : -1;
        float distance = Vector2.Distance(NPC.Center, target.Center);
        if (attackTimer > 0) {
            NPC.velocity.X *= .65f;
            attackTimer++;
            frame = 8 + System.Math.Min(4, attackTimer / 6);
            if (attackTimer == 18 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center + new Vector2(NPC.direction * 26f, 0),
                    new Vector2(NPC.direction, 0), ModContent.ProjectileType<CannotOriginiutantSlash>(),
                    45, 2f, Main.myPlayer);
            if (attackTimer >= 42) attackTimer = 0;
        }
        else {
            NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, NPC.direction * 2.2f, .12f);
            if (NPC.collideX && NPC.collideY) NPC.velocity.Y = -7f;
            if (target.Center.Y < NPC.Center.Y - 60f && NPC.collideY && distance < 200f) NPC.velocity.Y = -8f;
            frame = (int)(NPC.ai[0]++ / 7) % 8;
            if (distance < 95f && Collision.CanHitLine(NPC.position, NPC.width, NPC.height, target.position, target.width, target.height))
                attackTimer = 1;
        }
    }
    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
        Texture2D texture = TextureAssets.Npc[Type].Value;
        int height = texture.Height / 19;
        Rectangle source = new(0, frame * height, texture.Width, height);
        spriteBatch.Draw(texture, NPC.Bottom - screenPos, source, NPC.GetAlpha(drawColor), 0f,
            new Vector2(texture.Width * .5f, height), .95f,
            NPC.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
        return false;
    }
}

public sealed class CannotOriginiutantSlash : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;
    public override void SetDefaults() {
        Projectile.width = 76;
        Projectile.height = 60;
        Projectile.hostile = true;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 6;
    }
    public override bool ShouldUpdatePosition() => false;
    public override void AI() {
        if (Main.dedServ) return;
        for (int i = 0; i < 3; i++) {
            var dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height,
                DustID.PurpleTorch, Projectile.velocity.X * 2f, 0f, 100, default, .9f);
            dust.noGravity = true;
        }
    }
    public override bool PreDraw(ref Color lightColor) => false;
}
