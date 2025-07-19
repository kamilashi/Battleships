using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    private enum MainMenuPage
    {
        GameModeSelect,
        Connection
    }

    [Header("Setup")]
    public RectTransform menuEntriesParent;
    public TMPro.TMP_Text mainMenuTitleText;
    public TMPro.TMP_Text mainMenuStatusInfoText;
    public GameObject menuButtonPrefab;
    public Button backButton;

    [Header("DebugView")]
    private List<MainMenuPage> pageHistory = new List<MainMenuPage>();
    private List<UIBasicButton> menuButtons = new List<UIBasicButton>();

    void Awake()
    {
        backButton.onClick.AddListener(OnBackButtonClicked);
        NetworkController.onStatusInfoLogged.AddListener(UpdateStatusInfo);

        LoadMenuPage(MainMenuPage.GameModeSelect);
    }

    void ClearMenuEntries()
    {
        foreach(UIBasicButton btn in menuButtons)
        {
            Destroy(btn.gameObject);
        }

        menuButtons.Clear();    
    }

    UIBasicButton CreateButton()
    {
        GameObject buttonGameObject = GameObject.Instantiate(menuButtonPrefab, menuEntriesParent); //#TODO: replace with pooling
        UIBasicButton button = buttonGameObject.GetComponent<UIBasicButton>();
        menuButtons.Add(button);

        return button;
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

    void LoadMenuPage(MainMenuPage page)
    {
        if (menuButtons.Count != 0)
        {
            ClearMenuEntries();
        }

        switch (page)
        {
            case MainMenuPage.GameModeSelect:
                CreateGameModeMenu();
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
/*
    void LoadConnectionMenu()
    {
        LoadMenuPage(MainMenuPage.Connection);
        NetworkController.onNetworkmanagerReady.RemoveListener(LoadConnectionMenu);
    }*/

    private void UpdateStatusInfo(string info)
    {
        mainMenuStatusInfoText.text = info;
    }

    void OnLocalMultiplayerSelected()
    {
        NetworkController.onTransportSelected?.Invoke(MultiplayerMode.Local);
        LoadMenuPage(MainMenuPage.Connection);

        //NetworkController.onNetworkmanagerReady.AddListener(LoadConnectionMenu);
    }

    void OnRemoteMultiplayerSelected()
    {
        NetworkController.onTransportSelected?.Invoke(MultiplayerMode.Remote);
        LoadMenuPage(MainMenuPage.Connection);

        //NetworkController.onNetworkmanagerReady.AddListener(LoadConnectionMenu);
    }

    void OnStartHostSelected()
    {
        NetworkController.onStartHostSelected?.Invoke();
    }

    void OnStartClientSelected()
    {
        NetworkController.onStartClientSelected?.Invoke();
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
