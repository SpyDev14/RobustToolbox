using System;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Robust.Shared.ViewVariables;

namespace Robust.Client.UserInterface
{
    // ReSharper disable once RequiredBaseTypesIsNotInherited
    public partial class Control
    {
        public const string StylePropertyModulateSelf = "modulate-self";

        /// <summary>
        ///     Overrides the style sheet used for this control and its descendants.
        /// </summary>
        /// <seealso cref="IUserInterfaceManager.Stylesheet"/>
        public Stylesheet? Stylesheet
        {
            get => _stylesheet;
            set
            {
                if (ReferenceEquals(_stylesheet, value))
                    return;

                _stylesheet = value;
                StylesheetUpdateRecursive();
            }
        }

        [ViewVariables]
        public StyleClassCollection StyleClasses
        {
            get => _styleClasses;
            set
            {
                _styleClasses = new(value);
                _styleClasses._owner = this;
            }
        }

        [ViewVariables(VVAccess.ReadOnly)]
        public IReadOnlyCollection<string> StylePseudoClass => _stylePseudoClass;

        [ViewVariables]
        public string? StyleIdentifier
        {
            get => _styleIdentifier;
            set
            {
                _styleIdentifier = value;
                Restyle();
            }
        }

        private readonly Dictionary<string, object> _styleProperties = new();
        private StyleClassCollection _styleClasses;
        private readonly HashSet<string> _stylePseudoClass = new();

        // Styling needs to be updated.
        private bool _stylingDirty;
        // _actualStylesheetCached needs update.
        private bool _stylesheetUpdateNeeded;
        internal int RestyleGeneration;

        private string? _styleIdentifier;

        private Stylesheet? _actualStylesheetCached;
        private Stylesheet? _stylesheet;

        public bool HasStylePseudoClass(string className)
        {
            return _stylePseudoClass.Contains(className);
        }

        protected void AddStylePseudoClass(string className)
        {
            if (_stylePseudoClass.Contains(className))
            {
                return;
            }

            _stylePseudoClass.Add(className);
            Restyle();
        }

        protected void RemoveStylePseudoClass(string className)
        {
            _stylePseudoClass.Remove(className);
            Restyle();
        }

        protected void SetOnlyStylePseudoClass(string? className)
        {
            _stylePseudoClass.Clear();

            if (className == null)
            {
                Restyle();
                return;
            }

            _stylePseudoClass.Add(className);
            Restyle();
        }
        public bool HasStyleClass(string className)
        {
            return _styleClasses.Contains(className);
        }

        public void AddStyleClass(string className)
        {
            _styleClasses.Add(className);
        }

        public void RemoveStyleClass(string className)
        {
            _styleClasses.Remove(className);
        }

        public void SetOnlyStyleClass(string className)
        {
            _styleClasses.Clear();
            _styleClasses.Add(className);
        }

        /// <summary>
        /// Ensures the control will be restyled, handled automatically through most API calls.
        /// </summary>
        /// <remarks>
        /// You likely don't need to call this yourself.
        /// </remarks>
        public void Restyle()
        {
            if (_stylingDirty)
            {
                // Already queued for a style update, don't bother.
                return;
            }

            _stylingDirty = true;

            UserInterfaceManagerInternal.QueueStyleUpdate(this);
        }

        public void InvalidateStyleSheet()
        {
            _stylesheetUpdateNeeded = true;

            Restyle();
        }

        internal void StylesheetUpdateRecursive()
        {
            InvalidateStyleSheet();

            foreach (var child in Children)
            {
                // Don't descent into children that have a style sheet since those aren't affected.
                if (child.Stylesheet == null)
                {
                    child.StylesheetUpdateRecursive();
                }
            }
        }

        public void DoStyleUpdate()
        {
            _styleProperties.Clear();

            if (_stylesheetUpdateNeeded)
            {
                _actualStylesheetCached = UserInterfaceManager.Stylesheet;

                for (var p = this; p != null; p = p.Parent)
                {
                    if (p.Stylesheet != null)
                    {
                        _actualStylesheetCached = p.Stylesheet;
                        break;
                    }
                }

                _stylesheetUpdateNeeded = false;
            }

            var stylesheet = _actualStylesheetCached;
            if (stylesheet == null)
            {
                return;
            }

            // Get all rules that apply to us, sort them and apply they params again.
            var ruleList = new List<(int index, StyleRule rule)>();

            // Unsorted rules
            foreach (var (index, rule) in stylesheet.UnsortedRules)
            {
                if (rule.Selector.Matches(this))
                {
                    ruleList.Add((index, rule));
                }
            }

            // Rules specific to our type.
            for (var type = GetType(); type != typeof(Control); type = type!.BaseType)
            {
                if (!stylesheet.TypeSortedRules.TryGetValue(type!, out var typeRuleList))
                {
                    continue;
                }

                foreach (var (index, rule) in typeRuleList)
                {
                    if (rule.Selector.Matches(this))
                    {
                        ruleList.Add((index, rule));
                    }
                }
            }

            // Sort by specificity.
            // The index is there to sort by if specificity is the same, in which case the last takes precedence.
            ruleList.Sort((a, b) =>
            {
                var cmp = a.rule.Specificity.CompareTo(b.rule.Specificity);
                // Reverse this sort so that high specificity is at the TOP.
                return -(cmp != 0 ? cmp : a.index.CompareTo(b.index));
            });

            // Go over each rule.
            foreach (var (_, rule) in ruleList)
            {
                foreach (var property in rule.Properties)
                {
                    if (_styleProperties.ContainsKey(property.Name))
                    {
                        // Since we've sorted by priority in reverse,
                        // the first ones to get applied have highest priority.
                        // So if we have a duplicate it's always lower priority and we can discard it.
                        continue;
                    }

                    _styleProperties[property.Name] = property.Value;
                }
            }

            StylePropertiesChanged();

            // Setting this at the end of the function to prevent style updates from ever re-queueing a style update,
            // which would cause an infinite loop.
            _stylingDirty = false;
        }

