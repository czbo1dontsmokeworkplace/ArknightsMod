using ArknightsMod.Content.Items.Weapons.Guard.Crusher;
using ArknightsMod.Content.Projectiles.Charged;

namespace ArknightsMod.Content.Projectiles.Guard.Crusher;

// Screen coordinates: positive rotation is clockwise. Downcuts turn left CCW / right CW;
// an uppercut reverses that direction. Gravity mirroring is supplied by the caller's facing.
internal static class CrusherSwingMotion
{
    internal static int RotationDirection(CrusherKind kind, int facing) => kind == CrusherKind.Ulpianus ? -facing : facing;
    internal static float ReleaseDuration(CrusherKind kind, float attackSpeed) => BladeSwingTemplate.Duration(attackSpeed);
    // An uppercut mirrors the complete downcut, including its wind-up, angular
    // excursion and recovery. Direction alone must never shorten the arc or timing.
    internal static float Angle(CrusherKind kind, float aim, int facing, float progress) =>
        BladeSwingTemplate.Angle(aim, RotationDirection(kind, facing), progress);
}
