using System.Collections.Generic;
using Lumenfall.Core;
using Lumenfall.Data;
using Lumenfall.Gameplay.Abilities;
using Lumenfall.Services;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Lumenfall.UI
{
    public sealed class VirtualStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform knob;
        [SerializeField] private float maxRadius = 76f;

        private InputService _inputService;
        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = transform as RectTransform;
        }

        public void Initialize(RectTransform knobRect)
        {
            knob = knobRect;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            UpdateDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            UpdateDrag(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_inputService == null)
            {
                ServiceRegistry.TryGet(out _inputService);
            }

            if (knob != null)
            {
                knob.anchoredPosition = Vector2.zero;
            }

            _inputService?.SetVirtualMove(Vector2.zero);
        }

        private void UpdateDrag(PointerEventData eventData)
        {
            if (_inputService == null)
            {
                ServiceRegistry.TryGet(out _inputService);
            }

            if (_rectTransform == null || knob == null)
            {
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
            Vector2 normalized = Vector2.ClampMagnitude(localPoint / maxRadius, 1f);
            knob.anchoredPosition = normalized * maxRadius;
            _inputService?.SetVirtualMove(normalized);
        }
    }

    public sealed class TouchActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private VirtualActionButton buttonType;

        private InputService _inputService;
        private Image _image;
        private Vector3 _releasedScale;
        private Color _releasedColor;
        private bool _visualsInitialized;

        private void Awake()
        {
            EnsureVisuals();
        }

        public void Initialize(VirtualActionButton actionButton)
        {
            buttonType = actionButton;
            EnsureVisuals();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_inputService == null)
            {
                ServiceRegistry.TryGet(out _inputService);
            }

            EnsureVisuals();
            transform.localScale = _releasedScale * 0.94f;
            if (_image != null)
            {
                _image.color = Color.Lerp(_releasedColor, Color.white, 0.16f);
            }

            _inputService?.SetVirtualButton(buttonType, true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Release();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Release();
        }

        private void EnsureVisuals()
        {
            if (_image == null)
            {
                _image = GetComponent<Image>();
            }

            if (!_visualsInitialized)
            {
                _releasedScale = transform.localScale;
                if (_image != null)
                {
                    _releasedColor = _image.color;
                }

                _visualsInitialized = true;
            }
        }

        private void Release()
        {
            if (_inputService == null)
            {
                ServiceRegistry.TryGet(out _inputService);
            }

            EnsureVisuals();
            transform.localScale = _releasedScale;
            if (_image != null)
            {
                _image.color = _releasedColor;
            }

            _inputService?.SetVirtualButton(buttonType, false);
        }
    }

    public sealed class MobileHudController : MonoBehaviour
    {
        [SerializeField] private bool forceVisibleInEditor = true;

        private readonly List<AbilityChipView> _abilityChips = new();

        private GameObject _gameplayRoot;
        private GameObject _pauseOverlay;
        private Text _areaLabel;
        private Text _healthLabel;
        private Text _corruptionLabel;
        private Text _footerLabel;
        private Text _contextLabel;
        private GameObject _contextButtonObject;
        private AbilityController _abilityController;
        private GameStateService _gameStateService;
        private SceneService _sceneService;
        private string _activeSceneName = string.Empty;

        private sealed class AbilityChipView
        {
            public GameObject Root;
            public Image Surface;
            public Text Label;
        }

        private void Start()
        {
            BuildHud();
            CacheServices();
            RefreshSceneState(true);
        }

        private void Update()
        {
            CacheServices();
            RefreshSceneState(false);
            UpdateAbilityController();
            UpdateVisibility();
            UpdateStatusLabels();
            UpdateAbilityChips();
        }

        private void BuildHud()
        {
            if (_gameplayRoot != null)
            {
                return;
            }

            Canvas canvas = LumenfallUiFactory.CreateCanvas("MobileHudCanvas", transform);
            RectTransform root = canvas.GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            _gameplayRoot = LumenfallUiFactory.CreateStretchObject("GameplayHudRoot", canvas.transform, Vector2.zero, Vector2.zero);
            CreateAtmosphere(_gameplayRoot.transform);
            CreateTopBar(_gameplayRoot.transform);
            CreateAbilityRail(_gameplayRoot.transform);
            CreateVirtualStick(_gameplayRoot.transform);
            CreateActionCluster(_gameplayRoot.transform);
            CreateFooterCaption(_gameplayRoot.transform);
            _pauseOverlay = CreatePauseOverlay(canvas.transform);
            _pauseOverlay.SetActive(false);
            _gameplayRoot.SetActive(false);
        }

        private void CacheServices()
        {
            if (_gameStateService == null)
            {
                ServiceRegistry.TryGet(out _gameStateService);
            }

            if (_sceneService == null)
            {
                ServiceRegistry.TryGet(out _sceneService);
            }
        }

        private void RefreshSceneState(bool forceRefresh)
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (!forceRefresh && sceneName == _activeSceneName)
            {
                return;
            }

            _activeSceneName = sceneName;
            _abilityController = Object.FindFirstObjectByType<AbilityController>();
        }

        private void UpdateAbilityController()
        {
            if (_abilityController == null && ShouldDisplayHud())
            {
                _abilityController = Object.FindFirstObjectByType<AbilityController>();
            }
        }

        private void UpdateVisibility()
        {
            bool shouldDisplayHud = ShouldDisplayHud();
            if (_gameplayRoot != null)
            {
                _gameplayRoot.SetActive(shouldDisplayHud);
            }

            bool showPauseOverlay = shouldDisplayHud && _gameStateService != null && _gameStateService.SessionState.isPaused;
            if (_pauseOverlay != null)
            {
                _pauseOverlay.SetActive(showPauseOverlay);
            }

            if (_contextButtonObject != null)
            {
                ContextActionType actionType = _abilityController != null ? _abilityController.CurrentContextAction : ContextActionType.None;
                bool showContext = shouldDisplayHud && actionType != ContextActionType.None;
                _contextButtonObject.SetActive(showContext);
                if (showContext && _contextLabel != null)
                {
                    _contextLabel.text = LumenfallUiText.ContextActionLabel(actionType);
                }
            }
        }

        private bool ShouldDisplayHud()
        {
            bool allowEditorPreview = Application.isMobilePlatform || forceVisibleInEditor;
            bool showTouchControls = _gameStateService == null || _gameStateService.ActiveSave == null || _gameStateService.ActiveSave.settings.showTouchControls;
            return allowEditorPreview && showTouchControls && IsGameplayScene(_activeSceneName);
        }

        private static bool IsGameplayScene(string sceneName)
        {
            return sceneName != LumenfallSceneNames.Boot
                && sceneName != LumenfallSceneNames.MainMenu
                && sceneName != LumenfallSceneNames.PersistentSystems;
        }

        private void UpdateStatusLabels()
        {
            if (_areaLabel == null || _healthLabel == null || _corruptionLabel == null || _footerLabel == null)
            {
                return;
            }

            string areaId = _gameStateService != null && _gameStateService.SessionState != null && !string.IsNullOrWhiteSpace(_gameStateService.SessionState.currentAreaId)
                ? _gameStateService.SessionState.currentAreaId
                : _activeSceneName;

            SaveGameData save = _gameStateService != null ? _gameStateService.ActiveSave : null;
            int currentHealth = save != null ? save.currentHealth : 5;
            int maxHealth = save != null ? save.maxHealth : 5;
            int corruption = save != null ? save.corruptionScore : 0;
            int abilityCount = save != null ? save.unlockedAbilities.Count : 0;

            _areaLabel.text = $"AREA  {LumenfallUiText.HumanizeToken(areaId)}";
            _healthLabel.text = $"HP  {currentHealth:00}/{maxHealth:00}";
            _corruptionLabel.text = $"CORR  {corruption:00}";
            _footerLabel.text = $"RITES {abilityCount:00}   |   MOVE   ATTACK   DASH   JUMP";
        }

        private void UpdateAbilityChips()
        {
            if (_abilityChips.Count == 0)
            {
                return;
            }

            SaveGameData save = _gameStateService != null ? _gameStateService.ActiveSave : null;
            AbilityType[] abilityOrder =
            {
                AbilityType.DashCore,
                AbilityType.WallClimb,
                AbilityType.LumenPulse,
                AbilityType.PhaseShift,
                AbilityType.CrystalGrapple
            };

            for (int index = 0; index < _abilityChips.Count; index++)
            {
                AbilityType ability = abilityOrder[index];
                bool unlocked = save != null && save.HasAbility(ability);
                _abilityChips[index].Surface.color = unlocked ? new Color(0.15f, 0.28f, 0.32f, 0.94f) : new Color(0.1f, 0.12f, 0.16f, 0.78f);
                _abilityChips[index].Label.color = unlocked ? LumenfallUiPalette.Accent : LumenfallUiPalette.TextDim;
                _abilityChips[index].Label.text = LumenfallUiText.AbilityLabel(ability);
                _abilityChips[index].Root.SetActive(true);
            }
        }

        private void CreateAtmosphere(Transform parent)
        {
            LumenfallUiFactory.CreateOrb("HudOrbLeft", parent, new Vector2(0f, 0f), new Vector2(360f, 360f), new Vector2(110f, 140f), new Color(0.22f, 0.82f, 0.98f, 0.12f), new Vector2(14f, 24f), 0.24f, 0.8f);
            LumenfallUiFactory.CreateOrb("HudOrbRight", parent, new Vector2(1f, 0f), new Vector2(320f, 320f), new Vector2(-160f, 170f), new Color(0.96f, 0.68f, 0.28f, 0.1f), new Vector2(18f, 14f), 0.2f, 2.2f);
        }

        private void CreateTopBar(Transform parent)
        {
            GameObject areaChip = CreateInfoChip(parent, "AreaChip", new Vector2(0f, 1f), new Vector2(44f, -40f), new Vector2(348f, 64f));
            _areaLabel = LumenfallUiFactory.CreateText("Text", areaChip.transform, "AREA  Silent Gate", 22, LumenfallUiPalette.TextPrimary, TextAnchor.MiddleLeft, FontStyle.Bold);

            GameObject healthChip = CreateInfoChip(parent, "HealthChip", new Vector2(1f, 1f), new Vector2(-260f, -40f), new Vector2(204f, 64f));
            _healthLabel = LumenfallUiFactory.CreateText("Text", healthChip.transform, "HP  05/05", 22, LumenfallUiPalette.TextPrimary, TextAnchor.MiddleCenter, FontStyle.Bold);

            GameObject corruptionChip = CreateInfoChip(parent, "CorruptionChip", new Vector2(1f, 1f), new Vector2(-44f, -40f), new Vector2(196f, 64f));
            _corruptionLabel = LumenfallUiFactory.CreateText("Text", corruptionChip.transform, "CORR  00", 22, LumenfallUiPalette.TextPrimary, TextAnchor.MiddleCenter, FontStyle.Bold);

            Button pauseButton = LumenfallUiFactory.CreateButton("PauseButton", parent, LumenfallUiPalette.PanelSoft, new Color(0.32f, 0.48f, 0.56f, 0.96f));
            RectTransform pauseRect = pauseButton.GetComponent<RectTransform>();
            pauseRect.anchorMin = new Vector2(1f, 1f);
            pauseRect.anchorMax = new Vector2(1f, 1f);
            pauseRect.pivot = new Vector2(1f, 1f);
            pauseRect.sizeDelta = new Vector2(88f, 88f);
            pauseRect.anchoredPosition = new Vector2(-44f, -124f);
            pauseButton.onClick.AddListener(TogglePause);
            LumenfallUiFactory.AddOutline(pauseButton.targetGraphic, LumenfallUiPalette.Border, new Vector2(1.2f, -1.2f));
            LumenfallUiFactory.CreateText("Text", pauseButton.transform, "II", 32, LumenfallUiPalette.TextPrimary, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        private void CreateAbilityRail(Transform parent)
        {
            GameObject rail = LumenfallUiFactory.CreateAnchoredObject("AbilityRail", parent, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(620f, 92f), new Vector2(0f, 42f), new Vector2(0.5f, 0f));
            Image railImage = LumenfallUiFactory.AddImage(rail, new Color(0.07f, 0.1f, 0.15f, 0.78f));
            LumenfallUiFactory.AddOutline(railImage, LumenfallUiPalette.Border, new Vector2(1f, -1f));

            GameObject title = LumenfallUiFactory.CreateAnchoredObject("Title", rail.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(72f, 52f), new Vector2(34f, 0f), new Vector2(0f, 0.5f));
            Text titleText = LumenfallUiFactory.CreateText("Text", title.transform, "RITES", 16, LumenfallUiPalette.TextDim, TextAnchor.MiddleLeft, FontStyle.Bold);

            AbilityType[] abilityOrder =
            {
                AbilityType.DashCore,
                AbilityType.WallClimb,
                AbilityType.LumenPulse,
                AbilityType.PhaseShift,
                AbilityType.CrystalGrapple
            };

            for (int index = 0; index < abilityOrder.Length; index++)
            {
                float xOffset = -158f + index * 114f;
                GameObject chip = LumenfallUiFactory.CreateAnchoredObject("AbilityChip", rail.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(98f, 46f), new Vector2(xOffset, 0f), new Vector2(0.5f, 0.5f));
                Image chipImage = LumenfallUiFactory.AddImage(chip, new Color(0.1f, 0.12f, 0.16f, 0.78f));
                LumenfallUiFactory.AddOutline(chipImage, new Color(0.28f, 0.42f, 0.5f, 0.72f), new Vector2(1f, -1f));
                Text chipLabel = LumenfallUiFactory.CreateText("Text", chip.transform, LumenfallUiText.AbilityLabel(abilityOrder[index]), 16, LumenfallUiPalette.TextDim, TextAnchor.MiddleCenter, FontStyle.Bold);

                _abilityChips.Add(new AbilityChipView
                {
                    Root = chip,
                    Surface = chipImage,
                    Label = chipLabel
                });
            }
        }

        private void CreateVirtualStick(Transform parent)
        {
            GameObject shell = LumenfallUiFactory.CreateAnchoredObject("StickShell", parent, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(236f, 236f), new Vector2(136f, 138f), new Vector2(0f, 0f));
            Image shellImage = LumenfallUiFactory.AddImage(shell, new Color(0.06f, 0.08f, 0.11f, 0.6f));
            LumenfallUiFactory.AddOutline(shellImage, LumenfallUiPalette.Border, new Vector2(1.2f, -1.2f));

            GameObject ring = LumenfallUiFactory.CreateAnchoredObject("Ring", shell.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(182f, 182f), Vector2.zero, new Vector2(0.5f, 0.5f));
            Image ringImage = LumenfallUiFactory.AddImage(ring, new Color(0.08f, 0.12f, 0.16f, 0.82f));
            LumenfallUiFactory.AddOutline(ringImage, LumenfallUiPalette.BorderStrong, new Vector2(1.6f, -1.6f));

            GameObject knobObject = LumenfallUiFactory.CreateAnchoredObject("Knob", ring.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(84f, 84f), Vector2.zero, new Vector2(0.5f, 0.5f));
            Image knobImage = LumenfallUiFactory.AddImage(knobObject, new Color(0.48f, 0.96f, 1f, 0.86f));
            LumenfallUiFactory.AddShadow(knobImage, new Color(0.2f, 0.72f, 0.84f, 0.64f), new Vector2(0f, -2f));

            GameObject caption = LumenfallUiFactory.CreateAnchoredObject("Caption", shell.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(120f, 24f), new Vector2(0f, 16f), new Vector2(0.5f, 0f));
            Text captionText = LumenfallUiFactory.CreateText("Text", caption.transform, "MOVE", 16, LumenfallUiPalette.TextDim, TextAnchor.MiddleCenter, FontStyle.Bold);

            VirtualStick stick = ring.AddComponent<VirtualStick>();
            stick.Initialize(knobObject.transform as RectTransform);
        }

        private void CreateActionCluster(Transform parent)
        {
            CreateActionButton(parent, "Attack", "ATK", "ATTACK", new Vector2(-132f, 214f), VirtualActionButton.Attack);
            CreateActionButton(parent, "Jump", "JMP", "JUMP", new Vector2(-246f, 118f), VirtualActionButton.Jump);
            CreateActionButton(parent, "Dash", "DSH", "DASH", new Vector2(-360f, 214f), VirtualActionButton.Dash);
            _contextButtonObject = CreateContextButton(parent, new Vector2(-246f, 308f), VirtualActionButton.Ability);
        }

        private void CreateFooterCaption(Transform parent)
        {
            GameObject footer = LumenfallUiFactory.CreateAnchoredObject("Footer", parent, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(540f, 24f), new Vector2(0f, 10f), new Vector2(0.5f, 0f));
            _footerLabel = LumenfallUiFactory.CreateText("Text", footer.transform, "RITES 00   |   MOVE   ATTACK   DASH   JUMP", 15, LumenfallUiPalette.TextDim, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        private GameObject CreatePauseOverlay(Transform parent)
        {
            GameObject overlay = LumenfallUiFactory.CreateStretchObject("PauseOverlay", parent, Vector2.zero, Vector2.zero);
            Image overlayImage = LumenfallUiFactory.AddImage(overlay, new Color(0.02f, 0.03f, 0.05f, 0.82f), true);

            GameObject card = LumenfallUiFactory.CreateAnchoredObject("PauseCard", overlay.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(520f, 320f), Vector2.zero, new Vector2(0.5f, 0.5f));
            Image cardImage = LumenfallUiFactory.AddImage(card, new Color(0.08f, 0.11f, 0.17f, 0.96f));
            LumenfallUiFactory.AddOutline(cardImage, LumenfallUiPalette.BorderStrong, new Vector2(1.8f, -1.8f));

            GameObject title = LumenfallUiFactory.CreateAnchoredObject("Title", card.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(420f, 48f), new Vector2(0f, -42f), new Vector2(0.5f, 1f));
            Text titleText = LumenfallUiFactory.CreateText("Text", title.transform, "PAUSED", 34, LumenfallUiPalette.TextPrimary, TextAnchor.MiddleCenter, FontStyle.Bold);

            GameObject description = LumenfallUiFactory.CreateAnchoredObject("Description", card.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(420f, 60f), new Vector2(0f, -96f), new Vector2(0.5f, 1f));
            Text descriptionText = LumenfallUiFactory.CreateText("Text", description.transform, "Hold the line, save the current cycle, or return to the redesigned menu.", 20, LumenfallUiPalette.TextMuted, TextAnchor.UpperCenter);

            Button resumeButton = CreateOverlayButton(card.transform, "ResumeButton", "RESUME", new Vector2(0f, 32f));
            resumeButton.onClick.AddListener(ResumeGameplay);

            Button saveButton = CreateOverlayButton(card.transform, "SaveButton", "SAVE CYCLE", new Vector2(0f, -40f));
            saveButton.onClick.AddListener(SaveCycle);

            Button menuButton = CreateOverlayButton(card.transform, "MenuButton", "MAIN MENU", new Vector2(0f, -112f));
            menuButton.onClick.AddListener(ReturnToMenu);

            return overlay;
        }

        private GameObject CreateInfoChip(Transform parent, string name, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject chip = LumenfallUiFactory.CreateAnchoredObject(name, parent, anchor, anchor, size, anchoredPosition, anchor);
            Image image = LumenfallUiFactory.AddImage(chip, new Color(0.08f, 0.12f, 0.16f, 0.72f));
            LumenfallUiFactory.AddOutline(image, LumenfallUiPalette.Border, new Vector2(1.2f, -1.2f));
            return chip;
        }

        private void CreateActionButton(Transform parent, string name, string glyph, string caption, Vector2 anchoredPosition, VirtualActionButton buttonType)
        {
            GameObject shell = LumenfallUiFactory.CreateAnchoredObject(name, parent, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(108f, 124f), anchoredPosition, new Vector2(1f, 0f));

            GameObject buttonObject = LumenfallUiFactory.CreateAnchoredObject("Button", shell.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(108f, 108f), new Vector2(0f, 16f), new Vector2(0.5f, 0.5f));
            Image image = LumenfallUiFactory.AddImage(buttonObject, new Color(0.08f, 0.12f, 0.16f, 0.8f), true);
            LumenfallUiFactory.AddOutline(image, LumenfallUiPalette.BorderStrong, new Vector2(1.4f, -1.4f));

            TouchActionButton button = buttonObject.AddComponent<TouchActionButton>();
            button.Initialize(buttonType);

            Text glyphText = LumenfallUiFactory.CreateText("Glyph", buttonObject.transform, glyph, 22, LumenfallUiPalette.TextPrimary, TextAnchor.MiddleCenter, FontStyle.Bold);

            GameObject captionObject = LumenfallUiFactory.CreateAnchoredObject("Caption", shell.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(108f, 20f), new Vector2(0f, 0f), new Vector2(0.5f, 0f));
            Text captionText = LumenfallUiFactory.CreateText("Text", captionObject.transform, caption, 14, LumenfallUiPalette.TextDim, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        private GameObject CreateContextButton(Transform parent, Vector2 anchoredPosition, VirtualActionButton buttonType)
        {
            GameObject buttonObject = LumenfallUiFactory.CreateAnchoredObject("ContextButton", parent, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(168f, 54f), anchoredPosition, new Vector2(1f, 0f));
            Image image = LumenfallUiFactory.AddImage(buttonObject, new Color(0.18f, 0.33f, 0.38f, 0.9f), true);
            LumenfallUiFactory.AddOutline(image, LumenfallUiPalette.BorderStrong, new Vector2(1.2f, -1.2f));

            TouchActionButton button = buttonObject.AddComponent<TouchActionButton>();
            button.Initialize(buttonType);

            _contextLabel = LumenfallUiFactory.CreateText("Text", buttonObject.transform, "CONTEXT", 18, LumenfallUiPalette.TextPrimary, TextAnchor.MiddleCenter, FontStyle.Bold);
            return buttonObject;
        }

        private Button CreateOverlayButton(Transform parent, string name, string label, Vector2 anchoredPosition)
        {
            Button button = LumenfallUiFactory.CreateButton(name, parent, LumenfallUiPalette.AccentSoft, new Color(0.54f, 0.96f, 1f, 0.44f));
            RectTransform rectTransform = button.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(320f, 56f);
            rectTransform.anchoredPosition = anchoredPosition;
            LumenfallUiFactory.AddOutline(button.targetGraphic, LumenfallUiPalette.BorderStrong, new Vector2(1.2f, -1.2f));
            LumenfallUiFactory.CreateText("Text", button.transform, label, 22, LumenfallUiPalette.TextPrimary, TextAnchor.MiddleCenter, FontStyle.Bold);
            return button;
        }

        private void TogglePause()
        {
            if (_gameStateService == null)
            {
                return;
            }

            _gameStateService.SetPaused(!_gameStateService.SessionState.isPaused);
        }

        private void ResumeGameplay()
        {
            if (_gameStateService == null)
            {
                return;
            }

            _gameStateService.SetPaused(false);
        }

        private void SaveCycle()
        {
            _gameStateService?.SaveToActiveSlot();
        }

        private void ReturnToMenu()
        {
            if (_gameStateService != null)
            {
                _gameStateService.SetPaused(false);
            }

            if (_sceneService != null)
            {
                _sceneService.LoadMainMenu();
            }
            else if (Application.CanStreamedLevelBeLoaded(LumenfallSceneNames.MainMenu))
            {
                SceneManager.LoadScene(LumenfallSceneNames.MainMenu, LoadSceneMode.Single);
            }
        }
    }
}
