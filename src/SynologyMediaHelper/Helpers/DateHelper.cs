using System.Globalization;
using System.Text.RegularExpressions;

namespace SynologyMediaHelper.Helpers;
public class DateHelper
{
    #region Fields-Static
    private const int RegexTimeoutMilliseconds = 500;
    private static readonly Func<string, (bool, DateTime)>[] Parsers;

    private static readonly Regex[] InvalidRegexes;
    private static readonly (Regex, string)[] FormatedRegexes;
    private static readonly Regex[] NumericsRegexes;
    #endregion

    #region Constructors
    static DateHelper()
    {
        InvalidRegexes = new[]
        {
            GetRegex(@"(^FB_IMG_)(\d+)"),
            GetRegex(@"(^received_)(\d+)"),
        };

        FormatedRegexes = new[]
        {
            //DateTime
            (GetRegex(@"(20\d{6})_(\d{6})"), "yyyyMMdd_HHmmss"),
            (GetRegex(@"(20\d{6})-(\d{6})"), "yyyyMMdd-HHmmss"),
            (GetRegex(@"(20\d{2})-(\d{2})-(\d{2})-(\d{2})-(\d{2})-(\d{2})"), "yyyy-MM-dd-HH-mm-ss"),
            (GetRegex(@"(20\d{2})_(\d{2})_(\d{2}) (\d{2})_(\d{2})_(\d{2})"), "yyyy_MM_dd HH_mm_ss"),
            (GetRegex(@"(20\d{2})-(\d{2})-(\d{2})_(\d{2})-(\d{2})-(\d{2})"), "yyyy-MM-dd_HH-mm-ss"),

            //DateTimeLONG
            (GetRegex(@"/\\b(?:Mon|Tue(?:s)?|Wed(?:nes)?|Thu(?:rs)?|Fri|Sat(?:ur)?|Sun)(?:day)?\\b,?\\s+(?:(?:0?[1-9])|(?:[12][0-9])|(?:3[01]))\\s+(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\\s+\\d{4}\\s+(?:(?:[01]?[0-9])|(?:2[0-3]))(?::[0-5]?[0-9]){2}/g"), "DD MMM YYYY HH:MM:SS"),
            (GetRegex(@"/\\b(?:Mon|Tue(?:s)?|Wed(?:nes)?|Thu(?:rs)?|Fri|Sat(?:ur)?|Sun)(?:day)?\\b,?\\s+(?:(?:0?[1-9])|(?:[12][0-9])|(?:3[01]))\\s+(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\\s+\\d{4}\\s+(?:(?:[01]?[0-9])|(?:2[0-3]))(?:-[0-5]?[0-9]){2}/g"), "DD MMM YYYY HH-MM-SS"),
            (GetRegex(@"/\\b(?:Mon|Tue(?:s)?|Wed(?:nes)?|Thu(?:rs)?|Fri|Sat(?:ur)?|Sun)(?:day)?\\b,?\\s+(?:(?:0?[1-9])|(?:[12][0-9])|(?:3[01]))\\s+(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\\s+\\d{4}\\s+(?:(?:[01]?[0-9])|(?:2[0-3]))(?:_[0-5]?[0-9]){2}/g"), "DD MMM YYYY HH_MM_SS"),

            //DateONLY
            (GetRegex(@"(20\d{6})"), "yyyyMMdd"),
            (GetRegex(@"(20\d{2})[-](\d{2})[-](\d{2})"), "yyyy-MM-dd"),
            (GetRegex(@"(20\d{2})[_](\d{2})[_](\d{2})"), "yyyy_MM_dd")
        };

        NumericsRegexes = new Regex[]
        {
            GetRegex(@"\d+")
        };

        Parsers = new[]
        {
            MatchFormatedRegex,
            MatchNumericRegex,
        };
    }
    #endregion

    #region Behavior
    public static bool TryExtractDateTime(string s, out DateTime result)
    {
        result = default;

        if (MatchesInvalidRegex(s)) return false;

        foreach (var parser in Parsers)
        {
            var res = parser(s);
            if (res.Item1 && IsValidDateTime(res.Item2))
            {
                result = res.Item2;
                return true;
            }
        }

        return false;
    }
    public static bool TryExtractMinimumValidDateTime(string s, out DateTime result)
    {
        if (MatchesInvalidRegex(s))
        {
            result = default;
            return false;
        }

        var dates = new List<DateTime>();
        foreach (var parser in Parsers)
        {
            var res = parser(s);
            if (res.Item1 && IsValidDateTime(res.Item2))
                dates.Add(res.Item2);
        }

        if (dates.Any())
        {
            result = dates.Min();
            return true;
        }

        result = default;
        return false;
    }

    public static bool TryParseDateTime(string s, string format, out DateTime result)
    {
        return DateTime.TryParseExact(s, format,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None, out result);
    }
    public static bool IsValidDateTime(DateTime date)
    {
        return date < DateTime.Now && date.Year > 2000;
    }
    private static bool IsValidDateTime(long number)
    {
        try
        {
            if (number >= DateTime.MinValue.Ticks && number <= DateTime.MaxValue.Ticks)
            {
                var date = new DateTime(number);
                if (IsValidDateTime(date))
                    return true;
            }
        }
        catch { }

        try
        {
            var date = DateTime.FromBinary(number);
            if (IsValidDateTime(date))
                return true;
        }
        catch { }

        try
        {
            var date = DateTime.FromFileTimeUtc(number);
            if (IsValidDateTime(date))
                return true;
        }
        catch { }

        return false;
    }

    private static Regex GetRegex(string s)
    {
        return new Regex(s, RegexOptions.Compiled,
            new TimeSpan(0, 0, 0, 0, RegexTimeoutMilliseconds));
    }
    public static bool MatchesInvalidRegex(string s)
    {
        return TryMatchesRegex(s, InvalidRegexes, out var _);
    }
    private static (bool, DateTime) MatchFormatedRegex(string s)
    {
        foreach (var i in FormatedRegexes)
        {
            var match = i.Item1.Match(s);
            if (match.Success && TryParseDateTime(match.Value, i.Item2, out var date))
                return (true, date);
        }

        return (false, default);
    }
    private static (bool, DateTime) MatchNumericRegex(string s)
    {
        while (TryMatchesRegex(s, NumericsRegexes, out Match match))
        {
            if (long.TryParse(match.Value, out var res) && IsValidDateTime(res))
                return (true, new DateTime(res));

            s = s.Replace(match.Value, string.Empty);
        }

        return (false, default);
    }
    private static bool TryMatchesRegex(string s, Regex[] regexes, out Match result)
    {
        foreach (var regex in regexes)
        {
            result = regex.Match(s);
            if (result.Success)
                return true;
        }

        result = Match.Empty;
        return false;
    }
    #endregion
}
