using System;
using System.Collections.Generic;
using AurigaGames.Deep.Characters;
using AurigaGames.Deep.Core;
using AurigaGames.Deep.Core.AbilitySystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AurigaGames.Deep.Players
{
    [RequireComponent(typeof(PlayerInput))]
    [DisallowMultipleComponent]
    public class PlayerController : MonoBehaviour, IAbilityBinder
    {
        private readonly List<AbilityBinding> _abilityBindings = new();

        private CharacterBody _body;
        private CharacterMovement _movement;
        private AbilityController _abilityController;
        private PlayerInput _playerInput;
        private InputAction _moveAction;
        private InputAction _lookAction;

        private Vector3 _planarDirection;
        private float _targetVerticalAngle;

        private Quaternion _planarRotation;
        private Quaternion _verticalRotation;

        [Tooltip("X = Change in mouse position.\nY = Multiplicative factor for camera rotation.")]
        public AnimationCurve LookSensitivityCurve = new(
            new Keyframe(0f, 0.5f, 0f, 5f),
            new Keyframe(1f, 2.5f, 0f, 0f));

        public float LookSensitivityFactor = 0.2f;

        public float MinVerticalAngle = -89.0f;
        public float MaxVerticalAngle = 89.0f;
        public float RotationLerpTime = 0.15f;

        public Transform CameraTarget;

        private void Awake()
        {
            _body = GetComponent<CharacterBody>();
            _movement = GetComponent<CharacterMovement>();
            _abilityController = GetComponent<AbilityController>();

            _playerInput = GetComponent<PlayerInput>();
            _moveAction = _playerInput.actions["Move"];
            _lookAction = _playerInput.actions["Look"];

            _planarDirection = _movement.transform.forward;
            _targetVerticalAngle = 0.0f;
        }

        private void Start()
        {
            foreach (var abilityBinding in _abilityBindings)
            {
                var inputAction = _playerInput.actions.FindAction(abilityBinding.Action);
                if (inputAction == null)
                {
                    Debug.LogWarning($"Ability action '{abilityBinding.Action}' not found in '{gameObject.name}'");
                    continue;
                }

                abilityBinding.Performed = _ => _abilityController.ExecuteAbility(abilityBinding.Instance);
                inputAction.performed += abilityBinding.Performed;

                if (abilityBinding.Instance is IAbilityCancelableInstance cancelable)
                {
                    abilityBinding.Canceled = _ => _abilityController.EndAbility(cancelable);
                    inputAction.canceled += abilityBinding.Canceled;
                }
            }
        }

        private void OnDestroy()
        {
            foreach (var abilityBinding in _abilityBindings)
            {
                var inputAction = _playerInput.actions.FindAction(abilityBinding.Action);
                if (inputAction != null)
                {
                    if (abilityBinding.Performed != null)
                        inputAction.performed -= abilityBinding.Performed;
                    if (abilityBinding.Canceled != null)
                        inputAction.canceled -= abilityBinding.Canceled;
                }
            }
        }

        private void Update()
        {
            HandleLook(Time.deltaTime);

            var moveInput = CalculateMoveInput();

            _movement.SetMoveInput(_planarRotation * moveInput);

            if (CameraTarget)
            {
                CameraTarget.localRotation = _verticalRotation;
            }

            _body.AimPitch = MathHelper.NormalizeAngle(_verticalRotation.eulerAngles.x);
        }

        private void HandleLook(float deltaTime)
        {
            var rotationInput = CalculateLookInput();

            var rotationLerpPct = 1f - Mathf.Exp((Mathf.Log(1f - 0.99f) / RotationLerpTime) * deltaTime);

            var followUp = transform.up;
            var rotationFromInput = Quaternion.Euler(followUp * (rotationInput.x));
            _planarDirection = rotationFromInput * _planarDirection;
            _planarDirection = Vector3.Cross(followUp, Vector3.Cross(_planarDirection, followUp));
            var planarRotation = Quaternion.LookRotation(_planarDirection, followUp);
            _planarRotation = Quaternion.Slerp(_planarRotation, planarRotation, rotationLerpPct);

            _targetVerticalAngle -= rotationInput.y;
            _targetVerticalAngle = Mathf.Clamp(_targetVerticalAngle, MinVerticalAngle, MaxVerticalAngle);
            var verticalRotation = Quaternion.Euler(_targetVerticalAngle, 0, 0);
            _verticalRotation = Quaternion.Slerp(_verticalRotation, verticalRotation, rotationLerpPct);

            _movement.SetLookInput(_planarRotation * Vector3.forward);
        }

        private Vector3 CalculateMoveInput()
        {
            var moveInput = _moveAction.ReadValue<Vector2>();
            return Vector3.ClampMagnitude(new Vector3(moveInput.x, 0.0f, moveInput.y), 1.0f);
        }

        private Vector3 CalculateLookInput()
        {
            var lookInput = _lookAction.ReadValue<Vector2>() * LookSensitivityFactor;
            if (!ApplicationManager.Instance.IsLookAvailable)
            {
                lookInput = Vector2.zero;
            }

            return lookInput * LookSensitivityCurve.Evaluate(lookInput.magnitude);
        }

        public void BindAbility(string action, IAbilityInstance abilityInstance)
        {
            _abilityBindings.Add(new AbilityBinding(action, abilityInstance));
        }

        private sealed class AbilityBinding
        {
            public string Action;
            public Action<InputAction.CallbackContext> Performed;
            public Action<InputAction.CallbackContext> Canceled;
            public IAbilityInstance Instance;

            public AbilityBinding(string action, IAbilityInstance instance)
            {
                Action = action;
                Instance = instance;
            }
        }
    }
}
