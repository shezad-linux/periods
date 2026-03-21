using System.Collections.Generic;
using Lumenfall.Core;
using Lumenfall.Data;
using Lumenfall.Services;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Lumenfall.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        private const string PrototypeSceneName = "SampleScene";

        private readonly List<SlotCardView> _slotCards = new();
        private readonly List<SlotSnapshot> _slotSnapshots = new();

        private SaveService _saveService;
        private GameStateService _gameStateService;
        private Text _detailEyebrow;
        private Text _detailTitle;
        private Text _detailSummary;
        private Text _detailMeta;
        private Text _detailHint;
        private Text _enterButtonLabel;
        private int _selectedSlotIndex;

        private sealed class SlotCardView
        {
            public Button Button;
            public Image Surface;
            public Text Label;
            public Text Summary;
            public Text Meta;
        }

        private readonly struct SlotSnapshot
        {
            public readonly int SlotIndex;
            public readonly bool HasData;
            public readonly SaveGameData Save;

            public SlotSnapshot(int slotIndex, bool hasData, SaveGameData save)
            {
                SlotIndex = slotIndex;
                HasData = hasData;
                Save = save;
            }
        }

        private void Awake()
        {
            BuildMenu();
            CacheServices();
            RefreshSlots();
        }

        private void OnEnable()
        {
            CacheServices();
            RefreshSlots();
        }

        private void CacheServices()
        {
            if (_saveService == null)
            {
                ServiceRegistry.TryGet(out _saveService);
            }

            if (_gameStateService == null)
            {
                ServiceRegistry.TryGet(out _gameStateService);
            }
        }

        private void BuildMenu()
        {
            Canvas canvas = LumenfallUiFactory.CreateCanvas("MainMenuCanvas", transform);
            RectTransform root = canvas.GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            GameObject background = LumenfallUiFactory.CreateStretchObject("Background", canvas.transform, Vector2.zero, Vector2.zero);
            LumenfallUiFactory.AddImage(background, LumenfallUiPalette.Void);

            LumenfallUiFactory.CreateOrb("OrbNorth", canvas.transform, new Vector2(0f, 1f), new Vector2(760f, 760f), new Vector2(180f, -120f), LumenfallUiPalette.AccentSoft, new Vector2(24f, 16f), 0.18f, 0.25f);
            LumenfallUiFactory.CreateOrb("OrbEast", canvas.transform, new Vector2(1f, 0.1f), new Vector2(520f, 520f), new Vector2(-140f, 220f), LumenfallUiPalette.EmberSoft, new Vector2(18f, 30f), 0.22f, 1.2f);
            LumenfallUiFactory.CreateOrb("OrbSouth", canvas.transform, new Vector2(0.56f, 0f), new Vector2(420f, 420f), new Vector2(-140f, 100f), new Color(0.35f, 0.72f, 0.96f, 0.16f), new Vector2(28f, 12f), 0.28f, 2.1f);

            GameObject heroFrame = LumenfallUiFactory.CreateStretchObject("HeroFrame", canvas.transform, new Vector2(92f, 88f), new Vector2(-720f, -88f));
            Image heroFrameImage = LumenfallUiFactory.AddImage(heroFrame, LumenfallUiPalette.PanelGlass);
            LumenfallUiFactory.AddOutline(heroFrameImage, LumenfallUiPalette.Border, new Vector2(1.4f, -1.4f));

            GameObject slotsFrame = LumenfallUiFactory.CreateStretchObject("SlotsFrame", canvas.transform, new Vector2(1260f, 88f), new Vector2(-92f, -88f));
            Image slotFrameImage = LumenfallUiFactory.AddImage(slotsFrame, new Color(0.06f, 0.09f, 0.14f, 0.94f));
            LumenfallUiFactory.AddOutline(slotFrameImage, LumenfallUiPalette.BorderStrong, new Vector2(1.6f, -1.6f));

            BuildHeroColumn(heroFrame.transform);
            BuildSlotsColumn(slotsFrame.transform);
        }

        private void BuildHeroColumn(Transform parent)
        {
            RectTransform heroRect = parent as RectTransform;
            if (heroRect == null)
            {
                return;
            }

            GameObject eyebrow = LumenfallUiFactory.CreateAnchoredObject("Eyebrow", parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(760f, 40f), new Vector2(68f, -72f), new Vector2(0f, 1f));
            Text eyebrowText = LumenfallUiFactory.CreateText("Text", eyebrow.transform, "ASHES OF LUMENFALL", 22, LumenfallUiPalette.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            LumenfallUiFactory.AddShadow(eyebrowText, new Color(0.14f, 0.5f, 0.58f, 0.72f), new Vector2(0f, -1.4f));

            GameObject title = LumenfallUiFactory.CreateAnchoredObject("Title", parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(900f, 220f), new Vector2(68f, -210f), new Vector2(0f, 1f));
            Text titleText = LumenfallUiFactory.CreateText("Text", title.transform, "Luminous ruins,\nritual menus,\nand a calmer HUD.", 64, LumenfallUiPalette.TextPrimary, TextAnchor.UpperLeft, FontStyle.Bold);
            LumenfallUiFactory.AddShadow(titleText, new Color(0f, 0f, 0f, 0.4f), new Vector2(0f, -3f));

            GameObject synopsis = LumenfallUiFactory.CreateAnchoredObject("Synopsis", parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(760f, 110f), new Vector2(68f, -410f), new Vector2(0f, 1f));
            Text synopsisText = LumenfallUiFactory.CreateText(
                "Text",
                synopsis.transform,
                "A Figma-led refresh for Ashes of Lumenfall: stronger hierarchy, save-slot clarity, luminous atmospheric panels, and a touch-first combat HUD that finally belongs to the world.",
                26,
                LumenfallUiPalette.TextMuted,
                TextAnchor.UpperLeft);

            BuildFeatureStrip(parent);
            BuildDetailPanel(parent);
        }

        private void BuildFeatureStrip(Transform parent)
        {
            string[] featureTitles =
            {
                "Save Slot Telemetry",
                "Combat-First Touch Layout",
                "World-State Readability"
            };

            string[] featureDescriptions =
            {
                "Each cycle exposes health, corruption, room progress, and ability count before you commit.",
                "Jump, dash, attack, and context actions form a readable arc instead of floating utility circles.",
                "Area, corruption, and unlocked traversal tools stay visible without stealing combat space."
            };

            for (int index = 0; index < featureTitles.Length; index++)
            {
                float xOffset = 68f + index * 244f;
                GameObject card = LumenfallUiFactory.CreateAnchoredObject("FeatureCard", parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(220f, 164f), new Vector2(xOffset, -550f), new Vector2(0f, 1f));
                Image image = LumenfallUiFactory.AddImage(card, LumenfallUiPalette.PanelSoft);
                LumenfallUiFactory.AddOutline(image, LumenfallUiPalette.Border, new Vector2(1f, -1f));

                GameObject titleContainer = LumenfallUiFactory.CreateStretchObject("Title", card.transform, new Vector2(18f, -18f), new Vector2(-18f, -92f));
                Text titleText = LumenfallUiFactory.CreateText("Text", titleContainer.transform, featureTitles[index], 22, LumenfallUiPalette.TextPrimary, TextAnchor.UpperLeft, FontStyle.Bold);

                GameObject descriptionContainer = LumenfallUiFactory.CreateStretchObject("Description", card.transform, new Vector2(18f, 56f), new Vector2(-18f, -18f));
                Text descriptionText = LumenfallUiFactory.CreateText("Text", descriptionContainer.transform, featureDescriptions[index], 18, LumenfallUiPalette.TextMuted, TextAnchor.UpperLeft);
                descriptionText.resizeTextForBestFit = true;
                descriptionText.resizeTextMinSize = 14;
                descriptionText.resizeTextMaxSize = 18;
            }
        }

        private void BuildDetailPanel(Transform parent)
        {
            GameObject detailCard = LumenfallUiFactory.CreateAnchoredObject("DetailCard", parent, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(760f, 286f), new Vector2(68f, 70f), new Vector2(0f, 0f));
            Image detailImage = LumenfallUiFactory.AddImage(detailCard, new Color(0.07f, 0.11f, 0.17f, 0.96f));
            LumenfallUiFactory.AddOutline(detailImage, LumenfallUiPalette.BorderStrong, new Vector2(1.8f, -1.8f));

            GameObject eyebrow = LumenfallUiFactory.CreateAnchoredObject("Eyebrow", detailCard.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(360f, 32f), new Vector2(28f, -28f), new Vector2(0f, 1f));
            _detailEyebrow = LumenfallUiFactory.CreateText("Text", eyebrow.transform, "SELECTED SLOT", 18, LumenfallUiPalette.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);

            GameObject title = LumenfallUiFactory.CreateAnchoredObject("Title", detailCard.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(520f, 72f), new Vector2(28f, -72f), new Vector2(0f, 1f));
            _detailTitle = LumenfallUiFactory.CreateText("Text", title.transform, "Cycle Slot 01", 38, LumenfallUiPalette.TextPrimary, TextAnchor.UpperLeft, FontStyle.Bold);

            GameObject summary = LumenfallUiFactory.CreateAnchoredObject("Summary", detailCard.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(520f, 72f), new Vector2(28f, -132f), new Vector2(0f, 1f));
            _detailSummary = LumenfallUiFactory.CreateText("Text", summary.transform, "Fresh cycle ready to descend into the Silent Gate.", 22, LumenfallUiPalette.TextMuted, TextAnchor.UpperLeft);

            GameObject meta = LumenfallUiFactory.CreateAnchoredObject("Meta", detailCard.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(520f, 80f), new Vector2(28f, -198f), new Vector2(0f, 1f));
            _detailMeta = LumenfallUiFactory.CreateText("Text", meta.transform, "HP 05/05  |  CORRUPTION 00  |  ABILITIES 00", 18, LumenfallUiPalette.TextDim, TextAnchor.UpperLeft, FontStyle.Bold);

            GameObject hint = LumenfallUiFactory.CreateAnchoredObject("Hint", detailCard.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(520f, 24f), new Vector2(28f, 24f), new Vector2(0f, 0f));
            _detailHint = LumenfallUiFactory.CreateText("Text", hint.transform, "Designed as a staging view for the Figma handoff and runtime menu prototype.", 16, LumenfallUiPalette.TextDim, TextAnchor.MiddleLeft);

            Button enterButton = LumenfallUiFactory.CreateButton("EnterButton", detailCard.transform, LumenfallUiPalette.AccentSoft, new Color(0.55f, 0.96f, 1f, 0.42f));
            RectTransform enterRect = enterButton.GetComponent<RectTransform>();
            enterRect.anchorMin = new Vector2(1f, 0f);
            enterRect.anchorMax = new Vector2(1f, 0f);
            enterRect.pivot = new Vector2(1f, 0f);
            enterRect.sizeDelta = new Vector2(188f, 88f);
            enterRect.anchoredPosition = new Vector2(-28f, 28f);
            enterButton.onClick.AddListener(LaunchSelectedSlot);
            LumenfallUiFactory.AddOutline(enterButton.targetGraphic, LumenfallUiPalette.BorderStrong, new Vector2(1.4f, -1.4f));

            _enterButtonLabel = LumenfallUiFactory.CreateText("Label", enterButton.transform, "BEGIN", 26, LumenfallUiPalette.TextPrimary, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        private void BuildSlotsColumn(Transform parent)
        {
            GameObject heading = LumenfallUiFactory.CreateAnchoredObject("Heading", parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(420f, 44f), new Vector2(32f, -34f), new Vector2(0f, 1f));
            Text headingText = LumenfallUiFactory.CreateText("Text", heading.transform, "SAVE SLOTS", 24, LumenfallUiPalette.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);

            GameObject subheading = LumenfallUiFactory.CreateAnchoredObject("Subheading", parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(420f, 64f), new Vector2(32f, -82f), new Vector2(0f, 1f));
            Text subheadingText = LumenfallUiFactory.CreateText("Text", subheading.transform, "Select the active cycle, review its world state, then continue into the prototype scene.", 18, LumenfallUiPalette.TextMuted, TextAnchor.UpperLeft);
            subheadingText.resizeTextForBestFit = true;
            subheadingText.resizeTextMinSize = 14;
            subheadingText.resizeTextMaxSize = 18;

            for (int index = 0; index < LumenfallConstants.MaxSaveSlots; index++)
            {
                float yOffset = -176f - index * 184f;
                Button button = LumenfallUiFactory.CreateButton("SlotButton", parent, LumenfallUiPalette.PanelSoft, new Color(0.26f, 0.38f, 0.46f, 0.96f));
                RectTransform rectTransform = button.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0f, 1f);
                rectTransform.anchorMax = new Vector2(1f, 1f);
                rectTransform.pivot = new Vector2(0.5f, 1f);
                rectTransform.sizeDelta = new Vector2(-56f, 156f);
                rectTransform.anchoredPosition = new Vector2(0f, yOffset);

                int slotIndex = index;
                button.onClick.AddListener(() => SelectSlot(slotIndex));

                Image surface = button.GetComponent<Image>();
                LumenfallUiFactory.AddOutline(surface, LumenfallUiPalette.Border, new Vector2(1.2f, -1.2f));

                GameObject label = LumenfallUiFactory.CreateStretchObject("Label", button.transform, new Vector2(22f, -20f), new Vector2(-22f, -96f));
                Text labelText = LumenfallUiFactory.CreateText("Text", label.transform, $"Cycle Slot {index + 1:00}", 26, LumenfallUiPalette.TextPrimary, TextAnchor.UpperLeft, FontStyle.Bold);

                GameObject summary = LumenfallUiFactory.CreateStretchObject("Summary", button.transform, new Vector2(22f, 54f), new Vector2(-22f, -46f));
                Text summaryText = LumenfallUiFactory.CreateText("Text", summary.transform, "Fresh cycle", 18, LumenfallUiPalette.TextMuted, TextAnchor.UpperLeft);

                GameObject meta = LumenfallUiFactory.CreateAnchoredObject("Meta", button.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(-44f, 26f), new Vector2(0f, 18f), new Vector2(0.5f, 0f));
                Text metaText = LumenfallUiFactory.CreateText("Text", meta.transform, "HP 05/05  |  CORR 00  |  AB 00", 16, LumenfallUiPalette.TextDim, TextAnchor.MiddleLeft, FontStyle.Bold);

                _slotCards.Add(new SlotCardView
                {
                    Button = button,
                    Surface = surface,
                    Label = labelText,
                    Summary = summaryText,
                    Meta = metaText
                });
            }
        }

        private void RefreshSlots()
        {
            CacheServices();
            _slotSnapshots.Clear();
            if (_saveService == null)
            {
                return;
            }

            _saveService.EnsureSaveDirectory();
            for (int slotIndex = 0; slotIndex < LumenfallConstants.MaxSaveSlots; slotIndex++)
            {
                bool hasData = _saveService.HasSlotData(slotIndex);
                SaveGameData save = hasData ? _saveService.LoadSlot(slotIndex) : SaveGameData.CreateDefault(slotIndex);
                _slotSnapshots.Add(new SlotSnapshot(slotIndex, hasData, save));
                ApplySlotSnapshot(_slotCards[slotIndex], _slotSnapshots[slotIndex]);
            }

            _selectedSlotIndex = Mathf.Clamp(_selectedSlotIndex, 0, _slotSnapshots.Count - 1);
            SelectSlot(_selectedSlotIndex);
        }

        private void ApplySlotSnapshot(SlotCardView view, SlotSnapshot snapshot)
        {
            SaveGameData save = snapshot.Save;
            string slotLabel = $"Cycle Slot {snapshot.SlotIndex + 1:00}";
            string title = snapshot.HasData ? slotLabel : $"{slotLabel}  |  New Cycle";
            string location = snapshot.HasData ? LumenfallUiText.HumanizeToken(save.currentAreaId) : "Silent Gate";
            string room = snapshot.HasData ? LumenfallUiText.HumanizeToken(save.activeRoomId) : "Awakening Chamber";
            string stateLine = snapshot.HasData ? $"Continue from {location}" : "Fresh descent prepared";

            view.Label.text = title;
            view.Summary.text = $"{stateLine}\n{room}";
            view.Meta.text = $"HP {save.currentHealth:00}/{save.maxHealth:00}  |  CORR {save.corruptionScore:00}  |  AB {save.unlockedAbilities.Count:00}";
        }

        private void SelectSlot(int slotIndex)
        {
            if (_slotSnapshots.Count == 0)
            {
                return;
            }

            _selectedSlotIndex = Mathf.Clamp(slotIndex, 0, _slotSnapshots.Count - 1);
            for (int index = 0; index < _slotCards.Count; index++)
            {
                bool isSelected = index == _selectedSlotIndex;
                _slotCards[index].Surface.color = isSelected ? new Color(0.16f, 0.25f, 0.31f, 0.98f) : LumenfallUiPalette.PanelSoft;
                _slotCards[index].Label.color = isSelected ? LumenfallUiPalette.TextPrimary : new Color(0.85f, 0.92f, 0.98f, 0.94f);
                _slotCards[index].Summary.color = isSelected ? LumenfallUiPalette.TextPrimary : LumenfallUiPalette.TextMuted;
                _slotCards[index].Meta.color = isSelected ? LumenfallUiPalette.Accent : LumenfallUiPalette.TextDim;
            }

            SlotSnapshot snapshot = _slotSnapshots[_selectedSlotIndex];
            SaveGameData save = snapshot.Save;
            string areaName = snapshot.HasData ? LumenfallUiText.HumanizeToken(save.currentAreaId) : "Silent Gate";
            string roomName = snapshot.HasData ? LumenfallUiText.HumanizeToken(save.activeRoomId) : "Awakening Chamber";

            _detailEyebrow.text = snapshot.HasData ? "ACTIVE CYCLE" : "NEW CYCLE";
            _detailTitle.text = snapshot.HasData
                ? $"Cycle Slot {snapshot.SlotIndex + 1:00}  |  {areaName}"
                : $"Cycle Slot {snapshot.SlotIndex + 1:00}  |  Fresh Descent";
            _detailSummary.text = snapshot.HasData
                ? $"Resume from {roomName} with {save.unlockedAbilities.Count} traversal rites and a corruption score of {save.corruptionScore:00}."
                : "Begin with full vitality, empty corruption, and the first descent through the Silent Gate.";
            _detailMeta.text = $"HP {save.currentHealth:00}/{save.maxHealth:00}  |  CORRUPTION {save.corruptionScore:00}  |  ABILITIES {save.unlockedAbilities.Count:00}";
            _detailHint.text = snapshot.HasData
                ? "Slot data comes from the runtime save service and is surfaced here before scene launch."
                : "No save file exists yet. The prototype will create one as soon as the descent begins.";
            _enterButtonLabel.text = snapshot.HasData ? "RESUME" : "BEGIN";
        }

        private void LaunchSelectedSlot()
        {
            CacheServices();
            if (_saveService == null || _gameStateService == null || _slotSnapshots.Count == 0)
            {
                return;
            }

            SlotSnapshot snapshot = _slotSnapshots[_selectedSlotIndex];
            if (snapshot.HasData)
            {
                _gameStateService.LoadGame(snapshot.SlotIndex);
            }
            else
            {
                _gameStateService.BeginNewGame(snapshot.SlotIndex);
            }

            _gameStateService.SetPaused(false);

            string launchScene = Application.CanStreamedLevelBeLoaded(PrototypeSceneName)
                ? PrototypeSceneName
                : LumenfallSceneNames.SilentGate;

            if (Application.CanStreamedLevelBeLoaded(launchScene))
            {
                SceneManager.LoadScene(launchScene, LoadSceneMode.Single);
            }
        }
    }
}
