using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.UI;
using ArknightsMod.Content.Tiles;

namespace ArknightsMod.Content.Systems
{
	[Autoload(Side = ModSide.Client)]
	public class ElevatorUISystem : ModSystem
	{
		private UserInterface _ui;
		internal ElevatorUIState _state;

		public override void PostSetupContent()
		{
			_ui = new UserInterface();
			_state = new ElevatorUIState();
			_state.Activate();
		}

		public override void UpdateUI(GameTime gameTime)
		{
			UpdateAutoFloorPanel();

			if (_ui?.CurrentState != null)
			{
				_ui.Update(gameTime);
				Player player = Main.LocalPlayer;
				if (player != null && (_state.IsSettingsVisible || _state.IsMouseOverFloorPanel || _state.IsMouseOverSettingsPanel))
					Main.LocalPlayer.mouseInterface = true;

				int selectedBeforeWheel = player?.selectedItem ?? 0;
				int wheelSteps = PlayerInput.ScrollWheelDelta / 120;
				if (wheelSteps != 0 && _state.IsMouseOverFloorPanel && !_state.IsSettingsVisible)
					_state.ScrollSelection(wheelSteps);

				if (player != null && wheelSteps != 0 && TEElevator.TryFindElevatorForPlayer(player, out _, out _, out _))
					player.selectedItem = selectedBeforeWheel;

				bool confirmPressed = Main.keyState.IsKeyDown(Keys.F) && !Main.oldKeyState.IsKeyDown(Keys.F);
				if (confirmPressed)
					_state.ConfirmSelectedFloor();
			}
		}

		private void UpdateAutoFloorPanel()
		{
			if (_state == null || _ui == null || Main.gameMenu)
				return;

			Player player = Main.LocalPlayer;
			if (player == null || !player.active || player.dead)
			{
				if (_ui.CurrentState == _state && !_state.IsSettingsVisible)
					HideAll();
				return;
			}

			if (_state.IsSettingsVisible)
			{
				if (!_state.ShouldKeepSettingsOpen(player))
				{
					CloseElevatorUiCompletely();
					return;
				}

				EnsureShown();
			}

			if (TEElevator.TryFindElevatorForPlayer(player, out TEElevator te, out int topLeftX, out int topLeftY)
				&& TEElevator.IsPlayerInsideCabin(player, te))
			{
				EnsureShown();
				if (_state.TargetX != topLeftX || _state.TargetY != topLeftY || _ui.CurrentState != _state)
					_state.SetFloorTarget(topLeftX, topLeftY, resetSelection: false);
				return;
			}

			if (_ui.CurrentState == _state && !_state.IsSettingsVisible)
				HideAll();
		}

		private void CloseElevatorUiCompletely()
		{
			if (_state != null && _state.IsSettingsVisible)
				_state.HideSettings();
			if (_ui?.CurrentState == _state)
				HideAll();
		}

		private void EnsureShown()
		{
			if (_state == null || _ui == null)
				return;
			if (_ui.CurrentState != _state)
				_ui.SetState(_state);
		}

		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
		{
			int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
			if (mouseTextIndex == -1)
				return;

			layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
				"ArknightsMod: Elevator UI",
				delegate {
					if (_ui?.CurrentState != null)
						_ui.Draw(Main.spriteBatch, new GameTime());
					return true;
				},
				InterfaceScaleType.UI
			));
		}

		public void Show(int topLeftX, int topLeftY)
		{
			ShowSettingsWindow(topLeftX, topLeftY);
		}

		public void ShowSettingsWindow(int topLeftX, int topLeftY)
		{
			if (_state == null || _ui == null)
				return;
			EnsureShown();
			_state.ShowSettings(topLeftX, topLeftY);
		}

		public void HideAll()
		{
			_ui?.SetState(null);
		}

		public void HideSettingsWindow()
		{
			_state?.HideSettings();
		}
	}
}
