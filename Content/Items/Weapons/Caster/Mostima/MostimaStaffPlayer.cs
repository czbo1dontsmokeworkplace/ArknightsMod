using ArknightsMod.Content.Projectiles.Caster.Mostima;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Caster.Mostima;

public sealed class MostimaStaffPlayer : ModPlayer
{
    private bool keyConsumed;
    internal void TryActivate()
    {
        if (Player.whoAmI != Main.myPlayer || keyConsumed || Player.HeldItem.ModItem is not MostimaStaff
            || Player.dead || Player.CCed || Player.noItems) return;
        keyConsumed = true;
        WeaponPlayer s = Player.GetModPlayer<WeaponPlayer>();
        if (s.CurrentSkill?.Key.Item != nameof(MostimaStaff) || s.Skill is < 0 or > 2
            || s.SkillActive || s.StockCount <= 0) return;
        s.DelStockCount();
        s.SkillActive = true;
        s.SkillTimer = 0;
        if (s.Skill == 1)
        {
            float rank = MostimaStaff.Rank(Player);
            int index = Projectile.NewProjectile(Player.GetSource_ItemUse(Player.HeldItem),
                MostimaStaff.Target(Player), Vector2.Zero, ModContent.ProjectileType<MostimaTimeLock>(),
                (int)(Player.GetWeaponDamage(Player.HeldItem) * MathHelper.Lerp(.8f, 1.4f, rank)),
                0f, Player.whoAmI, (int)(MathHelper.Lerp(4f, 7f, rank) * 60));
            if (Main.projectile.IndexInRange(index)) Main.projectile[index].CritChance = Player.GetWeaponCrit(Player.HeldItem);
        }
        SoundEngine.PlaySound(SoundID.Item29 with { Volume = .6f, Pitch = -.35f }, Player.Center);
    }
    public override void PostUpdate()
    {
        if (!ArknightsKeybinds.SkillActivatePressed(Player)) keyConsumed = false;
        else TryActivate();
    }
}
