using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Content.Items.Accessories.Rogue.Rarity_l3
{
    public class OldSteamArmor : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
			Item.value = Item.sellPrice(0, 6, 0, 0);
			Item.rare = ItemRarityID.Purple; // 青柠色(专家模式)稀有度
            Item.accessory = true;
            Item.defense = 10;
        }
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<OldSteamArmorPlayer>().active = true;
        }
    }

    public class OldSteamArmorPlayer : ModPlayer
    {
        public const float BurnRadius = 400f;
        public bool active;
        private int burnTimer;
        private double regenRemainder;

        public override void ResetEffects() => active = false;

        public override void UpdateDead()
        {
            burnTimer = 0;
            regenRemainder = 0;
        }

        public override void UpdateBadLifeRegen()
        {
            if (!active || Player.dead) return;
            if (Player.lifeRegen > 0) Player.lifeRegen = 0;
            Player.lifeRegenTime = 0;
            // Carry fractional regeneration units so odd life totals still lose exactly 1% per second.
            regenRemainder += Player.statLifeMax2 * 0.02;
            int regenLoss = (int)regenRemainder;
            regenRemainder -= regenLoss;
            Player.lifeRegen -= regenLoss;
        }

        public override void PostUpdate()
        {
            if (!active || Player.dead)
            {
                burnTimer = 0;
                regenRemainder = 0;
                return;
            }

            if (++burnTimer < 60) return;
            burnTimer = 0;

            // One authoritative strike per second; do not multiply by weapon bonuses.
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            float lifeLossPerSecond = Player.statLifeMax2 * 0.01f;
            float multiplier = Player.statLife < Player.statLifeMax2 * 0.5f ? 50f : 25f;
            int damage = Math.Max(1, (int)(lifeLossPerSecond * multiplier));

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy() || npc.Distance(Player.Center) > BurnRadius) continue;
                NPC.HitInfo hit = new NPC.HitInfo
                {
                    Damage = damage,
                    Knockback = 0f,
                    HitDirection = 0,
                    DamageType = DamageClass.Generic
                };
                npc.PlayerInteraction(Player.whoAmI);
                npc.StrikeNPC(hit);
                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendStrikeNPC(npc, hit);
                else
                    SpawnBurnDust(npc);
            }
        }

        private static void SpawnBurnDust(NPC npc)
        {
            for (int i = 0; i < 6; i++)
            {
                Dust dust = Dust.NewDustDirect(npc.position, npc.width, npc.height,
                    DustID.Torch, 0f, -1f, 100, default, 1.4f);
                dust.noGravity = true;
            }
        }
    }
}
