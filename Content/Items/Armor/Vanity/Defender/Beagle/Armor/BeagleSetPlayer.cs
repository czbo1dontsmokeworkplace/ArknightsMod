using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Defender.Beagle.Armor
{
    public class BeagleSetPlayer : ArknightsArmorPlayer
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
		public override void PostUpdateEquips() {
			if (BeagleSetActive) {
				Player.statDefense *= 1.5f;

			}
		}
	}
}