        protected virtual void StylePropertiesChanged()
        {
            UpdateLayoutStyleProperties();
            InvalidateMeasure();
        }

        public void ForceRunStyleUpdate()
        {
            DoStyleUpdate();

            foreach (var child in Children)
            {
                child.ForceRunStyleUpdate();
            }
        }

        /// <summary>
        /// Override for specify legacy names
        /// </summary>
        /// <remarks>
        /// <b>Example:</b>
        /// <code>
        /// protected override <see cref="LegacyNamesForStyleProperties"/> =&gt _legacyNamesForStyleProperties;
        /// private static readonly IReadOnlyDictionary&ltstring, IReadOnlyList&ltstring&gt&gt _legacyNamesForStyleProperties =
        /// {
        ///     { StylePropertyTextAlignMode, [ "alignMode", ]},
        ///     { StylePropertyBackgroundColor, [ "bg-color", "BgColor" ] },
        /// };
        /// </code>
        /// </remarks>
        /// <returns></returns>
        protected virtual IReadOnlyDictionary<string, IReadOnlyList<string>>? LegacyNamesForStyleProperties => null;

        public bool TryGetStyleProperty<T>(string param, [MaybeNullWhen(false)] out T value)
        {
            if (tryGetProperty(param, out value))
                return true;

            // If has legacy names for this property
            if (LegacyNamesForStyleProperties is { } aliases
                && aliases.TryGetValue(param, out var legacyNames))
            {
                foreach (var legacyName in legacyNames)
                    if (tryGetProperty(legacyName, out value))
                        return true;
            }

            value = default;
            return false;

            bool tryGetProperty(string param, [MaybeNullWhen(false)] out T value)
            {
                if (_styleProperties.TryGetValue(param, out var val) && val is T cast)
                {
                    value = cast;
                    return true;
                }

                value = default;
                return false;
            }
        }

        public T StylePropertyDefault<T>(string param, T defaultValue)
        {
            if (TryGetStyleProperty<T>(param, out var value))
                return value;

            return defaultValue;
        }


        public sealed class StyleClassCollection : ICollection<string>, IReadOnlyCollection<string>
        {
            internal Control? _owner;
            private readonly HashSet<string> _values;

            internal StyleClassCollection(Control control)
            {
                _owner = control;
                _values = new();
            }

            public StyleClassCollection()
            {
                _values = new();
            }

            public StyleClassCollection(IEnumerable<string> items)
            {
                _values = new(items);
            }

            public StyleClassCollection(params string[] items)
            {
                _values = new(items);
            }

            public static StyleClassCollection Parse(string s) => new(s.Split(' '));

            public IEnumerator<string> GetEnumerator()
            {
                return _values.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public void Add(string item)
            {
                _values.Add(item);
                _owner?.Restyle();
            }

            public void Clear()
            {
                _values.Clear();
                _owner?.Restyle();
            }

            public bool Contains(string item)
            {
                return _values.Contains(item);
            }

            public void CopyTo(string[] array, int arrayIndex)
            {
                _values.CopyTo(array, arrayIndex);
            }

            public bool Remove(string item)
            {
                var ret = _values.Remove(item);
                if (ret)
                {
                    _owner?.Restyle();
                }

                return ret;
            }

            int ICollection<string>.Count => _values.Count;
            public bool IsReadOnly => false;
            int IReadOnlyCollection<string>.Count => _values.Count;
        }


        [AttributeUsage(AttributeTargets.Field)]
        protected sealed class StylePropertyLegacyNames(params string[] legacyNames) : Attribute
        {
            public IReadOnlyList<string> LegacyNames = legacyNames;
        }

        private static class StylePropLegacyNamesAliasesCache
        {
            private static readonly ConditionalWeakTable<
                Type, FrozenDictionary<string, IReadOnlyList<string>>
            > _cache = new();

            public static FrozenDictionary<string, IReadOnlyList<string>> Get(
                [DynamicallyAccessedMembers(
                    DynamicallyAccessedMemberTypes.PublicFields |
                    DynamicallyAccessedMemberTypes.NonPublicFields
                )] Type controlType)
            {
                Debug.Assert(controlType.IsSubclassOf(typeof(Control)));
                return _cache.GetOrAdd(controlType, Build);
            }

            private static FrozenDictionary<string, IReadOnlyList<string>> Build(Type controlType)
            {
                Debug.Assert(controlType.IsSubclassOf(typeof(Control)));
                var map = new Dictionary<string, IReadOnlyList<string>>();

                var chain = new Stack<Type>();
                for (var t = controlType; t != typeof(Control).BaseType; t = t.BaseType)
                {
                    Debug.Assert(t is not null);
                    chain.Push(t);
                }

                while (chain.Count > 0)
                {
                    var type = chain.Pop();
                    foreach (var field in type.GetFields(
                        BindingFlags.DeclaredOnly | // not from base type
                        BindingFlags.Static |
                        BindingFlags.Public)) // all StyleProperty constants is public
                    {
                        if (!field.IsLiteral || field.FieldType != typeof(string))
                            continue;

                        var attr = field.GetCustomAttribute<StylePropertyLegacyNames>();
                        if (attr is null)
                            continue;

                        if (field.GetRawConstantValue() is string actualName)
                            map[actualName] = attr.LegacyNames;
                    }
                }

                return map.ToFrozenDictionary();
            }
        }
    }
}
