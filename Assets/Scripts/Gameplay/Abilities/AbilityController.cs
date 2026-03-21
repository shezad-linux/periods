using System;
using System.Collections.Generic;
using Lumenfall.Data;
using Lumenfall.Gameplay.Player;
using Lumenfall.Services;
using UnityEngine;

namespace Lumenfall.Gameplay.Abilities
{
    public abstract class AbilityModule
    {
        protected AbilityController Controller;
        protected AbilityRuntimeState RuntimeState;

        public abstract AbilityType AbilityType { get; }

        public void Initialize(AbilityController controller, AbilityRuntimeState runtimeState)
        {
            Controller = controller;
            RuntimeState = runtimeState;
        }

        public virtual bool TryUse(IContextActionTarget target)
        {
            return target != null;
        }
    }

    public sealed class DashAbility : AbilityModule
    {
        public override AbilityType AbilityType => AbilityType.DashCore;

        public override bool TryUse(IContextActionTarget target)
        {
            return Controller.PlayerMotor != null && Controller.PlayerMotor.BeginDash();
        }
    }

    public sealed class WallClimbAbility : AbilityModule
    {
        public override AbilityType AbilityType => AbilityType.WallClimb;
    }

    public sealed class PulseAbility : AbilityModule
    {
        public override AbilityType AbilityType => AbilityType.LumenPulse;

        public override bool TryUse(IContextActionTarget target)
        {
            if (target == null || target.ActionType != ContextActionType.Pulse)
            {
                return false;
            }

            target.Perform(Controller.gameObject);
            return true;
        }
    }

    public sealed class PhaseAbility : AbilityModule
    {
        public override AbilityType AbilityType => AbilityType.PhaseShift;

        public override bool TryUse(IContextActionTarget target)
        {
            if (target == null || target.ActionType != ContextActionType.PhaseWall)
            {
                return false;
            }

            target.Perform(Controller.gameObject);
            return true;
        }
    }

    public sealed class GrappleAbility : AbilityModule
    {
        public override AbilityType AbilityType => AbilityType.CrystalGrapple;

        public override bool TryUse(IContextActionTarget target)
        {
            if (target == null || target.ActionType != ContextActionType.Grapple)
            {
                return false;
            }

            target.Perform(Controller.gameObject);
            return true;
        }
    }

    [RequireComponent(typeof(ContextResolver))]
    public sealed class AbilityController : MonoBehaviour
    {
        [SerializeField] private List<AbilityDefinition> abilityDefinitions = new();

        private readonly Dictionary<AbilityType, AbilityRuntimeState> _runtimeStates = new();
        private readonly Dictionary<AbilityType, AbilityModule> _modules = new();
        private ContextResolver _contextResolver;
        private GameStateService _gameStateService;
        private InputService _inputService;

        public PlayerMotor2D PlayerMotor { get; private set; }

        public ContextActionType CurrentContextAction => _contextResolver != null ? _contextResolver.CurrentActionType : ContextActionType.None;

        public IContextActionTarget CurrentTarget => _contextResolver != null ? _contextResolver.CurrentTarget : null;

        private void Awake()
        {
            PlayerMotor = GetComponent<PlayerMotor2D>();
            _contextResolver = GetComponent<ContextResolver>();
            BuildRuntimeStates();
            BuildModules();
        }

        private void Update()
        {
            if (_gameStateService == null)
            {
                ServiceRegistry.TryGet(out _gameStateService);
            }

            if (_inputService == null)
            {
                ServiceRegistry.TryGet(out _inputService);
            }

            if (_gameStateService != null)
            {
                foreach (AbilityType unlockedAbility in _gameStateService.ActiveSave.unlockedAbilities)
                {
                    if (_runtimeStates.TryGetValue(unlockedAbility, out AbilityRuntimeState runtimeState))
                    {
                        runtimeState.isUnlocked = true;
                    }
                }
            }

            float deltaTime = Time.deltaTime;
            foreach (AbilityRuntimeState runtimeState in _runtimeStates.Values)
            {
                runtimeState.cooldownRemaining = Mathf.Max(0f, runtimeState.cooldownRemaining - deltaTime);
            }

            if (_inputService != null && _inputService.Gameplay.AbilityPressed)
            {
                TryUseContextAction();
            }
        }

        public bool IsUnlocked(AbilityType abilityType)
        {
            return _runtimeStates.TryGetValue(abilityType, out AbilityRuntimeState state) && state.isUnlocked;
        }

        public bool TryDash()
        {
            return TryUseAbility(AbilityType.DashCore, null);
        }

        public bool TryUseContextAction()
        {
            IContextActionTarget target = CurrentTarget;
            if (target == null)
            {
                return false;
            }

            return target.ActionType switch
            {
                ContextActionType.Grapple => TryUseAbility(AbilityType.CrystalGrapple, target),
                ContextActionType.PhaseWall => TryUseAbility(AbilityType.PhaseShift, target),
                ContextActionType.Pulse => TryUseAbility(AbilityType.LumenPulse, target),
                ContextActionType.Interact => TryInteract(target),
                _ => false
            };
        }

        private bool TryInteract(IContextActionTarget target)
        {
            if (target == null || _gameStateService == null || !target.IsAvailable(_gameStateService.ActiveSave))
            {
                return false;
            }

            target.Perform(gameObject);
            return true;
        }

        private bool TryUseAbility(AbilityType abilityType, IContextActionTarget target)
        {
            if (!_runtimeStates.TryGetValue(abilityType, out AbilityRuntimeState runtimeState))
            {
                return false;
            }

            if (!runtimeState.isUnlocked || runtimeState.cooldownRemaining > 0f)
            {
                return false;
            }

            if (!_modules.TryGetValue(abilityType, out AbilityModule module) || !module.TryUse(target))
            {
                return false;
            }

            AbilityDefinition definition = abilityDefinitions.Find(item => item != null && item.abilityType == abilityType);
            runtimeState.cooldownRemaining = definition != null ? definition.cooldownSeconds : 0f;
            _gameStateService?.AddCorruption(target == null ? 0 : 1);
            return true;
        }

        private void BuildRuntimeStates()
        {
            foreach (AbilityType abilityType in Enum.GetValues(typeof(AbilityType)))
            {
                _runtimeStates[abilityType] = new AbilityRuntimeState
                {
                    abilityType = abilityType
                };
            }
        }

        private void BuildModules()
        {
            RegisterModule(new DashAbility());
            RegisterModule(new WallClimbAbility());
            RegisterModule(new PulseAbility());
            RegisterModule(new PhaseAbility());
            RegisterModule(new GrappleAbility());
        }

        private void RegisterModule(AbilityModule module)
        {
            AbilityRuntimeState runtimeState = _runtimeStates[module.AbilityType];
            module.Initialize(this, runtimeState);
            _modules[module.AbilityType] = module;
        }
    }
}
