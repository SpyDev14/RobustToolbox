using Robust.Client.Graphics;
using Robust.Shared.Animations;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Robust.Shared.Utility.Caching;
using Robust.Shared.ViewVariables;
using System;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using TerraFX.Interop.Windows;

namespace Robust.Client.UserInterface.Controls;


#pragma warning disable CS0618
[StylePropertyLegacyNames(StylePropertyAlignMode, "alignMode")]
[StylePropertyLegacyNames(StylePropertyTextOutline, StylePropertyFontOutline)]
[StylePropertyLegacyNames(StylePropertyTextOutlineColor, StylePropertyFontOutlineColor)]
[StylePropertyLegacyNames(StylePropertyTextOutlineThickness, StylePropertyFontOutlineThickness)]
#pragma warning restore
public abstract class BaseLabel : Control
{
    public const string StylePropertyFontColor = "font-color";
    public const string StylePropertyFont = "font";
    public const string StylePropertyTextOutline = "text-outline";
    public const string StylePropertyTextOutlineThickness = "text-outline-thickness";
    public const string StylePropertyTextOutlineColor = "text-outline-color";
    public const string StylePropertyAlignMode = "text-align";
    public const string StylePropertyTextTransform = "text-transform";
    public const string StylePropertyTextOverflow = "text-overflow";
    /// <summary>Maximum of drawed lines. 0 means maximum is disabled.</summary>
    public const string StylePropertyLineClamp = "line-clamp";
    public const string StylePropertyTextWrap = "text-wrap";

    [Obsolete($"Use {nameof(StylePropertyTextOutline)}")]
    public const string StylePropertyFontOutline = "font-outline";

    [Obsolete($"Use {nameof(StylePropertyTextOutlineThickness)}")]
    public const string StylePropertyFontOutlineThickness = "font-outline-thickness";

    [Obsolete($"Use {nameof(StylePropertyTextOutlineColor)}")]
    public const string StylePropertyFontOutlineColor = "font-outline-color";


    protected readonly StylePropertyCache<Font> _font;
    protected readonly StylePropertyCache<Color> _fontColor;
    // Handled manual
    protected readonly Cached<TextOutline?> _textOutline;
    protected readonly StylePropertyCache<TextTransformMode> _textTransform;
    protected readonly StylePropertyCache<int> _lineClamp;
    protected readonly StylePropertyCache<TextOverflowMode> _textOverflow;
    protected readonly StylePropertyCache<TextWrapMode> _textWrap;

    public sealed class StylePropertyCache<T>
    {
        private Cached<T> _actualValue;
        private T? OverrideValue
        {
            get => field;
            set
            {
                _actualValue.Invalidate();
                field = value;
            }
        }

        public event Action? Invalidated;

        public StylePropertyCache(Control control, string propertyName, T defaultValue)
        {
            _actualValue = new(() => OverrideValue is null
                ? control.StylePropertyDefault(propertyName, defaultValue)
                : OverrideValue
            );

            _actualValue.Invalidated += () => { Invalidated?.Invoke(); };
        }

        public T Value => _actualValue.Value;
        public void Invalidate() => _actualValue.Invalidate();
        public void SetOverride(T value) => OverrideValue = value;
        public void ClearOverride() => OverrideValue = default;
    }

    private StylePropertyCache<AlignMode> _alignMode;

    // Manual cache
    private AlignMode? _alignModeOverride;
    private bool _alignModeCacheValid = false;


    [ViewVariables(VVAccess.ReadWrite)]
    public abstract string? Text { get; set; }

