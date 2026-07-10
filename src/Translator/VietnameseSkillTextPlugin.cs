using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace BD2VietnameseSkillText
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class VietnameseSkillTextPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "local.bd2.vietnamese.skilltext";
        public const string PluginName = "BD2 Vietnamese Skill Text";
        public const string PluginVersion = "0.1.1";

        private static readonly object TranslationLock = new object();
        private static Dictionary<string, string> _translations =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static ManualLogSource? _log;
        private Harmony? _harmony;
        private string _translationPath = string.Empty;

        private void Awake()
        {
            _log = Logger;
            _translationPath = Path.Combine(
                Paths.ConfigPath,
                "BD2Vietnamese",
                "SkillTextTable_EN.json"
            );

            Directory.CreateDirectory(Path.GetDirectoryName(_translationPath)!);
            LoadTranslations();

            _harmony = new Harmony(PluginGuid);
            int patchedCount = PatchRawDataManager(_harmony);

            Logger.LogMessage(
                PluginName + " loaded. Patched methods: " + patchedCount +
                ". F6=reload translation."
            );
            Logger.LogMessage("Translation file: " + _translationPath);

            if (patchedCount == 0)
            {
                Logger.LogWarning(
                    "No matching RawDataManager.GetIntroValueString method was found."
                );
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F6))
            {
                LoadTranslations();
            }
        }

        private void LoadTranslations()
        {
            try
            {
                if (!File.Exists(_translationPath))
                {
                    Logger.LogWarning(
                        "Translation file does not exist: " + _translationPath
                    );
                    return;
                }

                string json = File.ReadAllText(
                    _translationPath,
                    new UTF8Encoding(false, true)
                );

                Dictionary<string, string> loaded =
                    FlatStringJsonParser.Parse(json);

                lock (TranslationLock)
                {
                    _translations = loaded;
                }

                Logger.LogMessage(
                    "Loaded " + loaded.Count.ToString(CultureInfo.InvariantCulture) +
                    " Vietnamese skill translations."
                );
            }
            catch (Exception exception)
            {
                Logger.LogError(
                    "Could not load translation JSON: " + exception
                );
            }
        }

        private static int PatchRawDataManager(Harmony harmony)
        {
            MethodInfo postfix = typeof(VietnameseSkillTextPlugin).GetMethod(
                nameof(GetIntroValueStringPostfix),
                BindingFlags.Static | BindingFlags.NonPublic
            )!;

            HarmonyMethod postfixPatch = new HarmonyMethod(postfix);
            HashSet<MethodBase> patched = new HashSet<MethodBase>();
            int count = 0;

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in GetTypesSafely(assembly))
                {
                    if (!string.Equals(
                        type.Name,
                        "RawDataManager",
                        StringComparison.Ordinal
                    ))
                    {
                        continue;
                    }

                    const BindingFlags flags =
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.Static |
                        BindingFlags.Instance;

                    foreach (MethodInfo method in type.GetMethods(flags))
                    {
                        if (!string.Equals(
                            method.Name,
                            "GetIntroValueString",
                            StringComparison.Ordinal
                        ))
                        {
                            continue;
                        }

                        if (method.ReturnType != typeof(string))
                        {
                            continue;
                        }

                        ParameterInfo[] parameters = method.GetParameters();

                        if (parameters.Length < 5)
                        {
                            continue;
                        }

                        if (parameters[2].ParameterType != typeof(int) ||
                            parameters[4].ParameterType != typeof(string))
                        {
                            continue;
                        }

                        if (!patched.Add(method))
                        {
                            continue;
                        }

                        try
                        {
                            harmony.Patch(method, postfix: postfixPatch);
                            count++;

                            _log?.LogInfo(
                                "Patched: " + type.FullName + "." + method.Name
                            );
                        }
                        catch (Exception exception)
                        {
                            _log?.LogWarning(
                                "Could not patch " + type.FullName + "." +
                                method.Name + ": " + exception.Message
                            );
                        }
                    }
                }
            }

            return count;
        }

        private static IEnumerable<Type> GetTypesSafely(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                List<Type> types = new List<Type>();

                foreach (Type? type in exception.Types)
                {
                    if (type != null)
                    {
                        types.Add(type);
                    }
                }

                return types;
            }
            catch
            {
                return Array.Empty<Type>();
            }
        }

        private static void GetIntroValueStringPostfix(
            object[] __args,
            ref string __result
        )
        {
            try
            {
                if (__args == null || __args.Length < 5)
                {
                    return;
                }

                string table = Convert.ToString(
                    __args[4],
                    CultureInfo.InvariantCulture
                ) ?? string.Empty;

                if (!table.StartsWith(
                    "SkillTextTable_",
                    StringComparison.OrdinalIgnoreCase
                ))
                {
                    return;
                }

                string id = Convert.ToString(
                    __args[2],
                    CultureInfo.InvariantCulture
                ) ?? string.Empty;

                string field = Convert.ToString(
                    __args[3],
                    CultureInfo.InvariantCulture
                ) ?? string.Empty;

                if (id.Length == 0)
                {
                    return;
                }

                string translated;
                string fieldSpecificKey = id + ":" + field;

                lock (TranslationLock)
                {
                    if (!_translations.TryGetValue(
                            fieldSpecificKey,
                            out translated!
                        ) &&
                        !_translations.TryGetValue(id, out translated!))
                    {
                        return;
                    }
                }

                __result = translated;
            }
            catch (Exception exception)
            {
                _log?.LogWarning(
                    "Translation replacement failed: " + exception.Message
                );
            }
        }

        private sealed class FlatStringJsonParser
        {
            private readonly string _text;
            private int _index;

            private FlatStringJsonParser(string text)
            {
                _text = text ?? throw new ArgumentNullException(nameof(text));
            }

            public static Dictionary<string, string> Parse(string text)
            {
                return new FlatStringJsonParser(text).ParseObject();
            }

            private Dictionary<string, string> ParseObject()
            {
                Dictionary<string, string> result =
                    new Dictionary<string, string>(
                        StringComparer.OrdinalIgnoreCase
                    );

                SkipWhitespace();
                Expect('{');
                SkipWhitespace();

                if (TryConsume('}'))
                {
                    return result;
                }

                while (true)
                {
                    SkipWhitespace();
                    string key = ParseString();
                    SkipWhitespace();
                    Expect(':');
                    SkipWhitespace();
                    string value = ParseString();
                    result[key] = value;
                    SkipWhitespace();

                    if (TryConsume('}'))
                    {
                        break;
                    }

                    Expect(',');
                }

                SkipWhitespace();

                if (_index != _text.Length)
                {
                    throw Error("Unexpected characters after JSON object.");
                }

                return result;
            }

            private string ParseString()
            {
                Expect('"');
                StringBuilder builder = new StringBuilder();

                while (_index < _text.Length)
                {
                    char current = _text[_index++];

                    if (current == '"')
                    {
                        return builder.ToString();
                    }

                    if (current != '\\')
                    {
                        builder.Append(current);
                        continue;
                    }

                    if (_index >= _text.Length)
                    {
                        throw Error("Incomplete escape sequence.");
                    }

                    char escape = _text[_index++];

                    switch (escape)
                    {
                        case '"':
                            builder.Append('"');
                            break;
                        case '\\':
                            builder.Append('\\');
                            break;
                        case '/':
                            builder.Append('/');
                            break;
                        case 'b':
                            builder.Append('\b');
                            break;
                        case 'f':
                            builder.Append('\f');
                            break;
                        case 'n':
                            builder.Append('\n');
                            break;
                        case 'r':
                            builder.Append('\r');
                            break;
                        case 't':
                            builder.Append('\t');
                            break;
                        case 'u':
                            builder.Append(ParseUnicodeEscape());
                            break;
                        default:
                            throw Error(
                                "Unsupported escape sequence: \\" + escape
                            );
                    }
                }

                throw Error("Unterminated JSON string.");
            }

            private char ParseUnicodeEscape()
            {
                if (_index + 4 > _text.Length)
                {
                    throw Error("Incomplete Unicode escape.");
                }

                string hex = _text.Substring(_index, 4);
                _index += 4;

                if (!ushort.TryParse(
                    hex,
                    NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture,
                    out ushort value
                ))
                {
                    throw Error("Invalid Unicode escape: \\u" + hex);
                }

                return (char)value;
            }

            private void SkipWhitespace()
            {
                while (_index < _text.Length &&
                       char.IsWhiteSpace(_text[_index]))
                {
                    _index++;
                }
            }

            private bool TryConsume(char expected)
            {
                if (_index < _text.Length && _text[_index] == expected)
                {
                    _index++;
                    return true;
                }

                return false;
            }

            private void Expect(char expected)
            {
                if (!TryConsume(expected))
                {
                    throw Error("Expected '" + expected + "'.");
                }
            }

            private FormatException Error(string message)
            {
                return new FormatException(
                    message + " Position: " +
                    _index.ToString(CultureInfo.InvariantCulture)
                );
            }
        }
    }
}
