using MediaInfoLib;
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

        public static string ComposeLine1(CoreStreamInfo info)
        {
            var parts = new List<string>
            {
                info.Default??false ? "[x]" : "[ ]" ,
                info.Forced??false ? "[x]" : "[ ]" ,
            };

            if (info.StreamKind != StreamKind.Text && info.ParentFile.FileSizeBytes > 0 && info.StreamSizeBytes > 0)
            {
                parts.Add($"[{(int)((info.StreamSizeBytes * 100L) / info.ParentFile.FileSizeBytes)}%]");
            }

            if (!string.IsNullOrWhiteSpace(info.Language)) parts.Add(" " + info.Language);
            if (!string.IsNullOrWhiteSpace(info.Title)) parts.Add(" " + info.Title);
            return string.Join("", parts).Trim();
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
            if (string.IsNullOrWhiteSpace(ch)) return string.Empty;

            var s = ch.ToLowerInvariant();

            if (s.Contains("stereo")) return "2.0";
            if (s.Contains("mono")) return "1.0";

            if (string.IsNullOrWhiteSpace(channelPositions)) return ch+".0";

            var cp = channelPositions.ToLowerInvariant();
            bool hasLfe = cp.Contains("lfe") || cp.Contains("low frequency");

            if (int.TryParse(ch, out var n))
            {
                if (hasLfe) return (n - 1) + ".1";
                else return n + ".0";
            }

            return ch;
        }
    }
}
