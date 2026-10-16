using System;
using ArknightsMod.Content;
using ArknightsMod.Content.Items.Weapons.Charged;
using ArknightsMod.Content.Projectiles.Charged;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Players;

public sealed class ChargedOperatorPlayer : ModPlayer
{
    internal int Recovery;
    internal float AimCharge;
    private bool keyDown;
    private int shields;
    internal int UnhurtTicks;
    public override void ResetEffects() => AimCharge = 0f;
    public override void UpdateDead() { Recovery = shields = UnhurtTicks = 0; keyDown = false; }
    public override void PostUpdate()
    {
        if (Recovery > 0) Recovery--;
        UnhurtTicks = Math.Min(600, UnhurtTicks + 1);
        if (Player.HeldItem.ModItem is not ChargedOperatorWeapon weapon)
        { shields = 0; keyDown = false; return; }
        WeaponPlayer skills = Player.GetModPlayer<WeaponPlayer>();
        if (!(weapon.Kind == ChargedOperatorKind.NearlRadiant && skills.SkillActive && skills.Skill == 1)) shields = 0;
        if (Player.whoAmI != Main.myPlayer || Player.dead || Player.noItems || Player.CCed) return;
        bool pressed = ArknightsKeybinds.SkillActivatePressed(Player);
        if (pressed && !keyDown && !skills.SkillActive && skills.StockCount > 0 && skills.CurrentSkill != null)
        {
            skills.SkillActive = true; skills.SkillTimer = 0; skills.DelStockCount();
            int skill = skills.Skill + 1;
            if (!Main.dedServ) SoundEngine.PlaySound(SoundID.Item4 with { Volume = .55f }, Player.Center);
            ChargedOperatorVfx.Burst(Player.Center, weapon.Kind, 12, 36f);
            if (weapon.Kind == ChargedOperatorKind.Matoimaru && skill == 1)
            { Player.Heal((int)(Player.statLifeMax2 * .5f)); skills.SkillActive = false; }
            if (weapon.Kind == ChargedOperatorKind.NearlRadiant && skill == 2) shields = 4;
            if (weapon.Kind == ChargedOperatorKind.NearlRadiant && skill == 3)
                SpawnField(weapon, ClampTarget(480f), 3);
            if (weapon.Kind == ChargedOperatorKind.Firewatch && skill == 2)
            {
                Vector2 center = ClampTarget(1100f);
                for (int i = -1; i <= 1; i++) SpawnField(weapon, center + new Vector2(i * 110, -Math.Abs(i) * 30), 2);
            }
        }
        keyDown = pressed;
    }
    private Vector2 ClampTarget(float range)
    {
        Vector2 offset = Main.MouseWorld - Player.Center;
        if (offset.LengthSquared() > range * range) offset = offset.SafeNormalize(Vector2.UnitX) * range;
        Vector2 point = Player.Center + offset;
        return Vector2.Clamp(point, new Vector2(32), new Vector2(Main.maxTilesX * 16 - 32, Main.maxTilesY * 16 - 32));
    }
    private void SpawnField(ChargedOperatorWeapon weapon, Vector2 center, int mode)
    {
        int damage = Player.GetWeaponDamage(weapon.Item);
        Projectile.NewProjectile(Player.GetSource_ItemUse(weapon.Item), center, Vector2.Zero,
            ModContent.ProjectileType<ChargedOperatorField>(), damage * (mode == 2 ? 4 : 1), 5, Player.whoAmI,
            (int)weapon.Kind, mode, mode == 3 ? Player.GetModPlayer<WeaponPlayer>().CurrentSkill.CurrentLevelData.ActiveTime * 60 : 90);
    }
    public override void PostUpdateRunSpeeds()
    {
        // Read the live charge before player movement, not a stale last-frame projectile request.
        if (Player.HeldItem.ModItem is not ChargedOperatorWeapon weapon || weapon.Melee) return;
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == Player.whoAmI && p.ModProjectile is ChargedOperatorHoldout hold && p.ai[2] == 0)
            { AimCharge = hold.Completion; break; }
        float factor = ChargedOperatorRules.MovementMultiplier(AimCharge);
        Player.maxRunSpeed *= factor; Player.accRunSpeed *= factor; Player.runAcceleration *= factor;
        Player.velocity.X *= MathHelper.Lerp(1f, .78f, AimCharge);
    }
    public override void PostUpdateEquips()
    {
        if (Player.HeldItem.ModItem is not ChargedOperatorWeapon weapon) return;
        WeaponPlayer skills = Player.GetModPlayer<WeaponPlayer>();
        if (!skills.SkillActive) return;
        int skill = skills.Skill + 1;
        if ((weapon.Kind == ChargedOperatorKind.Matoimaru || weapon.Kind == ChargedOperatorKind.Franka) && skill == 2)
            Player.statDefense *= 0f;
        if (weapon.Kind == ChargedOperatorKind.NearlRadiant && skill == 3) Player.statDefense += 20;
        if (weapon.Kind == ChargedOperatorKind.Firewatch && skill == 1) { Player.aggro -= 1200; Player.invis = true; }
    }
    public override bool FreeDodge(Player.HurtInfo info)
    {
        if (shields <= 0 || Player.HeldItem.ModItem is not ChargedOperatorWeapon weapon
            || weapon.Kind != ChargedOperatorKind.NearlRadiant || ActiveSkill(Player) != 2) return false;
        shields--;
        Player.immune = true; Player.immuneTime = 30;
        ChargedOperatorVfx.Burst(Player.Center, weapon.Kind, 10, 48);
        return true;
    }
    public override void OnHurt(Player.HurtInfo info) => UnhurtTicks = 0;
    internal static int ActiveSkill(Player player) => player.GetModPlayer<WeaponPlayer>().SkillActive
        ? player.GetModPlayer<WeaponPlayer>().Skill + 1 : 0;
    internal static float DamageMultiplier(ChargedOperatorKind kind, int skill) => (kind, skill) switch {
        (ChargedOperatorKind.Melantha, 1) => 1.5f,
        (ChargedOperatorKind.Matoimaru, 2) => 2.5f,
        (ChargedOperatorKind.Franka, 1) => 1.45f, (ChargedOperatorKind.Franka, 2) => 2f,
        (ChargedOperatorKind.NearlRadiant, 1) => 1.7f, (ChargedOperatorKind.NearlRadiant, 2) => 2.1f,
        (ChargedOperatorKind.NearlRadiant, 3) => 2.4f,
        (ChargedOperatorKind.Ambriel, 2) => 1.8f,
        (ChargedOperatorKind.Andreana, 1) => 2f, (ChargedOperatorKind.Andreana, 2) => 3.4f,
        (ChargedOperatorKind.Firewatch, 1) => 2.4f,
        (ChargedOperatorKind.Fartooth, 1) => 1.45f, (ChargedOperatorKind.Fartooth, 3) => 2.4f,
        _ => 1f
    };
}
