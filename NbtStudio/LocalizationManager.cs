using System;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Text;
using System.Diagnostics;
using System.Collections.Concurrent;
using NbtStudio.Properties;
using System.Collections.Generic;

public static class LocalizationManager
{
    public static string CurrentLanguage { get; private set; } = "en-US";
    private static ConcurrentDictionary<string, string> _currentStrings = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, string> _defaultStrings = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private static readonly object _syncLock = new object();

    public static void LoadLanguageFile()
    {
        var savedLang = Settings.Default.Language ?? "en-US";
        if (!LoadLanguage(savedLang)) LoadLanguage("en-US");
    }

    public static bool LoadLanguage(string langCode)
    {
        lock (_syncLock)
        {
            try
            {
                var basePath = Path.Combine(Application.StartupPath, "Localization");
                var fullPath = Path.GetFullPath(Path.Combine(basePath, $"{langCode}.json"));

                // ��֤·����ȫ��
                if (!fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                {
                    ShowError("��Ч�������ļ�·��");
                    return false;
                }

                if (!File.Exists(fullPath))
                {
                    ShowError($"�����ļ� {langCode}.json δ�ҵ�");
                    return false;
                }

                string json;
                using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    json = reader.ReadToEnd();
                }

                if (string.IsNullOrWhiteSpace(json))
                {
                    ShowError($"�����ļ�Ϊ��: {langCode}.json");
                    return false;
                }

                // ��ȫ�����л�����
                var settings = new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore,
                    MaxDepth = 10
                };

                var newStrings = JsonConvert.DeserializeObject<ConcurrentDictionary<string, string>>(json, settings);
                _currentStrings = new ConcurrentDictionary<string, string>(newStrings, StringComparer.OrdinalIgnoreCase);

                CurrentLanguage = langCode;

                if (langCode.Equals("en-US", StringComparison.OrdinalIgnoreCase))
                {
                    _defaultStrings.Clear();
                    foreach (var pair in _currentStrings)
                    {
                        _defaultStrings[pair.Key] = pair.Value;
                    }
                }

                return true;
            }
            catch (JsonException ex)
            {
                ShowError($"JSON��������: {ex.Message}", "��ʽ����");
                return false;
            }
            catch (IOException ex)
            {
                ShowError($"�ļ����ʴ���: {ex.Message}", "IO����");
                return false;
            }
            catch (Exception ex)
            {
                ShowError($"��������ʧ��: {ex.GetType().Name} - {ex.Message}");
                return false;
            }
        }
    }

    public static string GetText(string key, string defaultValue = null, params object[] args)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            Debug.WriteLine($"��Ч�ı��ػ�������: [{key}]");
            return "[INVALID_KEY]";
        }

        // ��ȡԭʼ�ı�
        if (!TryGetLocalizedText(key, out var text, defaultValue))
        {
            Debug.WriteLine($"���ػ���ȱʧ: {key}");
            text = defaultValue ?? key;
        }

        // ��ȫ��ʽ������
        return FormatText(text, args, key);
    }

    private static bool TryGetLocalizedText(string key, out string text, string defaultValue)
    {
        if (_currentStrings.TryGetValue(key, out text)) return true;
        if (_defaultStrings.TryGetValue(key, out text)) return true;

        text = defaultValue;
        return false;
    }

    private static string FormatText(string text, object[] args, string key)
    {
        if (args == null || args.Length == 0) return text;

        try
        {
            return string.Format(text, args);
        }
        catch (FormatException ex)
        {
            Debug.WriteLine($"��ʽ��ʧ��: {key} - {ex.Message}");
            return $"[FORMAT_ERROR:{key}]";
        }
        catch (ArgumentNullException)
        {
            Debug.WriteLine($"�ղ����쳣: {key}");
            return $"[NULL_ARGUMENT:{key}]";
        }
    }

    private static void ShowError(string message, string title = "����")
    {
        try
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"������ʾʧ��: {ex.Message}");
        }
    }

    // ���Ĭ����������
    private static readonly Dictionary<string, Dictionary<string, string>> DefaultLanguages = new Dictionary<string, Dictionary<string, string>>
    {
        {
            "en-US", new Dictionary<string, string>
            {
                {"MenuFile", "File"},
                {"MenuEdit", "Edit"},
                {"MenuSearch", "Find"},
                {"MenuHelp", "Help"},
            }
        },
        {
            "zh-CN", new Dictionary<string, string>
            {
                {"MenuFile", "�ļ�"},
                {"MenuEdit", "�༭"},
                {"MenuSearch", "����"},
                {"MenuHelp", "����"},
            }
        }
    };

    public static void InitializeLocalizationFiles()
    {
        try
        {
            var localizationDir = Path.Combine(Application.StartupPath, "Localization");

            // �����ļ��У���������ڣ�
            if (!Directory.Exists(localizationDir))
            {
                Directory.CreateDirectory(localizationDir);
                Debug.WriteLine($"�Ѵ������ػ�Ŀ¼��{localizationDir}");
            }

            // ����Ĭ�������ļ�
            foreach (var lang in DefaultLanguages)
            {
                var filePath = Path.Combine(localizationDir, $"{lang.Key}.json");

                if (!File.Exists(filePath))
                {
                    CreateLanguageFile(filePath, lang.Value);
                }
                else
                {
                    ValidateLanguageFile(filePath, lang.Value);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"��ʼ�����ػ��ļ�ʧ��: {ex.Message}");
        }
    }

    private static void CreateLanguageFile(string path, Dictionary<string, string> defaultContent)
    {
        try
        {
            var json = JsonConvert.SerializeObject(defaultContent, Formatting.Indented);
            File.WriteAllText(path, json, Encoding.UTF8);
            Debug.WriteLine($"�Ѵ���Ĭ�������ļ��� {Path.GetFileName(path)}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"���������ļ�ʧ�� {path}: {ex.Message}");
        }
    }

    private static void ValidateLanguageFile(string path, Dictionary<string, string> defaultContent)
    {
        try
        {
            var existing = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path));

            // ���ȱʧ�ļ�
            foreach (var key in defaultContent.Keys)
            {
                if (!existing.ContainsKey(key))
                {
                    existing[key] = defaultContent[key];
                    Debug.WriteLine($"��ȱ�ٵļ� '{key}' ��ӵ� {Path.GetFileName(path)}");
                }
            }

            // �����ļ���������Ҫʱ��
            if (existing.Count > defaultContent.Count)
            {
                var updatedJson = JsonConvert.SerializeObject(existing, Formatting.Indented);
                File.WriteAllText(path, updatedJson, Encoding.UTF8);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"��֤�����ļ�ʧ�� {path}: {ex.Message}");
        }
    }
}