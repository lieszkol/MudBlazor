using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MudBlazor
{
    /// <summary>
    /// FORK ADDITION (2026-10-07): the default converter of <see cref="MudNumericField{T}"/> for decimal / double / float (nullable
    /// or not). It parses "2,5" AND "2.5" alike whatever the field's culture; display (the field's <c>Format</c> and
    /// <c>Culture</c>) and every other type are exactly <see cref="DefaultConverter{T}"/>.
    /// <para>
    /// Why: <see cref="DefaultConverter{T}"/> parses with the field's culture only. Under hu-HU "2.5" does not parse; the converter
    /// then answers default(T), and <c>MudBaseInput.UpdateValuePropertyAsync</c> writes that into the value whatever the error,
    /// so the field silently EMPTIED itself (a decimal? turned null). A "." is easy to type: an English phone keypad
    /// (<c>inputmode="decimal"</c>) or an English numpad gives one, and the field's key filter (<c>[0-9,.\-]</c>) lets it through.
    /// </para>
    /// <para>
    /// The rule (<see cref="Normalize"/>), after dropping every space (also the no-break spaces hu-HU groups digits with):
    /// <list type="bullet">
    /// <item>both "," and "." occur: the LAST one is the decimal point, the others are group separators ("1.234,5", "1,234.5");</item>
    /// <item>only the culture's decimal separator occurs: as typed ("2,5" under hu-HU, "2.5" under en-US);</item>
    /// <item>only the culture's GROUP separator occurs, in valid groups of three ("1,234" under en-US, "1.234.567" under de-DE):
    /// group separators — so a value the field itself displayed with a thousands separator (Format "N0") reads back unchanged
    /// when MudNumericField re-parses its text on blur;</item>
    /// <item>otherwise a single "," or "." is the decimal point ("2.5" under hu-HU, "2,5" under en-US, "1.234" under hu-HU is
    /// 1,234), and one that occurs several times is a group separator.</item>
    /// </list>
    /// The result goes to <see cref="DefaultConverter{T}"/> in the field's own notation, so its error text and every other
    /// behaviour stay as they were. A caller's own <c>Converter</c> parameter still replaces this one.
    /// </para>
    /// </summary>
    public class FlexibleNumberConverter<T> : DefaultConverter<T>
    {
        /// <summary>True for decimal, double and float (nullable or not): the types whose parsing this converter changes.</summary>
        public static readonly bool AppliesToType = IsFloatingType(typeof(T));

        /// <summary>A <see cref="FlexibleNumberConverter{T}"/> for a floating type, else a plain <see cref="DefaultConverter{T}"/>.</summary>
        public static Converter<T> CreateFor() => AppliesToType ? new FlexibleNumberConverter<T>() : new DefaultConverter<T>();

        private static bool IsFloatingType(Type type)
        {
            var underlying = Nullable.GetUnderlyingType(type) ?? type;
            return underlying == typeof(decimal) || underlying == typeof(double) || underlying == typeof(float);
        }

        protected override T ConvertFromString(string value)
        {
            if (!AppliesToType || string.IsNullOrWhiteSpace(value))
                return base.ConvertFromString(value);
            return base.ConvertFromString(Normalize(value, Culture));
        }

        /// <summary>
        /// <paramref name="text"/> rewritten in <paramref name="culture"/>'s notation without group separators ("2.5" → "2,5" under
        /// hu-HU, "1 234,5" → "1234,5", "1,234" → "1234" under en-US). See the class remarks for the rule.
        /// </summary>
        public static string Normalize(string text, CultureInfo culture)
        {
            var format = (culture ?? CultureInfo.CurrentCulture).NumberFormat;
            var s = new string(text.Where(c => !char.IsWhiteSpace(c) && c != ' ' && c != ' ').ToArray());

            string decimalSeparator = format.NumberDecimalSeparator;
            char? cultureDecimal = decimalSeparator.Length == 1 ? decimalSeparator[0] : (char?)null;
            char? cultureGroup = format.NumberGroupSeparator.Length == 1 ? format.NumberGroupSeparator[0] : (char?)null;

            int commas = s.Count(c => c == ','), dots = s.Count(c => c == '.');
            int decimalIndex;
            if (commas > 0 && dots > 0)
            {
                decimalIndex = Math.Max(s.LastIndexOf(','), s.LastIndexOf('.'));
            }
            else if (commas + dots == 0)
            {
                return s;
            }
            else
            {
                char separator = commas > 0 ? ',' : '.';
                int count = commas + dots;
                if (separator == cultureDecimal)
                {
                    if (count > 1)
                        return s;                                             // "1,2,3" under hu-HU: not a number, the base parser refuses it
                    decimalIndex = s.IndexOf(separator);
                }
                else if (separator == cultureGroup && IsValidGrouping(s, separator))
                    decimalIndex = -1;                                        // the culture's own thousands separators
                else
                    decimalIndex = count == 1 ? s.IndexOf(separator) : -1;    // a foreign decimal point, or repeated group separators
            }

            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == ',' || c == '.')
                {
                    if (i == decimalIndex)
                        sb.Append(decimalSeparator);
                    // any other "," / "." is a group separator: dropped
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        /// <summary>"1,234" / "-12,345,678": a 1-3 digit lead (an optional sign before it), then groups of exactly three digits.</summary>
        private static bool IsValidGrouping(string s, char separator)
        {
            var parts = s.Split(separator);
            var lead = parts[0].TrimStart('-', '+');
            if (lead.Length is < 1 or > 3 || !lead.All(char.IsDigit))
                return false;
            for (int i = 1; i < parts.Length; i++)
            {
                if (parts[i].Length != 3 || !parts[i].All(char.IsDigit))
                    return false;
            }
            return true;
        }
    }
}
