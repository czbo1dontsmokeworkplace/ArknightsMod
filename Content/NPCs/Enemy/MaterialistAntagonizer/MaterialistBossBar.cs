using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

public sealed class MaterialistBossBar : ModBossBar
{
    public override Asset<Texture2D> GetIconTexture(ref Rectangle? iconFrame) =>
        ModContent.Request<Texture2D>(MaterialistAntagonizer.AssetRoot + "MaterialistAntagonizer");
    public override bool? ModifyInfo(ref BigProgressBarInfo info, ref float life, ref float lifeMax, ref float shield, ref float shieldMax)
    {
        if (info.npcIndexToAimAt < 0 || info.npcIndexToAimAt >= Main.maxNPCs) return false;
        NPC npc = Main.npc[info.npcIndexToAimAt];
        if (!npc.active || npc.ModNPC is not MaterialistAntagonizer boss) return false;
        life = npc.life; lifeMax = npc.lifeMax;
        shield = boss.Shield; shieldMax = boss.ShieldMax;
        return true;
    }
    public override bool PreDraw(SpriteBatch spriteBatch, NPC npc, ref BossBarDrawParams drawParams)
    {
        drawParams.IconScale = 42f / drawParams.IconTexture.Width;
        return true;
    }
}
