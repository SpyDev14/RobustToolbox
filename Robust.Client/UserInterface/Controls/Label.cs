using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using Robust.Client.Graphics;
using Robust.Shared.Animations;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Robust.Shared.Utility.Caching;
using Robust.Shared.ViewVariables;

namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    ///     A label is a GUI control that displays simple text.
    /// </summary>
    [Virtual]
    public class Label : BaseLabel
    {
        public const string StylePropertyFontColor = "font-color";
        public const string StylePropertyFont = "font";
        public const string StylePropertyFontOutlineThickness = "font-outline-thickness";
        public const string StylePropertyFontOutlineColor = "font-outline-color";
        public const string StylePropertyAlignMode = "alignMode";
        /// <summary>
        /// <see cref="TextTransform"/>
        /// </summary>
        public const string StylePropertyTextTransform = "text-transform";
        // TODO
        public const string StylePropertyTextOverflow = "text-overflow";
        /// <summary>
        /// <see cref="int"/> max lines. 0 means it's disabled.
        /// </summary>
        public const string StylePropertyLineClamp = "line-clamp";
        public const string StylePropertyTextWrap = "text-wrap";
        public const string StylePropertyWordBreak = "word-break";

        enum WordBreakMode
        {
            KeepAll,

        }

        enum TextWrapMode
        {
            NoWrap,
            Normal,
            Balanced,
        }

        enum TextOverflowMode
        {
            Clip,
            Elipsis,
        }


        /// <summary>
        /// Store given text
        /// </summary>
        private string? _text;
        private ReadOnlyMemory<char> _textMemory;
        /// <summary>
        /// Cache for displayed text.
        /// like for transformed text.
        /// Reference equals to text if no text transformations specified.
        /// </summary>
        private readonly Cached<ReadOnlyMemory<char>> _textToDrawing;

        //public string DisplayedText => _textToDrawing.Value;

        private Cached<(int totalHeight, int maxWidth, IReadOnlyList<int> lineWidths)> _drawingSizes;
        private Cached<int> _linesCount;


        // Style properties
        private Cached<Font> _font;
        private Cached<Color> _fontColor;
        private Cached<TextOutline?> _textOutline;
        private Cached<TextTransform> _textTransform;

        // If you add a new property: add them invalidate to
        // StylePropertiesChanged method


        public Label() : base()
        {
            _linesCount = new(CalculateLinesCount);
            _drawingSizes = new(CalculateDrawingTextSizes);

            _textToDrawing = new(GetActualTextToDrawing);

            // Style properties cache
            _font = new(GetActualFont);
            _font.CacheInvalidated += (_, _) => TextSizeChanged();

            _fontColor = new(GetActualFontColor);

            _textOutline = new(GetActualTextOutline);

            _textTransform = new(GetActualTextTransform);
            _textTransform.CacheInvalidated += (_, _) => TextChanged();
        }


        // Cache invalidate "events"
        private void TextChanged()
        {
            _textToDrawing.Invalidate();
            _linesCount.Invalidate();
            TextSizeChanged();
        }

        private void TextSizeChanged(bool invalidateMeasue = true)
        {
            _drawingSizes.Invalidate();

            if (invalidateMeasue) // doesn't need in UIScaleChanged
                InvalidateMeasure();
        }

        protected override void StylePropertiesChanged()
        {
            _alignModeCacheValid = false;
            _font.Invalidate();
            _fontColor.Invalidate();
            _textOutline.Invalidate();
            _textTransform.Invalidate();

            base.StylePropertiesChanged();
        }
        protected internal override void UIScaleChanged()
        {
            TextSizeChanged(invalidateMeasue: false);
            base.UIScaleChanged();
        }


        // Value factories
        private int CalculateLinesCount()
        {
            int i = 0;
            foreach (var _ in _textMemory.Span.EnumerateLines())
                i++;
            return i;
        }

        /// <summary><b>DO NOT READ / WRITE THIS</b></summary>
        /// <remarks>It is for <see cref="CalculateDrawingTextSizes"/> only.</remarks>
        private List<int>? _longLiveLineWidthsContainer;

        /// <summary><b>DO NOT TOUCH THIS</b></summary>
        /// <remarks>It is for <see cref="CalculateDrawingTextSizes"/> only.</remarks>
        private static readonly IReadOnlyList<int> _emptyLabelLineWidths = [0, ];

        private (int totalHeight, int maxWidth, IReadOnlyList<int> lineWidths)
            CalculateDrawingTextSizes()
        {
            if (_textToDrawing.Value.Length == 0)
                return (0, 0, _emptyLabelLineWidths);

            List<int> lineWidths = _longLiveLineWidthsContainer ??= new List<int>(_linesCount.Value);
            var totalHeight = 0;
            var font = _font.Value;

            totalHeight += font.GetHeight(UIScale);
            lineWidths.Add(0);

            foreach (var rune in _textToDrawing.Value.Span.EnumerateRunes())
            {
                if (rune == new Rune('\n'))
                {
                    lineWidths.Add(0);
                    totalHeight += font.GetLineHeight(UIScale);
                }
                else
                {
                    var metrics = font.GetCharMetrics(rune, UIScale);
                    if (metrics == null)
                        continue;

                    lineWidths[^1] += metrics.Value.Advance;
                }
            }

            int maxWidth = lineWidths.Max();
            return (totalHeight, maxWidth, lineWidths);
        }

        private ReadOnlyMemory<char> GetActualTextToDrawing()
        {
            if (IsMemoryTextMode() && _textTransform.Value != TextTransform.None)
                throw new InvalidOperationException(
                    $"You can't use text transform for Label uses {nameof(TextMemory)} " +
                    $"instead {nameof(Text)}."
                );

            string textToDrawing = (
                _text != null
                ? _textTransform.Value.ApplyToString(_text)
                : ""
            );

            return textToDrawing.AsMemory();
        }


        // Style properties value factories
        private Font GetActualFont()
        {
            if (FontOverride != null)
                return FontOverride;

            if (TryGetStyleProperty<Font>(StylePropertyFont, out var font))
                return font;

            return UserInterfaceManager.ThemeDefaults.LabelFont;
        }

        private Color GetActualFontColor()
        {
            if (FontColorOverride.HasValue)
                return FontColorOverride.Value;

            if (TryGetStyleProperty<Color>(StylePropertyFontColor, out var color))
                return color;

            return Color.White;
        }

        private TextOutline? GetActualTextOutline()
        {
            var thickness = OutlineThicknessOverride;
            if (!thickness.HasValue &&
                TryGetStyleProperty<float>(StylePropertyFontOutlineThickness, out var styleThickness))
                thickness = styleThickness;

            var color = OutlineColorOverride;
            if (!color.HasValue &&
                TryGetStyleProperty<Color>(StylePropertyFontOutlineColor, out var styleColor))
                color = styleColor;

            return TextOutline.FromOverrides(thickness, color);
        }

        private TextTransform GetActualTextTransform()
        {
            if (TextTransformOverride.HasValue)
                return TextTransformOverride.Value;

            if (TryGetStyleProperty<TextTransform>(StylePropertyTextTransform, out var value))
                return value;

            return TextTransform.None;
        }


        /// <returns>
        ///     This Label uses <see cref="TextMemory"/>, not <see cref="Text"/>
        /// </returns>
        private bool IsMemoryTextMode() => _text == null && _textMemory.Length > 0;


        /// <summary>
        ///     The text to display.
        /// </summary>
        /// <remarks>
        /// Replaces <see cref="TextMemory"/> when set.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <see cref="TextMemory"/> was set directly and there is no backing string instance to fetch.
        /// </exception>
        [ViewVariables]
        [Animatable]
        public string? Text
        {
            get => _text ?? (
                IsMemoryTextMode()
                ? throw new InvalidOperationException("Label uses TextMemory, cannot fetch string text.")
                : null
            );
            set
            {
                _text = value;
                _textMemory = value.AsMemory();
                TextChanged();
            }
        }

        /// <summary>
        /// The text to display, set as a read-only memory.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Note that updating the backing memory while the control is using it can result in incorrect display due to caching of measure information and similar.
        /// If you modify the backing storage, re-assign the property to invalidate these.
        /// </para>
        /// <para>
        /// Sets <see cref="Text"/> to throw an exception if read, as there is no backing string to retrieve.
        /// </para>
        /// </remarks>
        public ReadOnlyMemory<char> TextMemory
        {
            get => _textMemory;
            set
            {
                _text = null;
                _textMemory = value;
                TextChanged();
            }
        }

        [ViewVariables]
        public bool ClipText
        {
            get => field;
            set
            {
                field = value;
                RectClipContent = value;
                InvalidateMeasure();
            }
        }


        // Is not "override" property and can't be it because uses in too
        // many other places as actual value source and override property both
        private AlignMode? _alignModeOverride;
        private bool _alignModeCacheValid = false;
        [ViewVariables] public AlignMode Align {
            get
            {
                if (_alignModeCacheValid)
                    return field;

                if (_alignModeOverride.HasValue)
                    field = _alignModeOverride.Value;
                else if (TryGetStyleProperty<AlignMode>(StylePropertyAlignMode, out var value))
                    field = value;
                else
                    field = AlignMode.Left;

                _alignModeCacheValid = true;
                return field;
            }

            set
            {
                if (value == field)
                    return;

                _alignModeOverride = field = value;
            }
        }

        [ViewVariables] public VAlignMode VAlign { get; set; }

        public Font? FontOverride
        {
            get => field;
            set
            {
                if (field == value)
                    return;

                field = value;
                _font.Invalidate();
            }
        }

        [Obsolete("Unused")]
        public Color? FontColorShadowOverride { get; set; }


        [ViewVariables(VVAccess.ReadWrite)]
        [Animatable]
        public Color? FontColorOverride
        {
            get => field;
            set
            {
                if (field == value)
                    return;

                field = value;
                _fontColor.Invalidate();
            }
        }

        [Obsolete("Unused")]
        public int? ShadowOffsetXOverride { get; set; }

        [Obsolete("Unused")]
        public int? ShadowOffsetYOverride { get; set; }

        public float? OutlineThicknessOverride
        {
            get => field;
            set
            {
                if (field == value)
                    return;

                field = value;
                _textOutline.Invalidate();
            }
        }

        public Color? OutlineColorOverride
        {
            get => field;
            set
            {
                if (field == value)
                    return;

                field = value;
                _textOutline.Invalidate();
            }
        }

        public TextTransform? TextTransformOverride
        {
            get => field;
            set
            {
                if (value == field)
                    return;

                field = value;
                _textTransform.Invalidate();
            }
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            if (_textToDrawing.Value.Length == 0)
                return;

            int vOffset;
            int totalHeight = _drawingSizes.Value.totalHeight;
            switch (VAlign)
            {
                case VAlignMode.Top:
                    vOffset = 0;
                    break;
                case VAlignMode.Fill:
                case VAlignMode.Center:
                    vOffset = (PixelSize.Y - totalHeight) / 2;
                    break;
                case VAlignMode.Bottom:
                    vOffset = PixelSize.Y - totalHeight;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var currentLine = 0;
            var font = _font.Value;
            var actualFontColor = _fontColor.Value;
            var outline = _textOutline.Value;
            var align = Align;
            var ascent = font.GetAscent(UIScale);
            var lineHeight = font.GetLineHeight(UIScale);

            Vector2 calcBaseline()
            {
                int hOffset = -1;
                var lineWidths = _drawingSizes.Value.lineWidths;
                switch (align)
                {
                    case AlignMode.Left:
                        hOffset = 0;
                        break;
                    case AlignMode.Center:
                    case AlignMode.Fill:
                        hOffset = (PixelSize.X - lineWidths[currentLine]) / 2;
                        break;
                    case AlignMode.Right:
                        hOffset = PixelSize.X - lineWidths[currentLine];
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                Debug.Assert(hOffset != -1);

                return new Vector2(hOffset, ascent + lineHeight * currentLine + vOffset);
            }

            var baseLine = calcBaseline();

            // Outline
            if (outline is { } outlineSettings)
            {
                foreach (var rune in _textToDrawing.Value.Span.EnumerateRunes())
                {
                    if (rune == new Rune('\n'))
                    {
                        currentLine += 1;
                        baseLine = calcBaseline();
                        continue;
                    }

                    var advance = font.DrawCharOutline(handle, rune, baseLine, UIScale, outlineSettings);
                    baseLine.X += advance;
                }

                currentLine = 0;
                baseLine = calcBaseline();
            }

            // Font itself
            foreach (var rune in _textToDrawing.Value.Span.EnumerateRunes())
            {
                if (rune == new Rune('\n'))
                {
                    currentLine += 1;
                    baseLine = calcBaseline();
                    continue;
                }

                var advance = font.DrawChar(handle, rune, baseLine, UIScale, actualFontColor);
                baseLine.X += advance;
            }
        }

        public enum AlignMode : byte
        {
            Left = 0,
            Center = 1,
            Right = 2,
            Fill = 3
        }

        public enum VAlignMode : byte
        {
            Top = 0,
            Center = 1,
            Bottom = 2,
            Fill = 3
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            var totalWidth = _drawingSizes.Value.maxWidth;
            var totalHeight = _drawingSizes.Value.totalHeight;

            if (ClipText)
            {
                return new Vector2(0, totalHeight / UIScale);
            }

            return new Vector2(totalWidth / UIScale, totalHeight / UIScale);
        }
    }
}
