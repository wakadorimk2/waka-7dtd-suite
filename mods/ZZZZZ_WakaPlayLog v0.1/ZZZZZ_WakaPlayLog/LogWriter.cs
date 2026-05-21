using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace WakaPlayLog
{
    /// <summary>
    /// NDJSON session log writer. Files land in:
    ///   %APPDATA%\7DaysToDie\WakaLog\session_YYYY-MM-DD_HHMM.ndjson
    ///
    /// One JSON object per line. Buffered append; explicit flush only at
    /// session_end. If the game crashes mid-session, the last few events
    /// may be lost and session_end will be missing — chill-assistant treats
    /// a tail without session_end as "crash exit".
    /// </summary>
    public static class LogWriter
    {
        static readonly object _lock = new object();
        static StreamWriter _writer;
        static string _currentPath;
        static bool _sessionActive;
        static bool _disabled;

        public static bool IsActive { get { lock (_lock) return _sessionActive; } }
        public static string CurrentPath { get { lock (_lock) return _currentPath; } }

        public static void StartSession()
        {
            lock (_lock)
            {
                if (_disabled) return;
                if (_sessionActive) return;
                try
                {
                    var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    var dir = Path.Combine(appdata, "7DaysToDie", "WakaLog");
                    Directory.CreateDirectory(dir);

                    var stamp = DateTime.Now.ToString("yyyy-MM-dd_HHmm", CultureInfo.InvariantCulture);
                    _currentPath = Path.Combine(dir, $"session_{stamp}.ndjson");

                    var fs = new FileStream(_currentPath, FileMode.Append, FileAccess.Write, FileShare.Read);
                    _writer = new StreamWriter(fs, new UTF8Encoding(false));
                    _writer.NewLine = "\n";
                    _sessionActive = true;
                    Log.Out($"[WakaPlayLog] Session log opened: {_currentPath}");
                }
                catch (Exception e)
                {
                    Log.Error($"[WakaPlayLog] Failed to open session log: {e}");
                    _disabled = true;
                    SafeCloseLocked();
                }
            }
        }

        public static void EndSession()
        {
            lock (_lock)
            {
                if (!_sessionActive) return;
                try
                {
                    _writer?.Flush();
                }
                catch (Exception e)
                {
                    Log.Warning($"[WakaPlayLog] Flush on end failed: {e.Message}");
                }
                SafeCloseLocked();
                Log.Out("[WakaPlayLog] Session log closed");
            }
        }

        public static void Write(string cat, string evt, string sev, IDictionary<string, object> data)
        {
            lock (_lock)
            {
                if (!_sessionActive || _writer == null) return;
                try
                {
                    var line = BuildLine(cat, evt, sev, data);
                    _writer.WriteLine(line);
                }
                catch (Exception e)
                {
                    Log.Warning($"[WakaPlayLog] Write failed: {e.Message}");
                }
            }
        }

        public static void Flush()
        {
            lock (_lock)
            {
                if (!_sessionActive || _writer == null) return;
                try { _writer.Flush(); } catch { }
            }
        }

        static void SafeCloseLocked()
        {
            try { _writer?.Dispose(); } catch { }
            _writer = null;
            _sessionActive = false;
        }

        // ---------- JSON serialization ----------

        static string BuildLine(string cat, string evt, string sev, IDictionary<string, object> data)
        {
            var sb = new StringBuilder(256);
            sb.Append('{');
            AppendKV(sb, "ts", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)); sb.Append(',');
            AppendKV(sb, "game_time", GameTime.Format()); sb.Append(',');
            AppendKV(sb, "cat", cat); sb.Append(',');
            AppendKV(sb, "event", evt); sb.Append(',');
            AppendKV(sb, "sev", sev); sb.Append(',');
            sb.Append("\"data\":");
            AppendValue(sb, data ?? new Dictionary<string, object>());
            sb.Append('}');
            return sb.ToString();
        }

        static void AppendKV(StringBuilder sb, string key, object value)
        {
            AppendString(sb, key);
            sb.Append(':');
            AppendValue(sb, value);
        }

        static void AppendValue(StringBuilder sb, object value)
        {
            if (value == null) { sb.Append("null"); return; }
            switch (value)
            {
                case string s: AppendString(sb, s); return;
                case bool b: sb.Append(b ? "true" : "false"); return;
                case int i: sb.Append(i.ToString(CultureInfo.InvariantCulture)); return;
                case long l: sb.Append(l.ToString(CultureInfo.InvariantCulture)); return;
                case float f:
                    if (float.IsNaN(f) || float.IsInfinity(f)) { sb.Append("null"); return; }
                    sb.Append(f.ToString("0.###", CultureInfo.InvariantCulture)); return;
                case double d:
                    if (double.IsNaN(d) || double.IsInfinity(d)) { sb.Append("null"); return; }
                    sb.Append(d.ToString("0.###", CultureInfo.InvariantCulture)); return;
                case IDictionary dict: AppendDict(sb, dict); return;
                case IEnumerable ie when !(value is string): AppendArray(sb, ie); return;
                default: AppendString(sb, value.ToString()); return;
            }
        }

        static void AppendDict(StringBuilder sb, IDictionary dict)
        {
            sb.Append('{');
            bool first = true;
            foreach (DictionaryEntry e in dict)
            {
                if (!first) sb.Append(',');
                first = false;
                AppendString(sb, e.Key?.ToString() ?? "");
                sb.Append(':');
                AppendValue(sb, e.Value);
            }
            sb.Append('}');
        }

        static void AppendArray(StringBuilder sb, IEnumerable items)
        {
            sb.Append('[');
            bool first = true;
            foreach (var item in items)
            {
                if (!first) sb.Append(',');
                first = false;
                AppendValue(sb, item);
            }
            sb.Append(']');
        }

        static void AppendString(StringBuilder sb, string s)
        {
            sb.Append('"');
            if (string.IsNullOrEmpty(s)) { sb.Append('"'); return; }
            foreach (char c in s)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (c < 0x20)
                            sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else
                            sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }
    }
}
