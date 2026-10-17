using ArknightsMod.Content;
using ArknightsMod.Content.Items.Weapons.Vanguard;
using ArknightsMod.Content.Projectiles.Vanguard.Spears;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Players;

public sealed class VanguardSpearPlayer : ModPlayer
{
    public VanguardReserve Reserve { get; private set; } = new();
    private bool skillKeyDown, wasReady;
    internal int ReadyFlash;
    public override void Initialize() => Reserve = new();
    public override void UpdateDead()
    {
        Reserve.Clear(); ReadyFlash = 0;
        skillKeyDown = wasReady = false;
    }
    public override void PostUpdate()
    {
        if (Player.whoAmI != Main.myPlayer || Player.dead) return;
        if (ReadyFlash > 0) ReadyFlash--;
        if (Player.HeldItem.ModItem is not VanguardSpearWeapon weapon)
        { skillKeyDown = false; return; }
        Reserve.Tick(true);
        if (Reserve.Ready && !wasReady)
        {
            ReadyFlash = 24;
            SoundEngine.PlaySound(SoundID.MaxMana with { Volume = .32f, Pitch = .35f }, Player.Center);
            VanguardSpearVfx.Ready(Player, weapon.Kind);
        }
        wasReady = Reserve.Ready;
        bool pressed = ArknightsKeybinds.SkillActivatePressed(Player);
        if (pressed && !skillKeyDown && !Player.CCed && !Player.noItems)
        {
            WeaponPlayer skills = Player.GetModPlayer<WeaponPlayer>();
            if (skills.CurrentSkill?.AutoTrigger == false) Activate(skills, weapon.Kind);
        }
        skillKeyDown = pressed;
    }
    // Snapshot skill state at the start of an action. Changing the skill slot cannot mutate
    // already released spears or steal a charge for a volley which was refused.
    internal int AttackSkill(VanguardSpearKind kind, bool volley)
    {
        WeaponPlayer skills = Player.GetModPlayer<WeaponPlayer>();
        if (kind == VanguardSpearKind.Bagpipe && skills.Skill == 1)
        {
            if (volley || skills.StockCount <= 0) return 0;
            skills.DelStockCount(); // one stored S2 charge per melee action, never per enemy
            return 2;
        }
        if (kind == VanguardSpearKind.WildMane && skills.Skill == 0)
            Activate(skills, kind);
        return skills.SkillActive ? skills.Skill + 1 : 0;
    }
    private void Activate(WeaponPlayer skills, VanguardSpearKind kind)
    {
        if (skills.CurrentSkill == null || skills.SkillActive || skills.StockCount <= 0) return;
        skills.SkillActive = true; skills.SkillTimer = 0; skills.DelStockCount();
        SoundEngine.PlaySound(SoundID.Item4 with { Volume = .5f }, Player.Center);
        VanguardSpearVfx.Ready(Player, kind);
    }
    public override void PostUpdateEquips()
    {
        if (Player.HeldItem.ModItem is not VanguardSpearWeapon weapon) return;
        WeaponPlayer skills = Player.GetModPlayer<WeaponPlayer>();
        if (!skills.SkillActive) return;
        if (weapon.Kind == VanguardSpearKind.Grani)
        {
            Player.statDefense *= skills.Skill == 0 ? 2f : 1.8f;
            if (skills.Skill == 1) Player.noKnockback = true;
        }
        else if (weapon.Kind == VanguardSpearKind.Bagpipe && skills.Skill == 2)
            Player.statDefense *= 2.2f;
    }
    public override bool FreeDodge(Player.HurtInfo info)
    {
        if (Player.HeldItem.ModItem is not VanguardSpearWeapon { Kind: VanguardSpearKind.Grani }
            || !Main.rand.NextBool(5)) return false;
        Player.immune = true; Player.immuneTime = 30;
        VanguardSpearVfx.Ready(Player, VanguardSpearKind.Grani);
        return true;
    }
}