    public BaseLabel()
    {
        VerticalAlignment = VAlignment.Center;

        // Style properties cache
        // @formatter:off
        _font          = new(this, StylePropertyFont, UserInterfaceManager.ThemeDefaults.LabelFont);
        _fontColor     = new(this, StylePropertyFontColor,     Color.White);
        _textTransform = new(this, StylePropertyTextTransform, TextTransformMode.None);
        _textOverflow  = new(this, StylePropertyTextOverflow,  TextOverflowMode.Elipsis);
        _lineClamp     = new(this, StylePropertyLineClamp,     0);
        _textWrap      = new(this, StylePropertyTextWrap,      TextWrapMode.Simple);
        _alignMode     = new(this, StylePropertyAlignMode,     AlignMode.Left);

        _textTransform.Invalidated += DisplayedTextChanged;
        _lineClamp.Invalidated     += DisplayedTextChanged;
        _textWrap.Invalidated      += DisplayedTextChanged;
        _font.Invalidated          += TextSizeChanged;
        // @formatter:on

        // Handled manual
        _textOutline = new(getActualTextOutline);

        TextOutline? getActualTextOutline()
        {
            TextOutline? @base = null;
            if (TryGetStyleProperty<TextOutline>(StylePropertyTextOutline, out var outline))
                @base = outline;

            Color? colorFromStyleProp = StylePropertyDefault<Color?>(
                StylePropertyTextOutlineColor,
                @base.HasValue ? @base.Value.Color : null
            );

            float? thicknessFromStyleProp = StylePropertyDefault<float?>(
                StylePropertyTextOutlineThickness,
                @base.HasValue ? @base.Value.Thickness : null
            );

            return TextOutline.FromOverrides(
                OutlineThicknessOverride.HasValue
                    ? OutlineThicknessOverride.Value
                    : thicknessFromStyleProp,
                OutlineColorOverride.HasValue
                    ? OutlineColorOverride.Value
                    : colorFromStyleProp
            );
        }

        /*
        _font = new(getActualFont);
        _font.Invalidated += TextSizeChanged;

        _fontColor = new(getActualFontColor);


        _textTransform = new(getActualTextTransform);
        _textTransform.Invalidated += TextChanged;

        _textOverflow = new(getActualTextOverflow);

        _lineClamp = new(getActualLineClamp);

        _textWrap = new(getActualTextWrap);


        // Value factories, needs here only (incapsulated here)
        Font getActualFont() => FontOverride ?? StylePropertyDefault(
            StylePropertyFont, UserInterfaceManager.ThemeDefaults.LabelFont);

        Color getActualFontColor() => FontColorOverride.HasValue
            ? FontColorOverride.Value
            : StylePropertyDefault(StylePropertyFontColor, Color.White);

        TextTransformMode getActualTextTransform() => TextTransformOverride.HasValue
            ? TextTransformOverride.Value
            : StylePropertyDefault(StylePropertyTextTransform, TextTransformMode.None);

        TextOverflowMode getActualTextOverflow() =>
            StylePropertyDefault(StylePropertyTextOverflow, TextOverflowMode.Elipsis);

        int getActualLineClamp() =>
            StylePropertyDefault(StylePropertyLineClamp, 0);

        TextWrapMode getActualTextWrap() =>
            StylePropertyDefault(StylePropertyTextWrap, TextWrapMode.Simple);
        */
    }


    /// <summary>
    /// Something with affect to drawed text size was changed
    /// </summary>
    protected virtual void TextSizeChanged()
    {

    }

    /// <summary>
    /// Something with affect to drawed text (string) to drawing was changed
    /// (<see cref="Text"/> or <see cref="_textTransform"/> was changed, for example)
    /// </summary>
    /// <remarks>
    /// Call when change <see cref="Text"/>.
    /// </remarks>
    protected virtual void DisplayedTextChanged()
    {

    }

    // Style properties value factories


    // Style property overrides for control
    // Naming idea:
    // - Where setting by styles is first - prop-name + Override suffix.
    //   Font for example.
    // - Where setting by XAML is first, from styles is option - prop-name directly.
    //   VAlign for example.
    // But if i can, i will make everuthing without this fucking "Override" suffix,
    // because its not comfy for use.

