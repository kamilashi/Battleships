using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    private enum MainMenuPage
    {
        GameModeSelect,
        LocalNetworkDataInput,
        Connection
    }
    private enum InputType
    {
        IPAddress,
        Port
    }

    [Header("Setup")]
    public RectTransform menuEntriesParent;
    public TMPro.TMP_Text mainMenuTitleText;
    public TMPro.TMP_Text mainMenuStatusInfoText;
    public GameObject menuButtonPrefab;
    public GameObject labeledInputPrefab;
    public Button backButton;

    [Header("DebugView")]
    private List<MainMenuPage> pageHistory = new List<MainMenuPage>();
    private List<UIBasicButton> menuButtons = new List<UIBasicButton>();
    private Dictionary<InputType, UILabeledInput> labeledInputs = new Dictionary<InputType, UILabeledInput>();

    void Awake()
    {
        backButton.onClick.AddListener(OnBackButtonClicked);
        NetworkController.onStatusInfoLogged.AddListener(UpdateStatusInfo);
        NetworkController.onProceedToConnectionMenu.AddListener(LoadConnectionMenu);

        LoadMenuPage(MainMenuPage.GameModeSelect);
    }

    void ClearMenuButtons()
    {
        foreach(UIBasicButton btn in menuButtons)
        {
            Destroy(btn.gameObject);
        }

        menuButtons.Clear();    
    }
    void ClearLabeledInputs()
    {
        foreach (InputType type in labeledInputs.Keys)
        {
            Destroy(labeledInputs[type].gameObject);
        }

        labeledInputs.Clear();
    }

    UIBasicButton CreateButton()
    {
        GameObject buttonGameObject = Instantiate(menuButtonPrefab, menuEntriesParent); //#TODO: replace with pooling
        UIBasicButton button = buttonGameObject.GetComponent<UIBasicButton>();
        menuButtons.Add(button);

        return button;
    }

    UILabeledInput CreateLabeledInput(InputType type)
    {
        GameObject labelInputGameObject = Instantiate(labeledInputPrefab, menuEntriesParent); //#TODO: replace with pooling
        UILabeledInput labeledInput = labelInputGameObject.GetComponent<UILabeledInput>();
        labeledInputs.Add(type, labeledInput);

        return labeledInput;
    }

    void CreateGameModeMenu()
    {
        UIBasicButton localMultiplayerButton = CreateButton();
        localMultiplayerButton.button.onClick.AddListener(OnLocalMultiplayerSelected);
        localMultiplayerButton.buttonNameText.text = "Local Multiplayer";

        UIBasicButton remoteMultiplayerButton = CreateButton();
        remoteMultiplayerButton.button.onClick.AddListener(OnRemoteMultiplayerSelected);
        remoteMultiplayerButton.buttonNameText.text = "Remote Multiplayer";
    }

    void CreateConnectionMenu()
    {
        UIBasicButton startHostButton = CreateButton();
        startHostButton.button.onClick.AddListener(OnStartHostSelected);
        startHostButton.buttonNameText.text = "Start Host";

        UIBasicButton startClientButton = CreateButton();
        startClientButton.button.onClick.AddListener(OnStartClientSelected);
        startClientButton.buttonNameText.text = "Connect As Client";
    }

    void CreateLocalMultiplayerMenu()
    {
        UILabeledInput ipadress = CreateLabeledInput(InputType.IPAddress);
        ipadress.labelText.text = "Host IPv4:";
        ipadress.InputField.text = "192.168.0.X";

        UILabeledInput port = CreateLabeledInput(InputType.Port);
        port.labelText.text = "Port:";
        port.InputField.text = "7777";

        UIBasicButton submit = CreateButton();
        submit.button.onClick.AddListener(OnLocalMultiplayerDataSubmitted);
        submit.buttonNameText.text = "Submit";
    }

    void LoadMenuPage(MainMenuPage page)
    {
        if (menuButtons.Count != 0)
        {
            ClearMenuButtons();
        }

        if (labeledInputs.Count != 0)
        {
            ClearLabeledInputs();
        }

        switch (page)
        {
            case MainMenuPage.GameModeSelect:
                CreateGameModeMenu();
                break;
            case MainMenuPage.LocalNetworkDataInput:
                CreateLocalMultiplayerMenu();
                break;
            case MainMenuPage.Connection:
                CreateConnectionMenu();
                break;
        }

        if (pageHistory.Count == 0 || pageHistory[pageHistory.Count - 1] != page)
        {
            pageHistory.Add(page);
        }
    }

    private void UpdateStatusInfo(string info)
    {
        mainMenuStatusInfoText.text = info;
    }

    void OnLocalMultiplayerSelected()
    {
        LoadMenuPage(MainMenuPage.LocalNetworkDataInput);
    }

    void OnRemoteMultiplayerSelected()
    {
        NetworkController.onTransportSelected?.Invoke(MultiplayerMode.Remote);
        //LoadMenuPage(MainMenuPage.Connection);
    }

    void OnStartHostSelected()
    {
        NetworkController.onStartHostSelected?.Invoke();
    }

    void OnStartClientSelected()
    {
        NetworkController.onStartClientSelected?.Invoke();
    }

    void OnLocalMultiplayerDataSubmitted()
    {
        string ipAddress = labeledInputs[InputType.IPAddress].InputField.text;
        string portNumber = labeledInputs[InputType.Port].InputField.text;
        NetworkController.OnLocalMultiplayerDataSubmitted?.Invoke(ipAddress, portNumber);
    }

    void LoadConnectionMenu()
    {
        LoadMenuPage(MainMenuPage.Connection);
    }

    void OnBackButtonClicked()
    {
        if (pageHistory.Count > 1)
        {
            pageHistory.RemoveAt(pageHistory.Count - 1);
            MainMenuPage prevPage = pageHistory[pageHistory.Count - 1];
            LoadMenuPage(prevPage);
        }
    }
}
