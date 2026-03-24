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
				// 设置窗口打开，或鼠标悬停楼层按钮区时，占用鼠标交互，确保可点击楼层按钮。
				Player player = Main.LocalPlayer;
				if (player != null && (_state.IsSettingsVisible || _state.IsMouseOverFloorPanel))
					Main.LocalPlayer.mouseInterface = true;

				int selectedBeforeWheel = player?.selectedItem ?? 0;
				int wheelSteps = PlayerInput.ScrollWheelDelta / 120;
				if (wheelSteps != 0)
					_state.ScrollSelection(wheelSteps);

				// 玩家在电梯内时，禁用滚轮修改快捷物品栏（仅屏蔽滚轮，不影响数字键切换）。
				if (player != null && wheelSteps != 0 && 电梯TE.TryFindNearbyElevatorForPlayer(player, out _, out _, out _))
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

			if (电梯TE.TryFindNearbyElevatorForPlayer(player, out 电梯TE te, out int topLeftX, out int topLeftY))
			{
				// 仅当玩家“进入电梯轿厢”后才显示楼层按钮，而不是靠近就显示。
				if (IsPlayerInsideElevatorCabin(player, te))
				{
					EnsureShown();

					// 仅在目标电梯坐标变化时刷新按钮，避免每帧重建导致鼠标点击难以触发。
					if (_state.TargetX != topLeftX || _state.TargetY != topLeftY)
						_state.SetFloorTarget(topLeftX, topLeftY, resetSelection: false);
				}
				else if (_ui.CurrentState == _state && !_state.IsSettingsVisible)
				{
					HideAll();
				}
			}
			else if (_ui.CurrentState == _state && !_state.IsSettingsVisible)
			{
				HideAll();
			}
		}

		private static bool IsPlayerInsideElevatorCabin(Player player, 电梯TE te)
		{
			if (player == null || te == null)
				return false;

			// 电梯本体占 4x7 格，玩家 hitbox 与其相交才算“进入”。
			Rectangle elevatorRect = new Rectangle(te.Position.X * 16, te.Position.Y * 16, 4 * 16, 7 * 16);
			return player.Hitbox.Intersects(elevatorRect);
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
