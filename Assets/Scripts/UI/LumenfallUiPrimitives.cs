using System.Text;
using Lumenfall.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Lumenfall.UI
{
    public static class LumenfallUiPalette
    {
        public static readonly Color Void = new(0.03f, 0.05f, 0.08f, 1f);
        public static readonly Color DeepNight = new(0.06f, 0.09f, 0.15f, 1f);
        public static readonly Color Panel = new(0.08f, 0.12f, 0.18f, 0.92f);
        public static readonly Color PanelSoft = new(0.12f, 0.16f, 0.22f, 0.74f);
        public static readonly Color PanelGlass = new(0.08f, 0.16f, 0.2f, 0.46f);
        public static readonly Color Border = new(0.32f, 0.54f, 0.66f, 0.55f);
        public static readonly Color BorderStrong = new(0.58f, 0.84f, 0.92f, 0.9f);
        public static readonly Color Accent = new(0.39f, 0.93f, 0.98f, 1f);
        public static readonly Color AccentSoft = new(0.4f, 0.86f, 0.96f, 0.34f);
        public static readonly Color Ember = new(1f, 0.73f, 0.39f, 1f);
        public static readonly Color EmberSoft = new(0.95f, 0.62f, 0.28f, 0.24f);
        public static readonly Color TextPrimary = new(0.95f, 0.98f, 1f, 1f);
        public static readonly Color TextMuted = new(0.67f, 0.76f, 0.86f, 1f);
        public static readonly Color TextDim = new(0.52f, 0.61f, 0.69f, 1f);
        public static readonly Color Danger = new(0.94f, 0.38f, 0.43f, 1f);
        public static readonly Color Success = new(0.47f, 0.9f, 0.69f, 1f);
    }

    public sealed class CanvasDriftMotion : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Vector2 _origin;
        private Vector2 _amplitude = new(20f, 12f);
        private float _speed = 0.35f;
        private float _phase;

        public void Configure(Vector2 amplitude, float speed, float phase)
        {
            _amplitude = amplitude;
            _speed = speed;
            _phase = phase;
        }

        private void Awake()
        {
            _rectTransform = transform as RectTransform;
            if (_rectTransform != null)
            {
                _origin = _rectTransform.anchoredPosition;
            }
        }

        private void Update()
        {
            if (_rectTransform == null)
            {
                return;
            }

            float time = Time.unscaledTime * _speed + _phase;
            _rectTransform.anchoredPosition = _origin + new Vector2(Mathf.Sin(time), Mathf.Cos(time * 0.72f)) * _amplitude;
        }
    }

    public static class LumenfallUiText
    {
        public static string HumanizeToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Uncharted";
            }

            StringBuilder builder = new(value.Length + 4);
            char previous = '\0';
            foreach (char character in value)
            {
                if (character == '_' || character == '-')
                {
                    if (builder.Length > 0 && builder[builder.Length - 1] != ' ')
                    {
                        builder.Append(' ');
                    }

                    previous = character;
                    continue;
                }

                if (builder.Length == 0)
                {
                    builder.Append(char.ToUpperInvariant(character));
                }
                else if (char.IsUpper(character) && previous != ' ' && !char.IsUpper(previous))
                {
                    builder.Append(' ');
                    builder.Append(character);
                }
                else
                {
                    builder.Append(character);
                }

                previous = character;
            }

            return builder.ToString();
        }

        public static string AbilityLabel(AbilityType abilityType)
        {
            return abilityType switch
            {
                AbilityType.DashCore => "Dash",
                AbilityType.WallClimb => "Climb",
                AbilityType.LumenPulse => "Pulse",
                AbilityType.PhaseShift => "Phase",
                AbilityType.CrystalGrapple => "Grapple",
                _ => HumanizeToken(abilityType.ToString())
            };
        }

        public static string ContextActionLabel(ContextActionType actionType)
        {
            return actionType switch
            {
                ContextActionType.Grapple => "GRAPPLE",
                ContextActionType.PhaseWall => "PHASE",
                ContextActionType.Interact => "INTERACT",
                ContextActionType.Pulse => "PULSE",
                _ => "CONTEXT"
            };
        }
    }

    public static class LumenfallUiFactory
    {
        private static Font _font;
        private static Sprite _whiteSprite;

        public static Font DefaultFont => _font ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static Sprite WhiteSprite
        {
            get
            {
                if (_whiteSprite == null)
                {
                    _whiteSprite = Sprite.Create(
                        Texture2D.whiteTexture,
                        new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                        new Vector2(0.5f, 0.5f));
                }

                return _whiteSprite;
            }
        }

        public static Canvas CreateCanvas(string name, Transform parent)
        {
            GameObject canvasObject = new(name);
            canvasObject.transform.SetParent(parent, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static GameObject CreateStretchObject(string name, Transform parent, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject gameObject = new(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            return gameObject;
        }

        public static GameObject CreateAnchoredObject(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 sizeDelta,
            Vector2 anchoredPosition,
            Vector2 pivot)
        {
            GameObject gameObject = new(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.sizeDelta = sizeDelta;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.pivot = pivot;
            return gameObject;
        }

        public static Image AddImage(GameObject target, Color color, bool raycastTarget = false)
        {
            Image image = target.AddComponent<Image>();
            image.sprite = WhiteSprite;
            image.color = color;
            image.raycastTarget = raycastTarget;
            return image;
        }

        public static Text CreateText(
            string name,
            Transform parent,
            string content,
            int fontSize,
            Color color,
            TextAnchor alignment,
            FontStyle fontStyle = FontStyle.Normal)
        {
            GameObject textObject = new(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            Text text = textObject.AddComponent<Text>();
            text.font = DefaultFont;
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.fontStyle = fontStyle;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        public static Button CreateButton(string name, Transform parent, Color baseColor, Color highlightedColor)
        {
            GameObject buttonObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.GetComponent<Image>();
            image.sprite = WhiteSprite;
            image.color = baseColor;

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = baseColor;
            colors.highlightedColor = highlightedColor;
            colors.pressedColor = Color.Lerp(baseColor, highlightedColor, 0.72f);
            colors.selectedColor = highlightedColor;
            colors.disabledColor = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * 0.45f);
            button.colors = colors;
            button.targetGraphic = image;
            return button;
        }

        public static void AddOutline(Graphic graphic, Color color, Vector2 distance)
        {
            Outline outline = graphic.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
        }

        public static void AddShadow(Graphic graphic, Color color, Vector2 distance)
        {
            Shadow shadow = graphic.gameObject.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
        }

        public static GameObject CreateOrb(
            string name,
            Transform parent,
            Vector2 anchor,
            Vector2 size,
            Vector2 anchoredPosition,
            Color color,
            Vector2 driftAmplitude,
            float driftSpeed,
            float phase)
        {
            GameObject orbObject = CreateAnchoredObject(name, parent, anchor, anchor, size, anchoredPosition, anchor);
            Image image = AddImage(orbObject, color);
            image.maskable = false;

            CanvasDriftMotion driftMotion = orbObject.AddComponent<CanvasDriftMotion>();
            driftMotion.Configure(driftAmplitude, driftSpeed, phase);
            return orbObject;
        }
    }
}
