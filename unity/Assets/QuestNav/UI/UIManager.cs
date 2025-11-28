using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using QuestNav.Config;
using QuestNav.Core;
using QuestNav.Network;
using QuestNav.Utils;
using QuestNav.WebServer;
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
        Task initializeAsync();
        
        /// <summary>
        /// Updates the connection state and ip address in the UI
        /// </summary>
        void uiPeriodic();

        /// <summary>
        /// Updates the position and rotation text in the UI.
        /// </summary>
        void updatePositionText(Vector3 position, Quaternion rotation);
    }

    /// <summary>
    /// Manages UI elements and user interactions for the QuestNav application.
    /// </summary>
    public class UIManager : IUIManager
    {
        #region Fields
        /// <summary>
        /// Reference to the ConfigManager to handle config changes
        /// </summary>
        private IConfigManager configManager;
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
        /// Current auto start value
        /// </summary>
        private bool autoStartValue;

        /// <summary>
        /// Holds the detected local IP address of the HMD
        /// </summary>
        private string ipAddress = "0.0.0.0";
        #endregion

        /// <summary>
        /// Initializes the UI manager with required UI components.
        /// </summary>
        /// <param name="configManager">Config manager for writing config changes</param>
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
            IConfigManager configManager,
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
            this.configManager = configManager;
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
            
            teamUpdateButton.onClick.AddListener(setTeamNumberFromUIAsync);
            autoStartToggle.onValueChanged.AddListener(setAutoStartValueFromUIAsync);

            // Attach local methods to config event methods
            configManager.onTeamNumberChanged += onTeamNumberChanged;
            configManager.onDebugIpOverrideChanged += onDebugIpOverrideChanged;
            configManager.onEnableAutoStartOnBootChanged += onEnableAutoStartOnBootChanged;
        }

        public async Task initializeAsync()
        {
            // Load saved values from config
            onTeamNumberChanged(await configManager.getTeamNumberAsync());
            onEnableAutoStartOnBootChanged(await configManager.getEnableAutoStartOnBootAsync());
        }

        #region Event Subscribers
        /// <summary>
        /// Sets the input box placeholder text with the current team number.
        /// </summary>
        /// <param name="teamNumber">The team number to display</param>
        private void onTeamNumberChanged(int teamNumber)
        {
            teamInput.text = "";
            var placeholderText = teamInput.placeholder as TextMeshProUGUI;
            if (placeholderText != null)
            {
                placeholderText.text = teamNumber.ToString();
            }
        }

        private void onDebugIpOverrideChanged(string ipOverride)
        {
            // No handling right now
        }

        private void onEnableAutoStartOnBootChanged(bool newValue)
        {
            autoStartToggle.isOn = newValue;
        }
        #endregion
        
        #region Setters
        /// <summary>
        /// Updates the team number based on user input and saves to config
        /// </summary>
        private async void setTeamNumberFromUIAsync()
        {
            QueuedLogger.Log("Updating Team Number from UI");
            teamNumber = int.Parse(teamInput.text);

            try
            {
                // Update config
                await configManager.setTeamNumberAsync(teamNumber);
            }
            catch (Exception e)
            {
                QueuedLogger.LogException(e);
            }
        }

        private async void setAutoStartValueFromUIAsync(bool newValue)
        {
            QueuedLogger.Log("Updating Auto Start Value from UI");
            autoStartValue = newValue;

            try
            {
                // Update config
                await configManager.setEnableAutoStartOnBootAsync(autoStartValue);
            }
            catch (Exception e)
            {
                QueuedLogger.LogException(e);
            }
        }
        #endregion

        #region Periodic
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
                    ipAddress = ip.ToString();
                    if (ipAddressText is not TextMeshProUGUI ipText)
                        return;
                    if (ipAddress == "127.0.0.1")
                    {
                        ipText.text = "No Adapter Found";
                        ipText.color = Color.red;
                    }
                    else
                    {
                        ipText.text = ipAddress;
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
            if (networkTableConnection.isConnected)
            {
                conText.text = "Connected to NT4";
                conText.color = Color.green;
            }
            else if (teamNumber == QuestNavConstants.Network.DEFAULT_TEAM_NUMBER)
            {
                conText.text = "Warning! Default Team Number still set! Trying to connect!";
                conText.color = Color.red;
            }
            else if (networkTableConnection.isReadyToConnect)
            {
                conText.text = "Trying to connect to NT4";
                conText.color = Color.yellow;
            }
        }

        public void uiPeriodic()
        {
            updateConStateText();
            updateIPAddressText();
        }

        /// <summary>
        /// Updates the connection state text display.
        /// </summary>
        public void updatePositionText(Vector3 position, Quaternion rotation)
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
