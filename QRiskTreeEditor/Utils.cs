using QRiskTree.Engine;
using System.Globalization;
using System.Text.RegularExpressions;

namespace QRiskTreeEditor
{
    internal static class Utils
    {
        internal static string AddSpacesToCamelCase(this string input)
        {
            return Regex.Replace(input, "(?<!^)([A-Z])", " $1");
        }

        internal static string GetFormat(this QRiskTree.Engine.Range range)
        {
            string result;

            switch (range.RangeType)
            {
                case RangeType.Money:
                    result = "C0";
                    break;
                case RangeType.Frequency:
                    result = "F2";
                    break;
                case RangeType.Percentage:
                    result = "P2";
                    break;
                default:
                    result = "F2";
                    break;
            }

            return result;
        }

        internal static string GetMin(this QRiskTree.Engine.Range range)
        {
            return range.Min.ToString(range.GetFormat());
        }

        internal static string GetMode(this QRiskTree.Engine.Range range)
        {
            return range.Mode.ToString(range.GetFormat());
        }

        internal static string GetMax(this QRiskTree.Engine.Range range)
        {
            return range.Max.ToString(range.GetFormat());
        }

        internal static bool TryChangeValue(this string oldValue, string newValue, out double calculated)
        {
            bool result = false;
            calculated = 0.0;

            if (string.CompareOrdinal(oldValue, newValue) != 0)
            {
                if (newValue.Contains('%'))
                {
                    var number = newValue.Replace("%", "").Trim();
                    if (double.TryParse(number, out var percentage))
                    {
                        calculated = percentage / 100.0;
                        result = true;
                    }
                }
                else if (newValue.Contains(CultureInfo.CurrentCulture.NumberFormat.CurrencySymbol))
                {
                    var number = newValue.Replace(CultureInfo.CurrentCulture.NumberFormat.CurrencySymbol, "").Trim();
                    if (double.TryParse(number, out calculated))
                    {
                        result = true;
                    }
                }
                else if (double.TryParse(newValue, out calculated))
                {
                    result = true;
                }
            }

            return result;
        }

    }
}
