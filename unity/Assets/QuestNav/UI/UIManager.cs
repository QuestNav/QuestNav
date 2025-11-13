using System.Net;
using System.Net.Sockets;
using QuestNav.Config;
using QuestNav.Core;
using QuestNav.Network;
using QuestNav.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QuestNav.UI
{
    /// <summary>
    /// Interface for UI management.
    /// </summary>
    public interface IUIManager
    {
        /// <summary>
        /// Updates the connection state and ip address in the UI
        /// </summary>
        void UIPeriodic();
    }

    /// <summary>
    /// Manages UI elements and user interactions for the QuestNav application.
    /// </summary>
    public class UIManager : IUIManager
    {
        #region Fields
        /// <summary>
        /// Reference to NetworkTables connection
        /// </summary>
        private INetworkTableConnection networkTableConnection;

        /// <summary>
        /// Input field for team number entry
        /// </summary>
        private TMP_InputField teamInput;

        /// <summary>
        /// Checkbox for auto start on boot
        /// </summary>
        private Toggle autoStartToggle;

        /// <summary>
        /// IP address text
        /// </summary>
        private TMP_Text ipAddressText;

        /// <summary>
        /// ConState text
        /// </summary>
        private TMP_Text conStateText;

        /// <summary>
        /// posXText text
        /// </summary>
        private TMP_Text posXText;

        /// <summary>
        /// posYText text
        /// </summary>
        private TMP_Text posYText;

        /// <summary>
        /// posZText text
        /// </summary>
        private TMP_Text posZText;

        /// <summary>
        /// X rotation text
        /// </summary>
        private TMP_Text xRotText;

        /// <summary>
        /// Y rotation text
        /// </summary>
        private TMP_Text yRotText;

        /// <summary>
        /// Z rotation text
        /// </summary>
        private TMP_Text zRotText;

        /// <summary>
        /// Button to update team number
        /// </summary>
        private Button teamUpdateButton;

        /// <summary>
        /// Current team number
        /// </summary>
        private int teamNumber;

        /// <summary>
        /// Holds the detected local IP address of the HMD
        /// </summary>
        private string myAddressLocal = "0.0.0.0";
        #endregion

        /// <summary>
        /// Initializes the UI manager with required UI components.
        /// </summary>
        /// <param name="networkTableConnection">Network connection reference for updating state</param>
        /// <param name="teamInput">Input field for team number</param>
        /// <param name="ipAddressText">Text for IP address display</param>
        /// <param name="conStateText">Text for connection state display</param>
        /// <param name="posXText">Text for X coordinate of Quest position</param>
        /// <param name="posYText">Text for Y coordinate of Quest position</param>
        /// <param name="posZText">Text for Z coordinate of Quest position</param>
        /// <param name="xRotText">Text for X rotation of Quest position</param>
        /// <param name="yRotText">Text for Y rotation of Quest position</param>
        /// <param name="zRotText">Text for Z rotation of Quest position</param>
        /// <param name="teamUpdateButton">Button for updating team number</param>
        /// <param name="autoStartToggle">Button for turning auto start on/off</param>
        public UIManager(
            INetworkTableConnection networkTableConnection,
            TMP_InputField teamInput,
            TMP_Text ipAddressText,
            TMP_Text conStateText,
            TMP_Text posXText,
            TMP_Text posYText,
            TMP_Text posZText,
            TMP_Text xRotText,
            TMP_Text yRotText,
            TMP_Text zRotText,
            Button teamUpdateButton,
            Toggle autoStartToggle
        )
        {
            this.networkTableConnection = networkTableConnection;
            this.teamInput = teamInput;
            this.ipAddressText = ipAddressText;
            this.conStateText = conStateText;
            this.posXText = posXText;
            this.posYText = posYText;
            this.posZText = posZText;
            this.xRotText = xRotText;
            this.yRotText = yRotText;
            this.zRotText = zRotText;
            this.teamUpdateButton = teamUpdateButton;
            this.autoStartToggle = autoStartToggle;

            // Load team number from Tunables (synced with web config)
            teamNumber = Tunables.defaultTeamNumber;
            teamInput.text = teamNumber.ToString();
            setTeamNumberFromUI();

            teamUpdateButton.onClick.AddListener(setTeamNumberFromUI);

            // Load auto start from Tunables (synced with web config)
            autoStartToggle.isOn = Tunables.autoStartOnBoot;

            autoStartToggle.onValueChanged.AddListener(updateAutoStart);
        }

        #region Properties
        /// <summary>
        /// Gets the current team number.
        /// </summary>
        public int TeamNumber => teamNumber;

        /// <summary>
        /// Gets the current IP address.
        /// </summary>
        public string IPAddress => myAddressLocal;
        #endregion

        #region Setters
        /// <summary>
        /// Updates the team number based on user input and triggers an asynchronous connection reset.
        /// </summary>
        private void setTeamNumberFromUI()
        {
            QueuedLogger.Log("Updating Team Number");
            teamNumber = int.Parse(teamInput.text);

            // Update Tunables (synced with web config)
            Tunables.defaultTeamNumber = teamNumber;

            updateTeamNumberInputBoxPlaceholder(teamNumber);

            // Update the connection with new team number
            networkTableConnection.UpdateTeamNumber(teamNumber);
        }

        /// <summary>
        /// Sets the input box placeholder text with the current team number.
        /// </summary>
        /// <param name="teamNumber">The team number to display</param>
        private void updateTeamNumberInputBoxPlaceholder(int teamNumber)
        {
            teamInput.text = "";
            var placeholderText = teamInput.placeholder as TextMeshProUGUI;
            if (placeholderText != null)
            {
                placeholderText.text = teamNumber.ToString();
            }
        }

        /// <summary>
        /// Updates the default IP address shown in the UI with the current HMD IP address
        /// </summary>
        private void updateIPAddressText()
        {
            // Get the local IP
            var hostEntry = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in hostEntry.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    myAddressLocal = ip.ToString();
                    if (ipAddressText is not TextMeshProUGUI ipText)
                        return;
                    if (myAddressLocal == "127.0.0.1")
                    {
                        ipText.text = "No Adapter Found";
                        ipText.color = Color.red;
                    }
                    else
                    {
                        ipText.text = myAddressLocal;
                        ipText.color = Color.green;
                    }
                }
                break;
            }
        }

        /// <summary>
        /// Updates the connection state text display.
        /// </summary>
        private void updateConStateText()
        {
            TextMeshProUGUI conText = conStateText as TextMeshProUGUI;
            if (conText is null)
                return;
            if (networkTableConnection.IsConnected)
            {
                conText.text = "Connected to NT4";
                conText.color = Color.green;
            }
            else if (teamNumber == QuestNavConstants.Network.DEFAULT_TEAM_NUMBER)
            {
                conText.text = "Warning! Default Team Number still set! Trying to connect!";
                conText.color = Color.red;
            }
            else if (networkTableConnection.IsReadyToConnect)
            {
                conText.text = "Trying to connect to NT4";
                conText.color = Color.yellow;
            }
        }

        /// <summary>
        /// Updates the auto start preference in PlayerPrefs.
        /// </summary>
        /// <param name="newValue">new AutoStart value</param>
        void updateAutoStart(bool newValue)
        {
            // Update Tunables (synced with web config)
            Tunables.autoStartOnBoot = newValue;
        }

        public void UIPeriodic()
        {
            updateConStateText();
            updateIPAddressText();
            syncFromTunables();
        }

        private string lastDebugIPOverride = "";

        /// <summary>
        /// Syncs UI elements with Tunables values (updated via web config)
        /// </summary>
        private void syncFromTunables()
        {
            // Check if debug IP override changed - triggers reconnection
            if (lastDebugIPOverride != Tunables.debugNTServerAddressOverride)
            {
                string newValue = Tunables.debugNTServerAddressOverride ?? "";

                // Only trigger reconnection if the value is empty (cleared) or a valid IP
                bool shouldReconnect = string.IsNullOrEmpty(newValue) || IsValidIPAddress(newValue);

                if (shouldReconnect)
                {
                    lastDebugIPOverride = newValue;
                    // Trigger reconnection with current team number (which checks debug override internally)
                    networkTableConnection.UpdateTeamNumber(teamNumber);
                }
            }

            // Sync team number if changed via web interface
            if (teamNumber != Tunables.defaultTeamNumber)
            {
                teamNumber = Tunables.defaultTeamNumber;
                teamInput.text = teamNumber.ToString();
                updateTeamNumberInputBoxPlaceholder(teamNumber);
                networkTableConnection.UpdateTeamNumber(teamNumber);
            }

            // Sync auto-start toggle if changed via web interface
            if (autoStartToggle.isOn != Tunables.autoStartOnBoot)
            {
                autoStartToggle.SetIsOnWithoutNotify(Tunables.autoStartOnBoot);
            }
        }

        /// <summary>
        /// Validates if a string is a valid IPv4 address
        /// </summary>
        private bool IsValidIPAddress(string ipString)
        {
            if (string.IsNullOrEmpty(ipString))
                return false;

            string[] parts = ipString.Split('.');
            if (parts.Length != 4)
                return false;

            foreach (string part in parts)
            {
                if (!int.TryParse(part, out int num))
                    return false;

                if (num < 0 || num > 255)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Updates the connection state text display.
        /// </summary>
        public void UpdatePositionText(Vector3 position, Quaternion rotation)
        {
            TextMeshProUGUI xText = posXText as TextMeshProUGUI;
            TextMeshProUGUI yText = posYText as TextMeshProUGUI;
            TextMeshProUGUI zText = posZText as TextMeshProUGUI;
            TextMeshProUGUI xRotText = this.xRotText as TextMeshProUGUI;
            TextMeshProUGUI yRotText = this.yRotText as TextMeshProUGUI;
            TextMeshProUGUI zRotText = this.zRotText as TextMeshProUGUI;
            if (
                xText is null
                || yText is null
                || zText is null
                || xRotText is null
                || yRotText is null
                || zRotText is null
            )
                return;

            var frcPosition = Conversions.UnityToFrc3d(position, rotation);
            xText.text = $"{frcPosition.Translation.X:0.00} M";
            yText.text = $"{frcPosition.Translation.Y:0.00} M";
            zText.text = $"{frcPosition.Translation.Z:0.00} M";

            Quaternion unityQuat = new Quaternion(
                (float)frcPosition.Rotation.Q.X,
                (float)frcPosition.Rotation.Q.Y,
                (float)frcPosition.Rotation.Q.Z,
                (float)frcPosition.Rotation.Q.W
            );
            Vector3 euler = unityQuat.eulerAngles;
            xRotText.text = $"{euler.x:0.00}°";
            yRotText.text = $"{euler.y:0.00}°";
            zRotText.text = $"{euler.z:0.00}°";
        }
        #endregion
    }
}
