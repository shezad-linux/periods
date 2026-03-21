using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Lumenfall.Services
{
    public enum VirtualActionButton
    {
        Jump = 0,
        Attack = 1,
        Dash = 2,
        Ability = 3,
        Pause = 4,
        Submit = 5,
        Cancel = 6
    }

    public readonly struct GameplayInputSnapshot
    {
        public readonly Vector2 Move;
        public readonly bool JumpPressed;
        public readonly bool JumpHeld;
        public readonly bool AttackPressed;
        public readonly bool DashPressed;
        public readonly bool AbilityPressed;
        public readonly bool PausePressed;

        public GameplayInputSnapshot(
            Vector2 move,
            bool jumpPressed,
            bool jumpHeld,
            bool attackPressed,
            bool dashPressed,
            bool abilityPressed,
            bool pausePressed)
        {
            Move = move;
            JumpPressed = jumpPressed;
            JumpHeld = jumpHeld;
            AttackPressed = attackPressed;
            DashPressed = dashPressed;
            AbilityPressed = abilityPressed;
            PausePressed = pausePressed;
        }
    }

    public readonly struct UiInputSnapshot
    {
        public readonly Vector2 Navigate;
        public readonly bool SubmitPressed;
        public readonly bool CancelPressed;
        public readonly bool PausePressed;

        public UiInputSnapshot(Vector2 navigate, bool submitPressed, bool cancelPressed, bool pausePressed)
        {
            Navigate = navigate;
            SubmitPressed = submitPressed;
            CancelPressed = cancelPressed;
            PausePressed = pausePressed;
        }
    }

    internal struct VirtualButtonState
    {
        public bool IsHeld;
        public bool PressedThisFrame;
    }

    public sealed class InputService : ServiceBehaviour
    {
        private InputActionAsset _asset;
        private InputActionMap _gameplayMap;
        private InputActionMap _uiMap;
        private InputActionMap _debugMap;
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _attackAction;
        private InputAction _dashAction;
        private InputAction _abilityAction;
        private InputAction _pauseAction;
        private InputAction _uiNavigateAction;
        private InputAction _uiSubmitAction;
        private InputAction _uiCancelAction;
        private readonly Dictionary<VirtualActionButton, VirtualButtonState> _virtualButtons = new();
        private Vector2 _virtualMove;

        protected override Type ServiceType => typeof(InputService);

        public GameplayInputSnapshot Gameplay { get; private set; }

        public UiInputSnapshot Ui { get; private set; }

        public bool GameplayEnabled => _gameplayMap.enabled;

        protected override void Awake()
        {
            base.Awake();
            BuildActions();
            EnableGameplay();
            EnableUi();
        }

        private void Update()
        {
            Gameplay = new GameplayInputSnapshot(
                GetCombinedMove(),
                WasPressed(_jumpAction, VirtualActionButton.Jump),
                IsHeld(_jumpAction, VirtualActionButton.Jump),
                WasPressed(_attackAction, VirtualActionButton.Attack),
                WasPressed(_dashAction, VirtualActionButton.Dash),
                WasPressed(_abilityAction, VirtualActionButton.Ability),
                WasPressed(_pauseAction, VirtualActionButton.Pause));

            Ui = new UiInputSnapshot(
                _uiNavigateAction.ReadValue<Vector2>(),
                WasPressed(_uiSubmitAction, VirtualActionButton.Submit),
                WasPressed(_uiCancelAction, VirtualActionButton.Cancel),
                WasPressed(_pauseAction, VirtualActionButton.Pause));
        }

        private void LateUpdate()
        {
            List<VirtualActionButton> keys = new(_virtualButtons.Keys);
            foreach (VirtualActionButton key in keys)
            {
                VirtualButtonState state = _virtualButtons[key];
                state.PressedThisFrame = false;
                _virtualButtons[key] = state;
            }
        }

        private void OnDisable()
        {
            _asset?.Disable();
        }

        public void EnableGameplay()
        {
            _gameplayMap.Enable();
        }

        public void DisableGameplay()
        {
            _gameplayMap.Disable();
        }

        public void EnableUi()
        {
            _uiMap.Enable();
        }

        public void DisableUi()
        {
            _uiMap.Disable();
        }

        public void SetVirtualMove(Vector2 move)
        {
            _virtualMove = Vector2.ClampMagnitude(move, 1f);
        }

        public void SetVirtualButton(VirtualActionButton button, bool isPressed)
        {
            _virtualButtons.TryGetValue(button, out VirtualButtonState currentState);

            if (isPressed && !currentState.IsHeld)
            {
                currentState.PressedThisFrame = true;
            }

            currentState.IsHeld = isPressed;
            _virtualButtons[button] = currentState;
        }

        private void BuildActions()
        {
            _asset = ScriptableObject.CreateInstance<InputActionAsset>();

            _gameplayMap = new InputActionMap("Gameplay");
            _moveAction = _gameplayMap.AddAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            _moveAction.AddBinding("<Gamepad>/leftStick");

            _jumpAction = _gameplayMap.AddAction("Jump", InputActionType.Button);
            _jumpAction.AddBinding("<Keyboard>/space");
            _jumpAction.AddBinding("<Gamepad>/buttonSouth");

            _attackAction = _gameplayMap.AddAction("Attack", InputActionType.Button);
            _attackAction.AddBinding("<Keyboard>/j");
            _attackAction.AddBinding("<Mouse>/leftButton");
            _attackAction.AddBinding("<Gamepad>/buttonWest");

            _dashAction = _gameplayMap.AddAction("Dash", InputActionType.Button);
            _dashAction.AddBinding("<Keyboard>/leftShift");
            _dashAction.AddBinding("<Keyboard>/k");
            _dashAction.AddBinding("<Gamepad>/rightShoulder");

            _abilityAction = _gameplayMap.AddAction("Ability", InputActionType.Button);
            _abilityAction.AddBinding("<Keyboard>/e");
            _abilityAction.AddBinding("<Gamepad>/buttonEast");

            _pauseAction = _gameplayMap.AddAction("Pause", InputActionType.Button);
            _pauseAction.AddBinding("<Keyboard>/escape");
            _pauseAction.AddBinding("<Gamepad>/start");

            _uiMap = new InputActionMap("UI");
            _uiNavigateAction = _uiMap.AddAction("Navigate", InputActionType.Value, expectedControlType: "Vector2");
            _uiNavigateAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _uiNavigateAction.AddBinding("<Gamepad>/leftStick");

            _uiSubmitAction = _uiMap.AddAction("Submit", InputActionType.Button);
            _uiSubmitAction.AddBinding("<Keyboard>/enter");
            _uiSubmitAction.AddBinding("<Gamepad>/buttonSouth");

            _uiCancelAction = _uiMap.AddAction("Cancel", InputActionType.Button);
            _uiCancelAction.AddBinding("<Keyboard>/backspace");
            _uiCancelAction.AddBinding("<Gamepad>/buttonEast");

            _debugMap = new InputActionMap("Debug");
            InputAction respawnAction = _debugMap.AddAction("Respawn", InputActionType.Button);
            respawnAction.AddBinding("<Keyboard>/r");
            InputAction toggleOverlayAction = _debugMap.AddAction("ToggleOverlay", InputActionType.Button);
            toggleOverlayAction.AddBinding("<Keyboard>/f3");

            _asset.AddActionMap(_gameplayMap);
            _asset.AddActionMap(_uiMap);
            _asset.AddActionMap(_debugMap);
        }

        private Vector2 GetCombinedMove()
        {
            Vector2 combined = _moveAction.ReadValue<Vector2>() + _virtualMove;
            return Vector2.ClampMagnitude(combined, 1f);
        }

        private bool WasPressed(InputAction action, VirtualActionButton button)
        {
            return action.WasPressedThisFrame() || (_virtualButtons.TryGetValue(button, out VirtualButtonState state) && state.PressedThisFrame);
        }

        private bool IsHeld(InputAction action, VirtualActionButton button)
        {
            return action.IsPressed() || (_virtualButtons.TryGetValue(button, out VirtualButtonState state) && state.IsHeld);
        }
    }
}
