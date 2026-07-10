using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace BD2SkillTextExporter
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class SkillTextExporterPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "local.bd2.skilltextexporter";
        public const string PluginName = "BD2 Skill Text Exporter";
        public const string PluginVersion = "0.2.0";

        internal static SkillTextExporterPlugin? Instance { get; private set; }

        private readonly object _sync = new object();
        private readonly Dictionary<string, CapturedEntry> _entries =
            new Dictionary<string, CapturedEntry>(StringComparer.Ordinal);

        private Harmony? _harmony;
        private ConfigEntry<KeyboardShortcut>? _toggleCaptureKey;
        private ConfigEntry<KeyboardShortcut>? _exportKey;
        private ConfigEntry<KeyboardShortcut>? _clearKey;
        private ConfigEntry<bool>? _captureOnStart;
        private ConfigEntry<bool>? _autoExportWhenStopping;
        private ConfigEntry<bool>? _captureCaller;
        private ConfigEntry<int>? _minimumTextLength;

        private bool _captureEnabled;
        private string _outputRoot = string.Empty;
        private int _patchedMethodCount;
        private int _captureEventCount;

        private void Awake()
        {
            Instance = this;

            _toggleCaptureKey = Config.Bind(
                "Hotkeys",
                "Toggle capture",
                new KeyboardShortcut(KeyCode.F9),
                "F9: start or stop collecting text. Start it only while opening character skill/costume screens.");

            _exportKey = Config.Bind(
                "Hotkeys",
                "Export",
                new KeyboardShortcut(KeyCode.F10),
                "F10: export the current capture to JSON files.");

            _clearKey = Config.Bind(
                "Hotkeys",
                "Clear",
                new KeyboardShortcut(KeyCode.F11),
                "F11: clear the current in-memory capture.");

            _captureOnStart = Config.Bind(
                "Capture",
                "Capture on startup",
                false,
                "Keep disabled for normal use. F9 is safer because it limits collection to the screens you open.");

            _autoExportWhenStopping = Config.Bind(
                "Capture",
                "Auto export when capture stops",
                true,
                "Automatically writes JSON when F9 changes capture from ON to OFF.");

            _captureCaller = Config.Bind(
                "Diagnostics",
                "Capture caller stack",
                false,
                "Adds a caller hint but costs more CPU. Enable only when diagnosing missing or mixed text.");

            _minimumTextLength = Config.Bind(
                "Capture",
                "Minimum text length",
                1,
                "Ignore returned strings shorter than this value.");

            _captureEnabled = _captureOnStart.Value;
            _outputRoot = Path.Combine(Paths.BepInExRootPath, "exports", "BD2SkillText");

            Directory.CreateDirectory(_outputRoot);

            _harmony = new Harmony(PluginGuid);
            PatchCandidateResolvers();

            Logger.LogMessage(
                PluginName + " loaded. Patched methods: " + _patchedMethodCount +
                ". F9=capture, F10=export, F11=clear.");
            Logger.LogMessage("Output folder: " + _outputRoot);
        }

        private void Update()
        {
            if (_toggleCaptureKey != null && _toggleCaptureKey.Value.IsDown())
            {
                _captureEnabled = !_captureEnabled;
                Logger.LogMessage("Skill text capture: " + (_captureEnabled ? "ON" : "OFF"));

                if (!_captureEnabled &&
                    _autoExportWhenStopping != null &&
                    _autoExportWhenStopping.Value)
                {
                    ExportSnapshot();
                }
            }

            if (_exportKey != null && _exportKey.Value.IsDown())
            {
                ExportSnapshot();
            }

            if (_clearKey != null && _clearKey.Value.IsDown())
            {
                lock (_sync)
                {
                    _entries.Clear();
                    _captureEventCount = 0;
                }

                Logger.LogMessage("Current capture cleared.");
            }
        }

        private void OnDestroy()
        {
            // The whole process is shutting down, so Harmony patches disappear with it.
            // Calling UnpatchSelf here caused an unnecessary IL compile warning under Proton.
            Instance = null;
        }

        private void PatchCandidateResolvers()
        {
            List<CandidateMethod> candidates = new List<CandidateMethod>();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                string assemblyName = assembly.GetName().Name ?? string.Empty;

                if (!assemblyName.Equals("Assembly-CSharp", StringComparison.OrdinalIgnoreCase) &&
                    !assemblyName.Equals("lynesth.bd2.browndustx", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                foreach (Type type in GetTypesSafely(assembly))
                {
                    MethodInfo[] methods;

                    try
                    {
                        methods = type.GetMethods(
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.Static |
                            BindingFlags.Instance |
                            BindingFlags.DeclaredOnly);
                    }
                    catch
                    {
                        continue;
                    }

                    foreach (MethodInfo method in methods)
                    {
                        if (!IsPatchableStringMethod(method))
                        {
                            continue;
                        }

                        int score = ScoreCandidate(assemblyName, type, method);

                        if (score >= 5)
                        {
                            candidates.Add(new CandidateMethod(method, score));
                        }
                    }
                }
            }

            candidates = candidates
                .OrderByDescending(item => item.Score)
                .ThenBy(item => item.Method.DeclaringType?.FullName, StringComparer.Ordinal)
                .ThenBy(item => item.Method.Name, StringComparer.Ordinal)
                .Take(250)
                .ToList();

            string reportPath = Path.Combine(_outputRoot, "patched_methods.txt");

            using (StreamWriter writer = new StreamWriter(reportPath, false, new UTF8Encoding(false)))
            {
                foreach (CandidateMethod candidate in candidates)
                {
                    try
                    {
                        HarmonyMethod postfix = new HarmonyMethod(
                            typeof(LanguageResolverPostfix),
                            nameof(LanguageResolverPostfix.Postfix));

                        _harmony!.Patch(candidate.Method, postfix: postfix);
                        _patchedMethodCount++;

                        writer.WriteLine(
                            "[PATCHED score=" + candidate.Score.ToString(CultureInfo.InvariantCulture) + "] " +
                            FormatMethod(candidate.Method));
                    }
                    catch (Exception ex)
                    {
                        writer.WriteLine(
                            "[FAILED score=" + candidate.Score.ToString(CultureInfo.InvariantCulture) + "] " +
                            FormatMethod(candidate.Method) + " :: " + ex.GetType().Name + ": " + ex.Message);
                    }
                }
            }
        }

        private static IEnumerable<Type> GetTypesSafely(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(type => type != null).Cast<Type>();
            }
            catch
            {
                return Array.Empty<Type>();
            }
        }

        private static bool IsPatchableStringMethod(MethodInfo method)
        {
            if (method.ReturnType != typeof(string) ||
                method.IsAbstract ||
                method.IsGenericMethodDefinition ||
                method.ContainsGenericParameters)
            {
                return false;
            }

            if (method.GetParameters().Length > 8)
            {
                return false;
            }

            try
            {
                return method.GetMethodBody() != null;
            }
            catch
            {
                return false;
            }
        }

        private static int ScoreCandidate(string assemblyName, Type type, MethodInfo method)
        {
            int score = 0;
            string methodName = method.Name.ToLowerInvariant();
            string typeName = (type.FullName ?? type.Name).ToLowerInvariant();

            string[] exactNames =
            {
                "getlanguage",
                "getlanguagegame",
                "translate",
                "localize",
                "gettext",
                "getstring"
            };

            if (exactNames.Contains(methodName))
            {
                score += 8;
            }

            if (ContainsAny(methodName, "language", "localiz", "translate"))
            {
                score += 5;
            }

            if (ContainsAny(methodName, "gettext", "getstring", "resolve", "tooltip", "description"))
            {
                score += 3;
            }

            if (ContainsAny(typeName, "language", "localiz", "translate"))
            {
                score += 5;
            }

            if (ContainsAny(typeName, "rawdata", "table", "tooltip", "skill", "costume", "text"))
            {
                score += 2;
            }

            if (assemblyName.Equals("lynesth.bd2.browndustx", StringComparison.OrdinalIgnoreCase) &&
                ContainsAny(methodName, "translate", "translation"))
            {
                score += 7;
            }

            foreach (ParameterInfo parameter in method.GetParameters())
            {
                string parameterName = (parameter.Name ?? string.Empty).ToLowerInvariant();

                if (ContainsAny(parameterName, "id", "key"))
                {
                    score += 2;
                }

                if (ContainsAny(parameterName, "language", "locale"))
                {
                    score += 1;
                }

                if (IsSimpleKeyType(parameter.ParameterType))
                {
                    score += 1;
                }
            }

            return score;
        }

        private static bool ContainsAny(string value, params string[] needles)
        {
            foreach (string needle in needles)
            {
                if (value.Contains(needle))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSimpleKeyType(Type type)
        {
            Type actual = Nullable.GetUnderlyingType(type) ?? type;

            return actual == typeof(string) ||
                   actual == typeof(byte) ||
                   actual == typeof(sbyte) ||
                   actual == typeof(short) ||
                   actual == typeof(ushort) ||
                   actual == typeof(int) ||
                   actual == typeof(uint) ||
                   actual == typeof(long) ||
                   actual == typeof(ulong) ||
                   actual.IsEnum;
        }

        internal void Capture(MethodBase originalMethod, object[] args, string? result)
        {
            if (!_captureEnabled || string.IsNullOrWhiteSpace(result))
            {
                return;
            }

            int minimumLength = _minimumTextLength?.Value ?? 1;

            if (result.Length < minimumLength)
            {
                return;
            }

            ParameterInfo[] parameters;

            try
            {
                parameters = originalMethod.GetParameters();
            }
            catch
            {
                parameters = Array.Empty<ParameterInfo>();
            }

            string table = ExtractTable(originalMethod, parameters, args);
            IdSelection idSelection = ExtractId(parameters, args, result);
            string? id = idSelection.Value;

            if (string.IsNullOrWhiteSpace(id))
            {
                // Keep diagnostic entries, but they are exported separately because
                // BrownDustX needs a real table ID for translation replacement.
                id = "<unknown>";
            }

            string arguments = BuildArgumentsSummary(parameters, args);
            string sourceMethod = FormatMethod(originalMethod);
            string caller = string.Empty;

            if (_captureCaller != null && _captureCaller.Value)
            {
                caller = FindCaller();
            }

            string key = table + "\u001f" + id + "\u001f" + result;

            lock (_sync)
            {
                CapturedEntry entry;

                if (_entries.TryGetValue(key, out entry!))
                {
                    entry.Count++;
                    entry.LastSeenUtc = DateTime.UtcNow;
                }
                else
                {
                    _entries[key] = new CapturedEntry
                    {
                        Table = table,
                        Id = id,
                        Text = result,
                        SourceMethod = sourceMethod,
                        Arguments = arguments,
                        IdSource = idSelection.Source,
                        Caller = caller,
                        Count = 1,
                        FirstSeenUtc = DateTime.UtcNow,
                        LastSeenUtc = DateTime.UtcNow
                    };
                }

                _captureEventCount++;

                if (_captureEventCount % 100 == 0)
                {
                    Logger.LogInfo(
                        "Captured " + _captureEventCount.ToString(CultureInfo.InvariantCulture) +
                        " language resolution events; unique entries: " +
                        _entries.Count.ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        private static string ExtractTable(
            MethodBase method,
            ParameterInfo[] parameters,
            object[] args)
        {
            int count = Math.Min(parameters.Length, args.Length);

            // Brown Dust II exposes database selectors such as DB_COMMON.
            // Obfuscation hides the parameter/type names, so inspect the value itself first.
            for (int index = 0; index < count; index++)
            {
                object? value = args[index];

                if (value == null)
                {
                    continue;
                }

                string rendered = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;

                if (IsDatabaseToken(rendered))
                {
                    return SanitizeFileName(rendered);
                }
            }

            for (int index = 0; index < count; index++)
            {
                ParameterInfo parameter = parameters[index];
                object? value = args[index];

                if (value == null)
                {
                    continue;
                }

                string parameterName = (parameter.Name ?? string.Empty).ToLowerInvariant();
                Type valueType = value.GetType();
                string typeName = valueType.Name.ToLowerInvariant();

                if (ContainsAny(parameterName, "table", "database", "db") ||
                    (valueType.IsEnum && ContainsAny(typeName, "table", "database", "db")))
                {
                    return SanitizeFileName(
                        Convert.ToString(value, CultureInfo.InvariantCulture) ?? valueType.Name);
                }
            }

            return SanitizeFileName(method.DeclaringType?.Name ?? "UnknownTable");
        }

        private static IdSelection ExtractId(
            ParameterInfo[] parameters,
            object[] args,
            string result)
        {
            int count = Math.Min(parameters.Length, args.Length);

            // 1. Prefer an unobfuscated parameter explicitly named ID or key.
            for (int index = 0; index < count; index++)
            {
                object? value = args[index];

                if (value == null)
                {
                    continue;
                }

                string parameterName = (parameters[index].Name ?? string.Empty).ToLowerInvariant();
                string rendered = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;

                if (ContainsAny(parameterName, "language", "locale", "table", "database") ||
                    IsDatabaseToken(rendered))
                {
                    continue;
                }

                if (ContainsAny(parameterName, "id", "key") &&
                    TryConvertKey(value, out string? key))
                {
                    return new IdSelection(key, "named-parameter:" + parameters[index].Name);
                }
            }

            // 2. Inspect row-like objects for ID/Key fields or properties.
            foreach (object? value in args)
            {
                if (value == null || IsSimpleKeyType(value.GetType()))
                {
                    continue;
                }

                if (TryReadNamedMember(
                    value,
                    new[]
                    {
                        "ID", "Id", "id", "Key", "key",
                        "LanguageID", "LanguageId", "TextID", "TextId",
                        "StringID", "StringId", "SkillID", "SkillId"
                    },
                    out string? objectKey))
                {
                    return new IdSelection(objectKey, "object-member");
                }
            }

            // 3. Obfuscated methods often retain a single numeric key argument.
            // Prefer numeric primitives before enums/strings; this prevents DB_COMMON
            // from being incorrectly selected as the row ID.
            for (int index = 0; index < count; index++)
            {
                object? value = args[index];

                if (value == null || !IsNumericKeyType(value.GetType()))
                {
                    continue;
                }

                if (TryConvertKey(value, out string? numericKey))
                {
                    return new IdSelection(numericKey, "numeric-argument:" + index);
                }
            }

            // 4. Look for a key-like string, excluding the returned text and DB selector.
            for (int index = 0; index < count; index++)
            {
                object? value = args[index];

                if (!(value is string text))
                {
                    continue;
                }

                string trimmed = text.Trim();

                if (trimmed.Length == 0 ||
                    trimmed.Equals(result, StringComparison.Ordinal) ||
                    IsDatabaseToken(trimmed))
                {
                    continue;
                }

                if (LooksLikeStableKey(trimmed))
                {
                    return new IdSelection(trimmed, "key-like-string:" + index);
                }
            }

            // 5. Last-resort fallback for non-database simple values.
            for (int index = 0; index < count; index++)
            {
                object? value = args[index];

                if (value == null)
                {
                    continue;
                }

                string rendered = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;

                if (IsDatabaseToken(rendered) ||
                    rendered.Equals(result, StringComparison.Ordinal))
                {
                    continue;
                }

                if (TryConvertKey(value, out string? key))
                {
                    return new IdSelection(key, "fallback-argument:" + index);
                }
            }

            return new IdSelection(null, "not-found");
        }

        private static bool IsNumericKeyType(Type type)
        {
            Type actual = Nullable.GetUnderlyingType(type) ?? type;

            return actual == typeof(byte) ||
                   actual == typeof(sbyte) ||
                   actual == typeof(short) ||
                   actual == typeof(ushort) ||
                   actual == typeof(int) ||
                   actual == typeof(uint) ||
                   actual == typeof(long) ||
                   actual == typeof(ulong);
        }

        private static bool IsDatabaseToken(string value)
        {
            if (!value.StartsWith("DB_", StringComparison.Ordinal) || value.Length < 4)
            {
                return false;
            }

            for (int index = 3; index < value.Length; index++)
            {
                char character = value[index];

                if (!(character == '_' ||
                      (character >= 'A' && character <= 'Z') ||
                      (character >= '0' && character <= '9')))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool LooksLikeStableKey(string value)
        {
            if (value.Length > 160)
            {
                return false;
            }

            bool hasDigit = value.Any(char.IsDigit);
            bool hasSeparator = value.IndexOf('_') >= 0 ||
                                value.IndexOf('-') >= 0 ||
                                value.IndexOf('.') >= 0;

            bool allKeyCharacters = value.All(character =>
                char.IsLetterOrDigit(character) ||
                character == '_' ||
                character == '-' ||
                character == '.' ||
                character == ':');

            return allKeyCharacters && (hasDigit || hasSeparator);
        }

        private static string BuildArgumentsSummary(
            ParameterInfo[] parameters,
            object[] args)
        {
            int count = Math.Max(parameters.Length, args.Length);
            StringBuilder builder = new StringBuilder();

            for (int index = 0; index < count; index++)
            {
                if (index > 0)
                {
                    builder.Append(" | ");
                }

                string parameterName =
                    index < parameters.Length
                        ? parameters[index].Name ?? ("arg" + index.ToString(CultureInfo.InvariantCulture))
                        : "arg" + index.ToString(CultureInfo.InvariantCulture);

                object? value = index < args.Length ? args[index] : null;
                string typeName = value?.GetType().FullName ??
                                  (index < parameters.Length
                                      ? parameters[index].ParameterType.FullName ?? parameters[index].ParameterType.Name
                                      : "<unknown>");

                string rendered;

                try
                {
                    rendered = value == null
                        ? "<null>"
                        : Convert.ToString(value, CultureInfo.InvariantCulture) ?? "<null>";
                }
                catch
                {
                    rendered = "<ToString failed>";
                }

                rendered = rendered.Replace("\r", "\\r").Replace("\n", "\\n");

                if (rendered.Length > 300)
                {
                    rendered = rendered.Substring(0, 300) + "…";
                }

                builder.Append(index.ToString(CultureInfo.InvariantCulture));
                builder.Append(':');
                builder.Append(parameterName);
                builder.Append('<');
                builder.Append(typeName);
                builder.Append(">=");
                builder.Append(rendered);
            }

            return builder.ToString();
        }

        private sealed class IdSelection
        {
            public IdSelection(string? value, string source)
            {
                Value = value;
                Source = source;
            }

            public string? Value { get; }
            public string Source { get; }
        }

        private static bool TryReadNamedMember(
            object instance,
            IEnumerable<string> names,
            out string? value)
        {
            Type type = instance.GetType();

            foreach (string name in names)
            {
                try
                {
                    PropertyInfo? property = type.GetProperty(
                        name,
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.Instance |
                        BindingFlags.IgnoreCase);

                    if (property != null &&
                        property.GetIndexParameters().Length == 0 &&
                        TryConvertKey(property.GetValue(instance, null), out value))
                    {
                        return true;
                    }

                    FieldInfo? field = type.GetField(
                        name,
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.Instance |
                        BindingFlags.IgnoreCase);

                    if (field != null && TryConvertKey(field.GetValue(instance), out value))
                    {
                        return true;
                    }
                }
                catch
                {
                    // Ignore inaccessible or throwing members.
                }
            }

            value = null;
            return false;
        }

        private static bool TryConvertKey(object? input, out string? value)
        {
            value = null;

            if (input == null)
            {
                return false;
            }

            Type type = input.GetType();
            Type actual = Nullable.GetUnderlyingType(type) ?? type;

            if (actual.IsEnum)
            {
                value = Convert.ToString(input, CultureInfo.InvariantCulture);
                return !string.IsNullOrWhiteSpace(value);
            }

            if (actual == typeof(string))
            {
                string text = ((string)input).Trim();

                if (text.Length == 0 || text.Length > 160)
                {
                    return false;
                }

                value = text;
                return true;
            }

            if (actual == typeof(byte) ||
                actual == typeof(sbyte) ||
                actual == typeof(short) ||
                actual == typeof(ushort) ||
                actual == typeof(int) ||
                actual == typeof(uint) ||
                actual == typeof(long) ||
                actual == typeof(ulong))
            {
                value = Convert.ToString(input, CultureInfo.InvariantCulture);
                return true;
            }

            return false;
        }

        private static string FindCaller()
        {
            try
            {
                StackTrace trace = new StackTrace(2, false);

                foreach (StackFrame frame in trace.GetFrames() ?? Array.Empty<StackFrame>())
                {
                    MethodBase? method = frame.GetMethod();

                    if (method == null)
                    {
                        continue;
                    }

                    string declaringType = method.DeclaringType?.FullName ?? string.Empty;

                    if (declaringType.StartsWith("BD2SkillTextExporter.", StringComparison.Ordinal) ||
                        declaringType.StartsWith("HarmonyLib.", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    return FormatMethod(method);
                }
            }
            catch
            {
                // Caller information is optional.
            }

            return string.Empty;
        }

        private void ExportSnapshot()
        {
            List<CapturedEntry> snapshot;

            lock (_sync)
            {
                snapshot = _entries.Values
                    .Select(entry => entry.Clone())
                    .OrderBy(entry => entry.Table, StringComparer.Ordinal)
                    .ThenBy(entry => entry.Id, StringComparer.Ordinal)
                    .ThenBy(entry => entry.Text, StringComparer.Ordinal)
                    .ToList();
            }

            if (snapshot.Count == 0)
            {
                Logger.LogWarning(
                    "Nothing has been captured. Press F9, open character skill/costume details, " +
                    "then press F9 again.");
                return;
            }

            string sessionName = "skill_capture_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            string sessionDirectory = Path.Combine(_outputRoot, sessionName);
            string tablesDirectory = Path.Combine(sessionDirectory, "tables");

            Directory.CreateDirectory(sessionDirectory);
            Directory.CreateDirectory(tablesDirectory);

            WriteDetailedJson(Path.Combine(sessionDirectory, "all_entries.json"), snapshot);

            List<CapturedEntry> unknown = snapshot
                .Where(entry => entry.Id == "<unknown>")
                .ToList();

            if (unknown.Count > 0)
            {
                WriteDetailedJson(Path.Combine(sessionDirectory, "unknown_id_entries.json"), unknown);
            }

            List<CollisionRecord> collisions = new List<CollisionRecord>();

            foreach (IGrouping<string, CapturedEntry> tableGroup in snapshot
                         .Where(entry => entry.Id != "<unknown>")
                         .GroupBy(entry => entry.Table, StringComparer.Ordinal))
            {
                Dictionary<string, string> selected =
                    new Dictionary<string, string>(StringComparer.Ordinal);

                foreach (IGrouping<string, CapturedEntry> idGroup in tableGroup
                             .GroupBy(entry => entry.Id, StringComparer.Ordinal))
                {
                    List<CapturedEntry> alternatives = idGroup
                        .OrderByDescending(entry => entry.Count)
                        .ThenByDescending(entry => entry.LastSeenUtc)
                        .ToList();

                    selected[idGroup.Key] = alternatives[0].Text;

                    if (alternatives.Select(entry => entry.Text).Distinct(StringComparer.Ordinal).Count() > 1)
                    {
                        collisions.Add(new CollisionRecord
                        {
                            Table = tableGroup.Key,
                            Id = idGroup.Key,
                            Alternatives = alternatives
                        });
                    }
                }

                string tableFile = Path.Combine(
                    tablesDirectory,
                    SanitizeFileName(tableGroup.Key) + ".json");

                WriteStringMapJson(tableFile, selected);
            }

            WriteCollisionsJson(Path.Combine(sessionDirectory, "collisions.json"), collisions);

            File.WriteAllText(
                Path.Combine(sessionDirectory, "README.txt"),
                "These files were captured at runtime without modifying the game database.\n" +
                "The JSON files under tables/ are candidate ID -> original text maps.\n" +
                "Review collisions.json and unknown_id_entries.json before translating.\n" +
                "Do not assume the inferred table name is the exact BrownDustX filename until verified.\n",
                new UTF8Encoding(false));

            Logger.LogMessage(
                "Export complete: " + sessionDirectory +
                " | unique entries=" + snapshot.Count.ToString(CultureInfo.InvariantCulture) +
                " | collisions=" + collisions.Count.ToString(CultureInfo.InvariantCulture));
        }

        private static void WriteDetailedJson(string path, IEnumerable<CapturedEntry> entries)
        {
            using (StreamWriter writer = new StreamWriter(path, false, new UTF8Encoding(false)))
            {
                writer.WriteLine("[");

                CapturedEntry[] array = entries.ToArray();

                for (int index = 0; index < array.Length; index++)
                {
                    CapturedEntry entry = array[index];

                    writer.WriteLine("  {");
                    writer.WriteLine("    \"table\": \"" + EscapeJson(entry.Table) + "\",");
                    writer.WriteLine("    \"id\": \"" + EscapeJson(entry.Id) + "\",");
                    writer.WriteLine("    \"text\": \"" + EscapeJson(entry.Text) + "\",");
                    writer.WriteLine("    \"sourceMethod\": \"" + EscapeJson(entry.SourceMethod) + "\",");
                    writer.WriteLine("    \"arguments\": \"" + EscapeJson(entry.Arguments) + "\",");
                    writer.WriteLine("    \"idSource\": \"" + EscapeJson(entry.IdSource) + "\",");
                    writer.WriteLine("    \"caller\": \"" + EscapeJson(entry.Caller) + "\",");
                    writer.WriteLine("    \"count\": " + entry.Count.ToString(CultureInfo.InvariantCulture) + ",");
                    writer.WriteLine("    \"firstSeenUtc\": \"" + entry.FirstSeenUtc.ToString("O", CultureInfo.InvariantCulture) + "\",");
                    writer.WriteLine("    \"lastSeenUtc\": \"" + entry.LastSeenUtc.ToString("O", CultureInfo.InvariantCulture) + "\"");
                    writer.Write("  }");

                    if (index < array.Length - 1)
                    {
                        writer.Write(",");
                    }

                    writer.WriteLine();
                }

                writer.WriteLine("]");
            }
        }

        private static void WriteStringMapJson(string path, IDictionary<string, string> values)
        {
            using (StreamWriter writer = new StreamWriter(path, false, new UTF8Encoding(false)))
            {
                writer.WriteLine("{");

                KeyValuePair<string, string>[] array = values
                    .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .ToArray();

                for (int index = 0; index < array.Length; index++)
                {
                    KeyValuePair<string, string> pair = array[index];

                    writer.Write(
                        "  \"" + EscapeJson(pair.Key) + "\": \"" + EscapeJson(pair.Value) + "\"");

                    if (index < array.Length - 1)
                    {
                        writer.Write(",");
                    }

                    writer.WriteLine();
                }

                writer.WriteLine("}");
            }
        }

        private static void WriteCollisionsJson(string path, IEnumerable<CollisionRecord> collisions)
        {
            CollisionRecord[] array = collisions.ToArray();

            using (StreamWriter writer = new StreamWriter(path, false, new UTF8Encoding(false)))
            {
                writer.WriteLine("[");

                for (int index = 0; index < array.Length; index++)
                {
                    CollisionRecord collision = array[index];

                    writer.WriteLine("  {");
                    writer.WriteLine("    \"table\": \"" + EscapeJson(collision.Table) + "\",");
                    writer.WriteLine("    \"id\": \"" + EscapeJson(collision.Id) + "\",");
                    writer.WriteLine("    \"alternatives\": [");

                    for (int altIndex = 0; altIndex < collision.Alternatives.Count; altIndex++)
                    {
                        CapturedEntry alternative = collision.Alternatives[altIndex];

                        writer.Write(
                            "      { \"text\": \"" + EscapeJson(alternative.Text) +
                            "\", \"count\": " + alternative.Count.ToString(CultureInfo.InvariantCulture) +
                            ", \"sourceMethod\": \"" + EscapeJson(alternative.SourceMethod) + "\" }");

                        if (altIndex < collision.Alternatives.Count - 1)
                        {
                            writer.Write(",");
                        }

                        writer.WriteLine();
                    }

                    writer.WriteLine("    ]");
                    writer.Write("  }");

                    if (index < array.Length - 1)
                    {
                        writer.Write(",");
                    }

                    writer.WriteLine();
                }

                writer.WriteLine("]");
            }
        }

        private static string EscapeJson(string value)
        {
            StringBuilder builder = new StringBuilder(value.Length + 16);

            foreach (char character in value)
            {
                switch (character)
                {
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '\b':
                        builder.Append("\\b");
                        break;
                    case '\f':
                        builder.Append("\\f");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        if (character < 32)
                        {
                            builder.Append("\\u");
                            builder.Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            builder.Append(character);
                        }

                        break;
                }
            }

            return builder.ToString();
        }

        private static string SanitizeFileName(string value)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            StringBuilder builder = new StringBuilder(value.Length);

            foreach (char character in value)
            {
                builder.Append(invalid.Contains(character) ? '_' : character);
            }

            string result = builder.ToString().Trim();

            return string.IsNullOrWhiteSpace(result) ? "UnknownTable" : result;
        }

        private static string FormatMethod(MethodBase method)
        {
            string declaringType = method.DeclaringType?.FullName ?? "<unknown-type>";
            string parameters;

            try
            {
                parameters = string.Join(
                    ", ",
                    method.GetParameters().Select(parameter =>
                        parameter.ParameterType.Name + " " + (parameter.Name ?? "?")));
            }
            catch
            {
                parameters = "?";
            }

            return declaringType + "." + method.Name + "(" + parameters + ")";
        }

        private sealed class CandidateMethod
        {
            public CandidateMethod(MethodInfo method, int score)
            {
                Method = method;
                Score = score;
            }

            public MethodInfo Method { get; }
            public int Score { get; }
        }

        private sealed class CapturedEntry
        {
            public string Table { get; set; } = string.Empty;
            public string Id { get; set; } = string.Empty;
            public string Text { get; set; } = string.Empty;
            public string SourceMethod { get; set; } = string.Empty;
            public string Arguments { get; set; } = string.Empty;
            public string IdSource { get; set; } = string.Empty;
            public string Caller { get; set; } = string.Empty;
            public int Count { get; set; }
            public DateTime FirstSeenUtc { get; set; }
            public DateTime LastSeenUtc { get; set; }

            public CapturedEntry Clone()
            {
                return (CapturedEntry)MemberwiseClone();
            }
        }

        private sealed class CollisionRecord
        {
            public string Table { get; set; } = string.Empty;
            public string Id { get; set; } = string.Empty;
            public List<CapturedEntry> Alternatives { get; set; } = new List<CapturedEntry>();
        }

        private static class LanguageResolverPostfix
        {
            public static void Postfix(
                MethodBase __originalMethod,
                object[] __args,
                string __result)
            {
                try
                {
                    Instance?.Capture(__originalMethod, __args, __result);
                }
                catch (Exception ex)
                {
                    Instance?.Logger.LogWarning(
                        "Capture failure in " + FormatMethod(__originalMethod) + ": " + ex.Message);
                }
            }
        }
    }
}
