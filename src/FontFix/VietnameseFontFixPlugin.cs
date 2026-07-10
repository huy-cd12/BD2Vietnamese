using BepInEx;
using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace BD2VietnameseFontFix
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class VietnameseFontFixPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "local.bd2.vietnamese.fontfix";
        public const string PluginName = "BD2 Vietnamese Font Fix";
        public const string PluginVersion = "0.2.0";

        private const string VietnameseProbe =
            "ăâđêôơưĂÂĐÊÔƠƯ" +
            "áàảãạấầẩẫậắằẳẵặ" +
            "éèẻẽẹếềểễệ" +
            "íìỉĩị" +
            "óòỏõọốồổỗộớờởỡợ" +
            "úùủũụứừửữự" +
            "ýỳỷỹỵ";

        private ConfigEntry<string>? _preferredFontNames;
        private ConfigEntry<bool>? _replaceVietnameseText;
        private ConfigEntry<float>? _scanInterval;
        private ConfigEntry<float>? _initialDelay;

        private TMP_FontAsset? _selectedFont;
        private float _nextScanTime;
        private float _initialReadyTime;
        private bool _initialized;
        private int _lastChangedCount;

        private void Awake()
        {
            _preferredFontNames = Config.Bind(
                "Font",
                "PreferredFontNames",
                "Noto Sans,Arial,DejaVu Sans,Segoe UI",
                "Tên font ưu tiên. Plugin thử font có sẵn trong game trước, rồi mới tạo từ font hệ thống."
            );

            _replaceVietnameseText = Config.Bind(
                "Font",
                "ReplaceVietnameseText",
                true,
                "Đổi toàn bộ font cho dòng có tiếng Việt. Đây là chế độ sửa khoảng cách hiệu quả nhất."
            );

            _scanInterval = Config.Bind(
                "Font",
                "ScanIntervalSeconds",
                0.5f,
                "Khoảng thời gian quét lại giao diện."
            );

            _initialDelay = Config.Bind(
                "Font",
                "InitialDelaySeconds",
                3.0f,
                "Chờ TextMeshPro và font của game tải xong trước khi chọn font."
            );

            _initialReadyTime =
                Time.unscaledTime + Math.Max(0.5f, _initialDelay.Value);

            Logger.LogMessage(
                PluginName +
                " loaded. Waiting for TMP initialization. F7=rescan/rebuild."
            );
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F7))
            {
                _initialized = false;
                _selectedFont = null;
                _initialReadyTime = Time.unscaledTime + 0.25f;

                Logger.LogMessage(
                    "F7 requested font rescan/rebuild."
                );
            }

            if (!_initialized &&
                Time.unscaledTime >= _initialReadyTime)
            {
                InitializeFontFix();
            }

            if (!_initialized || _selectedFont == null)
            {
                return;
            }

            if (Time.unscaledTime < _nextScanTime)
            {
                return;
            }

            _nextScanTime =
                Time.unscaledTime +
                Math.Max(0.1f, _scanInterval?.Value ?? 0.5f);

            ApplyFontFix();
        }

        private void InitializeFontFix()
        {
            _initialized = true;

            TMP_FontAsset? gameFont = FindBestLoadedGameFont();

            if (gameFont != null)
            {
                _selectedFont = gameFont;
                AddGlobalFallback(gameFont);

                Logger.LogMessage(
                    "Selected loaded TMP font asset: " +
                    gameFont.name +
                    " | Vietnamese coverage: " +
                    GetCoverage(gameFont) + "/" +
                    VietnameseProbe.Length
                );

                ApplyFontFix();
                return;
            }

            TMP_FontAsset? systemFont = TryCreateSystemFontAsset();

            if (systemFont != null)
            {
                _selectedFont = systemFont;
                AddGlobalFallback(systemFont);

                Logger.LogMessage(
                    "Created TMP font asset from system font: " +
                    systemFont.name
                );

                ApplyFontFix();
                return;
            }

            Logger.LogError(
                "No usable Vietnamese TMP font could be selected or created. " +
                "Press F7 after opening the skill screen, then check the log."
            );
        }

        private TMP_FontAsset? FindBestLoadedGameFont()
        {
            TMP_FontAsset[] assets;

            try
            {
                assets = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            }
            catch (Exception exception)
            {
                Logger.LogWarning(
                    "Could not enumerate loaded TMP font assets: " +
                    exception.Message
                );
                return null;
            }

            if (assets == null || assets.Length == 0)
            {
                Logger.LogWarning(
                    "No loaded TMP font assets were found."
                );
                return null;
            }

            string[] preferredNames = ParsePreferredNames();

            List<FontCandidate> candidates =
                new List<FontCandidate>();

            foreach (TMP_FontAsset asset in assets)
            {
                if (asset == null)
                {
                    continue;
                }

                int coverage = GetCoverage(asset);
                int preferredScore =
                    GetPreferredNameScore(asset.name, preferredNames);

                candidates.Add(
                    new FontCandidate(
                        asset,
                        coverage,
                        preferredScore
                    )
                );
            }

            foreach (FontCandidate candidate in candidates
                .OrderByDescending(item => item.Coverage)
                .ThenByDescending(item => item.PreferredScore)
                .Take(10))
            {
                Logger.LogInfo(
                    "TMP font candidate: " +
                    candidate.Asset.name +
                    " | coverage " +
                    candidate.Coverage + "/" +
                    VietnameseProbe.Length +
                    " | preference " +
                    candidate.PreferredScore
                );
            }

            FontCandidate? best = candidates
                .Where(item =>
                    item.Coverage >= VietnameseProbe.Length - 2
                )
                .OrderByDescending(item => item.PreferredScore)
                .ThenByDescending(item => item.Coverage)
                .FirstOrDefault();

            return best?.Asset;
        }

        private TMP_FontAsset? TryCreateSystemFontAsset()
        {
            string[] names = ParsePreferredNames();

            foreach (string name in names)
            {
                try
                {
                    Font unityFont =
                        Font.CreateDynamicFontFromOSFont(name, 36);

                    if (unityFont == null)
                    {
                        Logger.LogWarning(
                            "System font was not created: " + name
                        );
                        continue;
                    }

                    TMP_FontAsset asset =
                        TMP_FontAsset.CreateFontAsset(unityFont);

                    if (asset == null)
                    {
                        Logger.LogWarning(
                            "TMP_FontAsset.CreateFontAsset returned null for: " +
                            name
                        );
                        continue;
                    }

                    asset.name =
                        "BD2VietnameseFontFix_" + name;

                    asset.atlasPopulationMode =
                        AtlasPopulationMode.Dynamic;

                    int coverage = GetCoverage(asset);

                    Logger.LogInfo(
                        "Created system TMP font candidate: " +
                        name +
                        " | coverage " +
                        coverage + "/" +
                        VietnameseProbe.Length
                    );

                    if (coverage >= VietnameseProbe.Length - 2)
                    {
                        return asset;
                    }
                }
                catch (Exception exception)
                {
                    Logger.LogWarning(
                        "Could not create TMP font from system font '" +
                        name + "': " +
                        exception.GetType().Name + ": " +
                        exception.Message
                    );
                }
            }

            return null;
        }

        private string[] ParsePreferredNames()
        {
            return (_preferredFontNames?.Value ??
                    "Noto Sans,Arial,DejaVu Sans,Segoe UI")
                .Split(
                    new[] { ',' },
                    StringSplitOptions.RemoveEmptyEntries
                )
                .Select(item => item.Trim())
                .Where(item => item.Length > 0)
                .ToArray();
        }

        private static int GetPreferredNameScore(
            string assetName,
            string[] preferredNames
        )
        {
            for (int index = 0;
                 index < preferredNames.Length;
                 index++)
            {
                if (assetName.IndexOf(
                        preferredNames[index],
                        StringComparison.OrdinalIgnoreCase
                    ) >= 0)
                {
                    return preferredNames.Length - index;
                }
            }

            return 0;
        }

        private static int GetCoverage(TMP_FontAsset asset)
        {
            int coverage = 0;

            foreach (char character in VietnameseProbe)
            {
                try
                {
                    if (asset.HasCharacter(
                            character,
                            true,
                            true
                        ))
                    {
                        coverage++;
                    }
                }
                catch
                {
                    // Ignore a single failed glyph lookup.
                }
            }

            return coverage;
        }

        private void AddGlobalFallback(TMP_FontAsset fontAsset)
        {
            try
            {
                List<TMP_FontAsset> fallbacks =
                    TMP_Settings.fallbackFontAssets;

                if (fallbacks != null &&
                    !fallbacks.Contains(fontAsset))
                {
                    fallbacks.Insert(0, fontAsset);
                }
            }
            catch (Exception exception)
            {
                Logger.LogWarning(
                    "Could not add global TMP fallback: " +
                    exception.Message
                );
            }
        }

        private void ApplyFontFix()
        {
            TMP_FontAsset? selectedFont = _selectedFont;

            if (selectedFont == null)
            {
                return;
            }

            TMP_Text[] textObjects;

            try
            {
                textObjects =
                    Resources.FindObjectsOfTypeAll<TMP_Text>();
            }
            catch (Exception exception)
            {
                Logger.LogWarning(
                    "Could not scan TMP_Text objects: " +
                    exception.Message
                );
                return;
            }

            int changed = 0;

            foreach (TMP_Text textObject in textObjects)
            {
                if (textObject == null)
                {
                    continue;
                }

                string text = textObject.text ?? string.Empty;

                if (!ContainsVietnamese(text))
                {
                    continue;
                }

                try
                {
                    if (_replaceVietnameseText?.Value ?? true)
                    {
                        if (textObject.font != selectedFont)
                        {
                            textObject.font = selectedFont;
                            changed++;
                        }
                    }
                    else
                    {
                        TMP_FontAsset currentFont = textObject.font;

                        if (currentFont != null)
                        {
                            List<TMP_FontAsset> fallbacks =
                                currentFont.fallbackFontAssetTable;

                            if (fallbacks != null &&
                                !fallbacks.Contains(selectedFont))
                            {
                                fallbacks.Insert(0, selectedFont);
                                changed++;
                            }
                        }
                    }

                    textObject.ForceMeshUpdate(true, true);
                }
                catch (Exception exception)
                {
                    Logger.LogDebug(
                        "Could not apply font to one text object: " +
                        exception.Message
                    );
                }
            }

            if (changed != _lastChangedCount)
            {
                _lastChangedCount = changed;

                Logger.LogMessage(
                    "Vietnamese font applied to text objects: " +
                    changed
                );
            }
        }

        private static bool ContainsVietnamese(string text)
        {
            foreach (char character in text)
            {
                int value = character;

                if ((value >= 0x0102 && value <= 0x01B0) ||
                    (value >= 0x1EA0 && value <= 0x1EF9) ||
                    (value >= 0x0300 && value <= 0x036F))
                {
                    return true;
                }
            }

            return false;
        }

        private sealed class FontCandidate
        {
            public FontCandidate(
                TMP_FontAsset asset,
                int coverage,
                int preferredScore
            )
            {
                Asset = asset;
                Coverage = coverage;
                PreferredScore = preferredScore;
            }

            public TMP_FontAsset Asset { get; }
            public int Coverage { get; }
            public int PreferredScore { get; }
        }
    }
}
