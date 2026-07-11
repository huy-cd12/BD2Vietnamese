using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;

namespace BD2VietnameseTooltip
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class VietnameseTooltipPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "local.bd2.vietnamese.tooltip";
        public const string PluginName = "BD2 Vietnamese Tooltip";
        public const string PluginVersion = "0.1.4";

        private readonly Dictionary<string, TooltipTranslation> _translations =
            new Dictionary<string, TooltipTranslation>(StringComparer.OrdinalIgnoreCase);

        private readonly HashSet<int> _loggedTooltipItems = new HashSet<int>();
        private readonly HashSet<string> _unknownTitles =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private ConfigEntry<KeyboardShortcut> _reloadKey;
        private ConfigEntry<float> _scanInterval;
        private ConfigEntry<bool> _logUnknownTitles;
        private ConfigEntry<bool> _autoReload;
        private ConfigEntry<float> _filePollInterval;

        private string _translationPath = string.Empty;
        private float _nextScanAt;
        private float _nextFilePollAt;
        private bool _knownFileExists;
        private DateTime _knownWriteTimeUtc;
        private long _knownFileLength;

        private void Awake()
        {
            _reloadKey = Config.Bind(
                "Hotkeys",
                "Reload translations",
                new KeyboardShortcut(KeyCode.F6),
                "Reload TooltipText_EN.json and immediately reapply visible tooltips.");

            _scanInterval = Config.Bind(
                "Runtime",
                "Scan interval seconds",
                0.20f,
                "How often active SkillInfoTooltip UI objects are checked.");

            _logUnknownTitles = Config.Bind(
                "Diagnostics",
                "Log unknown tooltip titles",
                true,
                "Log each untranslated tooltip title once per session.");

            _autoReload = Config.Bind(
                "Reload",
                "Auto reload JSON",
                true,
                "Automatically reload TooltipText_EN.json after it changes.");

            _filePollInterval = Config.Bind(
                "Reload",
                "File poll interval seconds",
                0.50f,
                "How often the tooltip translation file is checked for changes.");

            _translationPath = Path.Combine(
                Paths.ConfigPath,
                "BD2Vietnamese",
                "TooltipText_EN.json");

            LoadTranslations("startup");

            Logger.LogMessage(
                PluginName + " " + PluginVersion +
                " loaded. F6=reload. File: " + _translationPath);
        }

        private void Update()
        {
            if (_reloadKey.Value.IsDown())
            {
                LoadTranslations("F6");
                _loggedTooltipItems.Clear();
                _unknownTitles.Clear();
                ApplyVisibleTooltips();
                Logger.LogMessage("Tooltip translations reloaded with F6.");
            }

            if (_autoReload.Value &&
                Time.unscaledTime >= _nextFilePollAt)
            {
                _nextFilePollAt =
                    Time.unscaledTime +
                    Mathf.Max(
                        0.10f,
                        _filePollInterval.Value);

                PollTranslationFile();
            }

            if (Time.unscaledTime < _nextScanAt)
            {
                return;
            }

            float interval = Mathf.Max(0.05f, _scanInterval.Value);
            _nextScanAt = Time.unscaledTime + interval;
            ApplyVisibleTooltips();
        }

        private void PollTranslationFile()
        {
            bool exists =
                File.Exists(_translationPath);

            if (!exists)
            {
                if (_knownFileExists)
                {
                    LoadTranslations(
                        "file deleted");
                }

                return;
            }

            FileInfo info =
                new FileInfo(_translationPath);

            if (!_knownFileExists ||
                info.LastWriteTimeUtc !=
                    _knownWriteTimeUtc ||
                info.Length !=
                    _knownFileLength)
            {
                LoadTranslations(
                    "file changed");
            }
        }

        private void LoadTranslations(
            string reason)
        {
            try
            {
                if (!File.Exists(
                        _translationPath))
                {
                    _translations.Clear();
                    _knownFileExists = false;
                    _knownWriteTimeUtc =
                        DateTime.MinValue;
                    _knownFileLength = 0;

                    Logger.LogWarning(
                        "Tooltip translation file is missing; translations disabled. " +
                        "The plugin will not recreate it. Reason: " +
                        reason + ". Path: " +
                        _translationPath);

                    return;
                }

                FileInfo info =
                    new FileInfo(_translationPath);

                string json =
                    File.ReadAllText(
                        _translationPath,
                        Encoding.UTF8);

                Dictionary<string, TooltipTranslation> loaded =
                    JsonConvert.DeserializeObject<
                        Dictionary<string, TooltipTranslation>>(json);

                if (loaded == null)
                {
                    throw new InvalidDataException(
                        "JSON root must be an object.");
                }

                Dictionary<string, TooltipTranslation> validated =
                    new Dictionary<string, TooltipTranslation>(
                        StringComparer.OrdinalIgnoreCase);

                foreach (KeyValuePair<string, TooltipTranslation> entry in loaded)
                {
                    string normalizedKey =
                        NormalizeTitle(entry.Key);

                    if (string.IsNullOrWhiteSpace(
                            normalizedKey) ||
                        entry.Value == null)
                    {
                        continue;
                    }

                    TooltipTranslation value =
                        entry.Value;

                    value.Title =
                        value.Title ??
                        normalizedKey;

                    value.Info =
                        value.Info ??
                        string.Empty;

                    if (string.IsNullOrWhiteSpace(
                            value.Info))
                    {
                        Logger.LogWarning(
                            "Ignored tooltip with empty info: " +
                            normalizedKey);

                        continue;
                    }

                    if (validated.ContainsKey(
                            normalizedKey))
                    {
                        Logger.LogWarning(
                            "Duplicate normalized tooltip key replaced: " +
                            normalizedKey);
                    }

                    validated[normalizedKey] =
                        value;
                }

                _translations.Clear();

                foreach (
                    KeyValuePair<string, TooltipTranslation>
                        entry in validated)
                {
                    _translations[entry.Key] =
                        entry.Value;
                }

                _knownFileExists = true;
                _knownWriteTimeUtc =
                    info.LastWriteTimeUtc;
                _knownFileLength =
                    info.Length;

                Logger.LogMessage(
                    "Loaded " +
                    _translations.Count +
                    " tooltip translation entries. Reason: " +
                    reason + ".");
            }
            catch (Exception ex)
            {
                try
                {
                    FileInfo info =
                        new FileInfo(_translationPath);

                    _knownFileExists =
                        info.Exists;

                    if (info.Exists)
                    {
                        _knownWriteTimeUtc =
                            info.LastWriteTimeUtc;
                        _knownFileLength =
                            info.Length;
                    }
                }
                catch
                {
                    // Ignore metadata errors.
                }

                Logger.LogError(
                    "Failed to load TooltipText_EN.json. Existing in-memory translations were kept: " +
                    ex.GetType().Name + ": " +
                    ex.Message);
            }
        }

        private void ApplyVisibleTooltips()
        {
            if (_translations.Count == 0)
            {
                return;
            }

            TMP_Text[] allTexts;

            try
            {
                allTexts = Resources.FindObjectsOfTypeAll<TMP_Text>();
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    "Unable to scan TMP text objects: " +
                    ex.GetType().Name + ": " + ex.Message);
                return;
            }

            HashSet<int> processedTitles = new HashSet<int>();

            foreach (TMP_Text titleText in allTexts)
            {
                if (!IsUsableActiveText(titleText))
                {
                    continue;
                }

                if (!string.Equals(
                        titleText.gameObject.name,
                        "Text - Title",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!IsInsideSkillInfoTooltip(titleText.transform))
                {
                    continue;
                }

                int titleId = titleText.GetInstanceID();

                if (!processedTitles.Add(titleId))
                {
                    continue;
                }

                TMP_Text infoText = FindPairedInfoText(titleText);

                if (infoText == null)
                {
                    continue;
                }

                ApplyTooltipPair(titleText, infoText, titleId);
            }
        }

        private static bool IsUsableActiveText(TMP_Text text)
        {
            return text != null &&
                   text.gameObject != null &&
                   text.gameObject.activeInHierarchy;
        }

        private void ApplyTooltipPair(
            TMP_Text titleText,
            TMP_Text infoText,
            int titleId)
        {
            string currentTitle = NormalizeTitle(
                titleText.text ?? string.Empty);

            if (string.IsNullOrEmpty(currentTitle))
            {
                return;
            }

            TooltipTranslation translation;

            if (!TryFindTranslation(currentTitle, out translation))
            {
                if (_logUnknownTitles.Value &&
                    _unknownTitles.Add(currentTitle))
                {
                    Logger.LogMessage(
                        "Untranslated tooltip title: " +
                        currentTitle);
                }

                return;
            }

            bool changed = false;

            if (!string.IsNullOrEmpty(translation.Title) &&
                !string.Equals(
                    titleText.text,
                    translation.Title,
                    StringComparison.Ordinal))
            {
                titleText.text = translation.Title;
                changed = true;
            }

            if (!string.IsNullOrEmpty(translation.Info) &&
                !string.Equals(
                    infoText.text,
                    translation.Info,
                    StringComparison.Ordinal))
            {
                infoText.text = translation.Info;
                changed = true;
            }

            if (changed && _loggedTooltipItems.Add(titleId))
            {
                Logger.LogMessage(
                    "Applied tooltip translation: " +
                    currentTitle);
            }
        }

        private static TMP_Text FindPairedInfoText(TMP_Text titleText)
        {
            Transform current = titleText.transform.parent;
            int guard = 0;

            while (current != null && guard < 32)
            {
                if (!IsInsideSkillInfoTooltip(current))
                {
                    return null;
                }

                TMP_Text[] texts;

                try
                {
                    texts = current.GetComponentsInChildren<TMP_Text>(true);
                }
                catch
                {
                    return null;
                }

                TMP_Text[] activeTitles = texts
                    .Where(item =>
                        IsUsableActiveText(item) &&
                        string.Equals(
                            item.gameObject.name,
                            "Text - Title",
                            StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                TMP_Text[] activeInfos = texts
                    .Where(item =>
                        IsUsableActiveText(item) &&
                        string.Equals(
                            item.gameObject.name,
                            "Text - Info",
                            StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                if (activeTitles.Length == 1 &&
                    activeInfos.Length == 1 &&
                    activeTitles[0].GetInstanceID() ==
                        titleText.GetInstanceID())
                {
                    return activeInfos[0];
                }

                current = current.parent;
                guard++;
            }

            return null;
        }

        private static string NormalizeTitle(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            StringBuilder withoutTags =
                new StringBuilder(value.Length);

            bool insideTag = false;

            foreach (char character in value)
            {
                if (character == '<')
                {
                    insideTag = true;
                    continue;
                }

                if (insideTag)
                {
                    if (character == '>')
                    {
                        insideTag = false;
                    }

                    continue;
                }

                withoutTags.Append(
                    character == '\u00A0' ? ' ' : character);
            }

            StringBuilder normalized =
                new StringBuilder(withoutTags.Length);

            bool previousWasWhitespace = false;

            foreach (char character in withoutTags.ToString())
            {
                if (char.IsWhiteSpace(character))
                {
                    if (!previousWasWhitespace)
                    {
                        normalized.Append(' ');
                        previousWasWhitespace = true;
                    }
                }
                else
                {
                    normalized.Append(character);
                    previousWasWhitespace = false;
                }
            }

            return normalized.ToString().Trim();
        }

        private bool TryFindTranslation(
            string currentTitle,
            out TooltipTranslation translation)
        {
            string normalizedCurrentTitle =
                NormalizeTitle(currentTitle);

            if (_translations.TryGetValue(
                    normalizedCurrentTitle,
                    out translation))
            {
                return true;
            }

            foreach (KeyValuePair<string, TooltipTranslation> entry in _translations)
            {
                if (entry.Value != null &&
                    !string.IsNullOrEmpty(entry.Value.Title) &&
                    string.Equals(
                        NormalizeTitle(entry.Value.Title),
                        normalizedCurrentTitle,
                        StringComparison.OrdinalIgnoreCase))
                {
                    translation = entry.Value;
                    return true;
                }
            }

            translation = null;
            return false;
        }

        private static bool IsInsideSkillInfoTooltip(Transform transform)
        {
            Transform current = transform;
            int guard = 0;

            while (current != null && guard < 128)
            {
                string name = current.name ?? string.Empty;

                if (name.IndexOf(
                        "SkillInfoTooltip",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

                current = current.parent;
                guard++;
            }

            return false;
        }


        private sealed class TooltipTranslation
        {
            [JsonProperty("title")]
            public string Title { get; set; }

            [JsonProperty("info")]
            public string Info { get; set; }
        }


    }
}
