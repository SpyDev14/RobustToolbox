using System;
using System.Collections.Generic;
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


        public Label() : base()
        {
            _linesCount = new(CalculateLinesCount);
            _drawingSizes = new(CalculateDrawingTextSizes);

            _textToDrawing = new(GetActualTextToDrawing);

            _font.Invalidated += TextSizeChanged;
        }


        // Cache invalidate "events"
        protected override void DisplayedTextChanged()
        {
            base.DisplayedTextChanged();

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
            foreach (var _ in _textToDrawing.Value.Span.EnumerateLines())
                i++;
            return i;
        }

        /// <summary><b>DO NOT READ / WRITE THIS</b></summary>
        /// <remarks>It is for <see cref="CalculateDrawingTextSizes"/> only.</remarks>
        private List<int>? _longLiveLineWidthsList;

        /// <summary><b>DO NOT TOUCH THIS</b></summary>
        /// <remarks>It is for <see cref="CalculateDrawingTextSizes"/> only.</remarks>
        private static readonly IReadOnlyList<int> _emptyLabelLineWidths = [0, ];

        private (int totalHeight, int maxWidth, IReadOnlyList<int> lineWidths)
            CalculateDrawingTextSizes()
        {
            if (_textToDrawing.Value.Length == 0)
                return (0, 0, _emptyLabelLineWidths);

            _longLiveLineWidthsList?.Clear();
            var lineWidths = _longLiveLineWidthsList ??= new (_linesCount.Value);
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
            if (IsMemoryTextMode() && _textTransform.Value != TextTransformMode.None)
                throw new InvalidOperationException(
                    $"You can't use text transform for Label uses {nameof(TextMemory)} " +
                    $"instead {nameof(Text)}."
                );

            if (IsMemoryTextMode())
                return _textMemory;

            string textToDrawing = !string.IsNullOrEmpty(_text)
                ? TextTransformHelpers.ApplyToString(_textTransform.Value, _text)
                : string.Empty;

            return textToDrawing.AsMemory();
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
        public override string? Text
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
                DisplayedTextChanged();
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
                DisplayedTextChanged();
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

                field = _alignModeOverride.HasValue
                    ? _alignModeOverride.Value
                    : StylePropertyDefault(StylePropertyAlignMode, AlignMode.Left);

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

        public TextTransformMode? TextTransformOverride
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

            int totalHeight = _drawingSizes.Value.totalHeight;
            int vOffset = VAlign switch
            {
                VAlignMode.Top => 0,
                VAlignMode.Fill or
                VAlignMode.Center => (PixelSize.Y - totalHeight) / 2,
                VAlignMode.Bottom => PixelSize.Y - totalHeight,
                _ => throw new ArgumentOutOfRangeException(),
            };

            var currentLine = 0;
            var font = _font.Value;
            var actualFontColor = _fontColor.Value;
            var outline = _textOutline.Value;
            var align = Align;
            var ascent = font.GetAscent(UIScale);
            var lineHeight = font.GetLineHeight(UIScale);

            Vector2 calcBaseline()
            {
                var lineWidths = _drawingSizes.Value.lineWidths;
                int hOffset = align switch
                {
                    AlignMode.Left => 0,
                    AlignMode.Center or
                    AlignMode.Fill => (PixelSize.X - lineWidths[currentLine]) / 2,
                    AlignMode.Right => PixelSize.X - lineWidths[currentLine],
                    _ => throw new ArgumentOutOfRangeException(),
                };

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