    [ViewVariables(VVAccess.ReadWrite)]
    public AlignMode Align
    {
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
            _alignModeOverride = field = value;
            // already actual    ^^^^^
        }
    }

    [ViewVariables(VVAccess.ReadWrite)]
    public VAlignMode VAlign { get; set; }

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


    [ViewVariables(VVAccess.ReadWrite), Animatable]
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

    public enum AlignMode : byte
    {
        Left = 0,
        Center = 1,
        Right = 2,
        Fill = 3,
    }

    public enum VAlignMode : byte
    {
        Top = 0,
        Center = 1,
        Bottom = 2,
        Fill = 3
    }

    /// <summary>
    /// How words will breaking on text-wrap
    /// </summary>
    public enum WordBreakMode : byte
    {
        /// <summary>
        /// Any not word or digin char.
        /// </summary>
        /// <remarks>
        /// Example (1234 semi-auto, max: 10):
        /// <code>
        /// 1234 semi-
        /// auto
        /// </code>
        /// </remarks>
        Normal,

        /// <summary>
        /// Only \s (spacings)
        /// </summary>
        /// <remarks>
        /// Example (max: 10):
        /// <code>
        /// 1234
        /// semi-auto
        /// and some
        /// other
        /// text, like
        /// a big
        /// number:
        /// 1234562843 ◄ break in word is fallback for avoid rect clipping
        /// 422345         
        /// </code>
        /// </remarks>
        KeepAll,

        /// <summary>
        /// Word can be breaced at any position. Useful for long
        /// not-word character sequences, like a numbers.
        /// </summary>
        /// <remarks>
        /// Example (max: 10):
        /// <code>
        /// 1234 semi-
        /// auto and s
        /// ome other
        /// text, like
        /// a big numb
        /// er: 123456
        /// 2843422345
        /// </code>
        /// </remarks>
        BreakAll,
    }

    public enum TextWrapMode : byte
    {
        /// <summary>
        /// No text wrapping
        /// </summary>
        NoWrap,

        /// <summary>
        /// Wrap word if no have enougth space
        /// </summary>
        Simple,

        /// <summary>
        /// Calculate optimal wrapping for balance lines width.
        /// </summary>
        Balanced,
    }

    /// <summary>
    /// What to do on text overflow, like if wrapping is
    /// disabled, or <see cref="StylePropertyLineClamp"/> is set.
    /// </summary>
    public enum TextOverflowMode : byte
    {
        /// <summary>
        /// Just clip rect
        /// </summary>
        Clip,

        /// <summary>
        /// Replace last 3 runes by "..."
        /// </summary>
        Elipsis,
    }

    /// <summary>
    /// 
    /// </summary>
    public enum TextTransformMode : byte
    {
        /// <summary>
        /// No transorm.
        /// </summary>
        None = 0,

        /// <summary>
        /// Uppercase each letter: Hello World => HELLO WORLD;
        /// </summary>
        Upper = 1,

        /// <summary>
        /// Lowercase each letter: Hello World => hello world;
        /// </summary>
        Lower = 2,

        /// <summary>
        /// Uppercase the first character of each word, leave the rest as-is.
        /// Call <see cref="TextTransformModeUtils.ToSentenceCase"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Example:
        /// <code>
        /// 'hello' w0R-LD,ALL can't be rIgHt => Hello W0RLD,ALL-RIgHt;
        /// </code>
        /// </para>
        /// </remarks>
        Title = 3,

        /// <summary>
        /// Uppercase the first char of each word, lowercase the rest.
        /// Call <see cref="TextTransformModeUtils.ToSentenceCase"/>
        /// with <see cref="TextTransformModeUtils.Opts.Strict"/> = <see langword="true"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Example:
        /// <code>
        /// hello w0RLD,ALL-rIgHt => Hello W0rld,All-Right;
        /// </code>
        /// </para>
        /// </remarks>
        TitleStrict = 4,

        /// <summary>
        /// Uppercase the first character of each word, leave the rest as-is.
        /// Call <see cref="TextTransformModeUtils.ToSentenceCase"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Example:
        /// <code>
        /// hello w0RLD,ALL-rIgHt => Hello w0RLD,ALL-rIgHt;
        /// </code>
        /// </para>
        /// </remarks>
        Sentence = 5,


        /// <summary>
        /// First letter of string get upper, others get lower: hello woRLD,ALL rIgHt => Hello world,all right;
        /// Call <see cref="TextTransformModeUtils.ToSentenceCase"/>
        /// with <see cref="TextTransformModeUtils.Opts.Strict"/> = <see langword="true"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Example:
        /// <code>
        /// hello w0RLD,ALL-rIgHt => Hello w0rld,all-right
        /// </code>
        /// </para>
        /// </remarks>
        SentenceStrict = 6,
    }

    protected static class TextTransformHelpers
    {
        /// <summary>
        /// Return transformed string
        /// </summary>
        public static string ApplyToString(TextTransformMode mode, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            return mode switch
            {
                TextTransformMode.None => text,
                TextTransformMode.Upper => text.ToUpper(),
                TextTransformMode.Lower => text.ToLower(),
                TextTransformMode.Sentence => text.ToSentenceCase(),
                TextTransformMode.SentenceStrict => text.ToSentenceCase(new() { Strict = true }),
                TextTransformMode.Title => text.ToTitleCase(),
                TextTransformMode.TitleStrict => text.ToTitleCase(new () { Strict = true }),
                _ => throw new ArgumentOutOfRangeException(nameof(mode))
            };
        }
    }
}
