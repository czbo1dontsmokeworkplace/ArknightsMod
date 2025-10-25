using ArknightsMod.Content.Items.Armor.Vanity.Defender.Beagle;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Defender.Beagle
{
    public class BeagleSetPlayer : ModPlayer
    {
        public bool BeagleSetActive;

        public override void ResetEffects()
        {
			BeagleSetActive = false;
        }
        
        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            if (BeagleSetActive)
            {
				modifiers.FinalDamage *= 0.9f;
			}
        }
    }
}
