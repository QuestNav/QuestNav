using System;
using System.Net;
using System.Net.Sockets;
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
        /// <param name="teamUpdateButton">Button for updating team number</param>
        public UIManager(
            INetworkTableConnection networkTableConnection,
            TMP_InputField teamInput,
            TMP_Text ipAddressText,
            TMP_Text conStateText,
            Button teamUpdateButton,
            Toggle autoStartToggle
        )
        {
            this.networkTableConnection = networkTableConnection;
            this.teamInput = teamInput;
            this.autoStartToggle = autoStartToggle;
            this.ipAddressText = ipAddressText;
            this.conStateText = conStateText;
            this.teamUpdateButton = teamUpdateButton;

            teamNumber = PlayerPrefs.GetInt(
                "TeamNumber",
                QuestNavConstants.Network.DEFAULT_TEAM_NUMBER
            );
            teamInput.text = teamNumber.ToString();
            setTeamNumberFromUI();

            teamUpdateButton.onClick.AddListener(setTeamNumberFromUI);

            // Load/Save auto start preference
            bool checkboxValue = PlayerPrefs.GetInt("AutoStart", 1) == 1;
            autoStartToggle.isOn = checkboxValue;

            autoStartToggle.onValueChanged.AddListener(updateAutoStart);
        }

        #region Properties
        /// <summary>
        /// Gets the current team number.
        /// </summary>
        public int TeamNumber => teamNumber;
        #endregion

        #region Setters
        /// <summary>
        /// Updates the team number based on user input and triggers an asynchronous connection reset.
        /// </summary>
        private void setTeamNumberFromUI()
        {
            QueuedLogger.Log("Updating Team Number");
            teamNumber = int.Parse(teamInput.text);
            PlayerPrefs.SetInt("TeamNumber", teamNumber);
            PlayerPrefs.Save();
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
                placeholderText.text = "Current: " + teamNumber;
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
            PlayerPrefs.SetInt("AutoStart", newValue ? 1 : 0);
            PlayerPrefs.Save();
        }
        public void UIPeriodic()
        {
            updateConStateText();
            updateIPAddressText();
        }
        #endregion
    }
}
