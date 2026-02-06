using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace ReleaseChecker
{
    public static class StringFormatters
    {
        public static string FlagBox(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "[ ]";
            var v = value.Trim().ToLowerInvariant();
            if (v == "yes" || v == "y" || v == "1" || v == "true" || v == "да" || v == "дa") return "[x]";
            return "[ ]";
        }

        public static string BuildFlagsLangTitle(string def, string forced, string lang, string title, long streamBytes = 0, long fileBytes = 0)
        {
            var flagDef = FlagBox(def);
            var flagForced = FlagBox(forced);
            // combine flags and percent without space between them
            var flagPart = flagDef + flagForced;
            if (fileBytes > 0 && streamBytes > 0)
            {
                try
                {
                    var pct = (int)((streamBytes * 100L) / fileBytes);
                    flagPart = flagPart + $"[{pct}%]";
                }
                catch { }
            }
            var parts = new System.Collections.Generic.List<string> { flagPart };
            var langTitle = string.Join(" ", new[] { lang, title }.Where(s => !string.IsNullOrWhiteSpace(s)));
            if (!string.IsNullOrWhiteSpace(langTitle)) parts.Add(langTitle);
            return string.Join(" ", parts).Trim();
        }

        public static string NormalizeFrameRate(string fr)
        {
            if (string.IsNullOrWhiteSpace(fr)) return string.Empty;
            var s = fr.Trim();
            // Extract first numeric value (like 23.976) and ignore parenthesized ratios
            var m = Regex.Match(s, "[0-9]+(?:\\.[0-9]+)?");
            if (m.Success)
            {
                if (double.TryParse(m.Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var d))
                {
                    // format with up to 3 decimal places, trim trailing zeros
                    var outStr = d.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
                    return outStr + " fps";
                }
            }
            // fallback: return original trimmed string
            return s;
        }

        public static string NormalizeBitrate(string br)
        {
            if (string.IsNullOrWhiteSpace(br)) return string.Empty;
            var s = br.Trim();
            s = s.Replace("Mb/s", "Mbps");
            s = s.Replace("Mb/s", "Mbps");
            s = s.Replace("kb/s", "kbps");
            s = s.Replace("Kb/s", "kbps");
            s = s.Replace("kbit/s", "kbps");
            s = s.Replace("Mb/s", "Mbps");
            return s;
        }

        public static string NormalizeChannels(string ch, string channelPositions)
        {
            if (string.IsNullOrWhiteSpace(ch) && string.IsNullOrWhiteSpace(channelPositions)) return string.Empty;

            // check if channelPositions contains LFE -> then this stream has a .1 LFE channel
            bool hasLfe = false;
            if (!string.IsNullOrWhiteSpace(channelPositions))
            {
                var cp = channelPositions.ToLowerInvariant();
                if (cp.Contains("lfe") || cp.Contains("low frequency")) hasLfe = true;
            }

            var s = (ch ?? string.Empty).Trim().ToLowerInvariant();
            // already like 5.1 or 2.0
            var mDot = Regex.Match(s, "\\d+\\.\\d+");
            if (mDot.Success) return mDot.Value;

            // common words
            if (s.Contains("stereo"))
            {
                return hasLfe ? "1.1" : "2.0";
            }
            if (s.Contains("mono")) return "1.0";

            // extract number of channels
            var m = Regex.Match(s, "\\d+");
            if (m.Success)
            {
                if (int.TryParse(m.Value, out var n))
                {
                    if (hasLfe && n > 0)
                    {
                        // subtract LFE channel for the left.right count
                        var main = n - 1;
                        if (main < 1) main = 1;
                        return main + ".1";
                    }
                    else
                    {
                        return n + ".0";
                    }
                }
            }
            return ch ?? string.Empty;
        }
    }
}
