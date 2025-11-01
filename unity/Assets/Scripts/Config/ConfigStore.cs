using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace QuestNav.Config
{
    public class ConfigStore
    {
        private const string CONFIG_FILENAME = "config.json";
        private const string AUTH_FILENAME = "auth.json";

        private readonly string m_configPath;
        private readonly string m_authPath;

        public ConfigStore()
        {
            m_configPath = Path.Combine(Application.persistentDataPath, CONFIG_FILENAME);
            m_authPath = Path.Combine(Application.persistentDataPath, AUTH_FILENAME);
        }

        public ConfigData LoadConfig()
        {
            try
            {
                if (File.Exists(m_configPath))
                {
                    string json = File.ReadAllText(m_configPath);
                    return JsonConvert.DeserializeObject<ConfigData>(json);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigStore] Failed to load config: {ex.Message}");
            }
            return new ConfigData();
        }

        public bool SaveConfig(ConfigData config)
        {
            try
            {
                config.lastModified = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(m_configPath, json);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigStore] Failed to save config: {ex.Message}");
                return false;
            }
        }

        public AuthToken LoadOrGenerateToken()
        {
            try
            {
                if (File.Exists(m_authPath))
                {
                    string json = File.ReadAllText(m_authPath);
                    return JsonConvert.DeserializeObject<AuthToken>(json);
                }

                var token = new AuthToken
                {
                    token = GenerateSecureToken(),
                    createdAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    deviceId = SystemInfo.deviceUniqueIdentifier,
                };

                string tokenJson = JsonConvert.SerializeObject(token, Formatting.Indented);
                File.WriteAllText(m_authPath, tokenJson);

                Debug.Log($"[ConfigStore] Generated new auth token: {token.token}");
                return token;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigStore] Failed to load/generate token: {ex.Message}");
                return new AuthToken
                {
                    token = GenerateSecureToken(),
                    createdAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    deviceId = SystemInfo.deviceUniqueIdentifier,
                };
            }
        }

        private string GenerateSecureToken()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new System.Random();
            var token = new char[32];

            for (int i = 0; i < token.Length; i++)
            {
                token[i] = chars[random.Next(chars.Length)];
            }

            return new string(token);
        }

        public string GetConfigPath() => m_configPath;
    }
}
