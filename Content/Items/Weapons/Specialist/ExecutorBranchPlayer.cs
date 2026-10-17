using System;
using ArknightsMod.Content.Items.Weapons.Specialist.Gravel;
using ArknightsMod.Content.Items.Weapons.Specialist.Red;
using ArknightsMod.Content.Items.Weapons.Specialist.Phantom;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Specialist;

public sealed class ExecutorBranchPlayer : ModPlayer
{
    private bool initialized, wasFast, wasDashing, respawned;
    private float falling;
    public override void OnRespawn() => respawned = true;
    public override void PreUpdateMovement() => falling = Math.Max(falling, Player.velocity.Y * Player.gravDir);
    public override void PostUpdate()
    {
        if (Player.whoAmI != Main.myPlayer || Player.dead) return;
        bool fast = Math.Abs(Player.velocity.X) >= 12f;
        bool dashing = Player.dashDelay < 0;
        bool landed = initialized && falling > .1f && Math.Abs(Player.velocity.Y) < .1f
            && Math.Abs(Collision.TileCollision(Player.position, new Vector2(0, 2 * Player.gravDir),
                Player.width, Player.height, false, false, (int)Player.gravDir).Y) < 1f;
        bool trigger = respawned || initialized && (fast && !wasFast || dashing && !wasDashing || landed);
        if (trigger)
        {
            if (Player.HeldItem.ModItem is GravelDualBlades gravel) gravel.TryActivateSkill(Player);
            else if (Player.HeldItem.ModItem is RedDagger) Player.GetModPlayer<RedDaggerPlayer>().DeployFromBranchEvent();
            else if (Player.HeldItem.ModItem is PhantomDaggers) Player.GetModPlayer<PhantomPlayer>().TryDeploy(false);
        }
        respawned = false;
        initialized = true;
        wasFast = fast;
        wasDashing = dashing;
        falling = Math.Max(0f, Player.velocity.Y * Player.gravDir);
    }
    public override void UpdateDead()
    {
        initialized = wasFast = wasDashing = respawned = false;
        falling = 0f;
    }
}
