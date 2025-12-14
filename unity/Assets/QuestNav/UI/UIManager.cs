using System;
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
        /// Updates the position and rotation text in the UI.
        /// </summary>
        void UpdatePositionText(Vector3 position, Quaternion rotation);
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
        private readonly IConfigManager configManager;

        /// <summary>
        /// Reference to NetworkTables connection
        /// </summary>
        private readonly INetworkTableConnection networkTableConnection;

        /// <summary>
        /// Input field for team number entry
        /// </summary>
        private readonly TMP_InputField teamInput;

        /// <summary>
        /// Checkbox for auto start on boot
        /// </summary>
        private readonly Toggle autoStartToggle;

        /// <summary>
        /// IP address text
        /// </summary>
        private readonly TMP_Text ipAddressText;

        /// <summary>
        /// Connection state text
        /// </summary>
        private readonly TMP_Text conStateText;

        /// <summary>
        /// X position text
        /// </summary>
        private readonly TMP_Text posXText;

        /// <summary>
        /// Y position text
        /// </summary>
        private readonly TMP_Text posYText;

        /// <summary>
        /// Z position text
        /// </summary>
        private readonly TMP_Text posZText;

        /// <summary>
        /// X rotation text
        /// </summary>
        private readonly TMP_Text xRotText;

        /// <summary>
        /// Y rotation text
        /// </summary>
        private readonly TMP_Text yRotText;

        /// <summary>
        /// Z rotation text
        /// </summary>
        private readonly TMP_Text zRotText;

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

            teamUpdateButton.onClick.AddListener(SetTeamNumberFromUIAsync);
            autoStartToggle.onValueChanged.AddListener(SetAutoStartValueFromUIAsync);

            // Attach local methods to config event methods
            configManager.OnTeamNumberChanged += OnTeamNumberChanged;
            configManager.OnDebugIpOverrideChanged += OnDebugIpOverrideChanged;
            configManager.OnEnableAutoStartOnBootChanged += OnEnableAutoStartOnBootChanged;

            // Attach local methods to network table event methods
            networkTableConnection.OnConnect += OnConnect;
            networkTableConnection.OnDisconnect += OnDisconnect;
            networkTableConnection.OnIpAddressChanged += OnIpAddressChanged;
        }

        #region Event Subscribers

        // ConfigManager
        /// <summary>
        /// Sets the input box placeholder text with the current team number.
        /// </summary>
        /// <param name="teamNumber">The team number to display</param>
        private void OnTeamNumberChanged(int teamNumber)
        {
            this.teamNumber = teamNumber;
            teamInput.text = "";
            var placeholderText = teamInput.placeholder as TextMeshProUGUI;
            if (placeholderText != null)
            {
                placeholderText.text = teamNumber.ToString();
            }
        }

        /// <summary>
        /// Handles debug IP override changes.
        /// </summary>
        /// <param name="ipOverride">The new IP override value</param>
        private void OnDebugIpOverrideChanged(string ipOverride)
        {
            // No handling right now
        }

        /// <summary>
        /// Updates the auto start toggle to match the config value.
        /// </summary>
        /// <param name="newValue">The new auto start setting</param>
        private void OnEnableAutoStartOnBootChanged(bool newValue)
        {
            autoStartToggle.isOn = newValue;
        }

        // NetworkTableConnection
        /// <summary>
        /// Updates IP address display and color based on validity.
        /// </summary>
        /// <param name="newIp">The new IP address</param>
        private void OnIpAddressChanged(string newIp)
        {
            if (ipAddressText is not TextMeshProUGUI ipText)
                return;
            if (newIp == "127.0.0.1")
            {
                ipText.text = "No Adapter Found";
                ipText.color = Color.red;
            }
            else
            {
                ipText.text = newIp;
                ipText.color = Color.green;
            }
        }

        /// <summary>
        /// Updates connection state display to show connected status.
        /// </summary>
        private void OnConnect()
        {
            if (conStateText is not TextMeshProUGUI conText)
                return;

            conText.text = "Connected to NT4";
            conText.color = Color.green;
        }

        /// <summary>
        /// Updates connection state display to show disconnected status with appropriate warnings.
        /// </summary>
        private void OnDisconnect()
        {
            if (conStateText is not TextMeshProUGUI conText)
                return;

            if (teamNumber == QuestNavConstants.Network.DEFAULT_TEAM_NUMBER)
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
        #endregion

        #region Setters
        /// <summary>
        /// Updates the team number based on user input and saves to config
        /// </summary>
        private async void SetTeamNumberFromUIAsync()
        {
            QueuedLogger.Log("Updating Team Number from UI");
            teamNumber = int.Parse(teamInput.text);

            try
            {
                // Update config
                await configManager.SetTeamNumberAsync(teamNumber);
            }
            catch (Exception e)
            {
                QueuedLogger.LogException(e);
            }
        }

        /// <summary>
        /// Updates the auto start setting based on toggle value and saves to config.
        /// </summary>
        /// <param name="newValue">The new auto start value</param>
        private async void SetAutoStartValueFromUIAsync(bool newValue)
        {
            QueuedLogger.Log("Updating Auto Start Value from UI");
            autoStartValue = newValue;

            try
            {
                // Update config
                await configManager.SetEnableAutoStartOnBootAsync(autoStartValue);
            }
            catch (Exception e)
            {
                QueuedLogger.LogException(e);
            }
        }
        #endregion

        #region Periodic

        /// <summary>
        /// Updates the position and rotation text displays with current values.
        /// </summary>
        /// <param name="position">Current position vector</param>
        /// <param name="rotation">Current rotation quaternion</param>
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
