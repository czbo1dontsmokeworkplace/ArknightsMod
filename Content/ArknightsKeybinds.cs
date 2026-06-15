using Terraria.ModLoader;

namespace ArknightsMod.Content
{
	public class ArknightsKeybinds : ModSystem
	{
		public static ModKeybind RosmontisTacticalDeploy { get; private set; }

		public override void Load() {
			RosmontisTacticalDeploy = KeybindLoader.RegisterKeybind(Mod, "Rosmontis Tactical Deploy", "Z");
		}
	}
}
