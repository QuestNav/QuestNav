#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using UnityEngine;

namespace QuestNav.Config
{
    /// <summary>
    /// Editor utility to install Android network security configuration.
    /// Allows cleartext HTTP traffic on local networks for the configuration server.
    /// Required for Quest to serve HTTP web interface on local network.
    /// </summary>
    public static class AndroidNetworkSecurityInstaller
    {
        private const string NETWORK_SECURITY_CONFIG =
            @"<?xml version=""1.0"" encoding=""utf-8""?>
<network-security-config>
    <domain-config cleartextTrafficPermitted=""true"">
        <domain includeSubdomains=""true"">localhost</domain>
        <domain includeSubdomains=""true"">127.0.0.1</domain>
        <domain includeSubdomains=""true"">10.0.0.0/8</domain>
        <domain includeSubdomains=""true"">172.16.0.0/12</domain>
        <domain includeSubdomains=""true"">192.168.0.0/16</domain>
    </domain-config>
    <base-config cleartextTrafficPermitted=""false"">
        <trust-anchors>
            <certificates src=""system"" />
        </trust-anchors>
    </base-config>
</network-security-config>";

        /// <summary>
        /// Creates network_security_config.xml in Android library resources.
        /// Enables cleartext HTTP on private networks for configuration server.
        /// Menu: QuestNav > Config > Install Network Security Config
        /// </summary>
        [MenuItem("QuestNav/Config/Install Network Security Config")]
        public static void InstallNetworkSecurityConfig()
        {
            string androidLibPath = Path.Combine(
                Application.dataPath,
                "Plugins",
                "Android",
                "QuestNavConfig.androidlib",
                "res",
                "xml"
            );

            if (!Directory.Exists(androidLibPath))
            {
                Directory.CreateDirectory(androidLibPath);
            }

            string configPath = Path.Combine(androidLibPath, "network_security_config.xml");
            File.WriteAllText(configPath, NETWORK_SECURITY_CONFIG);

            Debug.Log(
                $"[AndroidNetworkSecurityInstaller] Created network_security_config.xml in Android Library"
            );
            AssetDatabase.Refresh();
        }
    }
}
#endif
