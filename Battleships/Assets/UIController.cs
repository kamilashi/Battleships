using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [Header("Setup - Gameplay")]

    public Canvas gamePlayCanvas;
    public GameObject shipButtonPrefab;
    public RectTransform shipButtonParent;
    public Button submitButton;
    public TMPro.TMP_Text gamePhaseText;
    public TMPro.TMP_Text hintText;

    [Header("Setup - Game Over")]
    public Canvas gameOverCanvas;
    public TMPro.TMP_Text gameOverTitleText;
    public Button restartButton;

    [Header("Setup - Message")]
    public RectTransform messageContainerTransform;
    public TMPro.TMP_Text messageText;
    public float messageStayDuration;
    public float messageAnimateSpeed;


    [Header("Auto Setup")]
    public float messagePanelMaxHeight;

    [Header("Debug View")]
    public PlayerState localPlayerState;
    public List<UIShipButton> shipButtons;
    public Coroutine messageLogCoroutine;


    public float debugProgress;

    void Awake()
    {
        PlayerState.onShipAdded.AddListener(UpdateShipButtons);
        PlayerState.onLocalPlayerInitializedEvent.AddListener(OnLocalPlayerInitialized);
        PlayerState.onMessageLogged.AddListener(OnLogMessage);
        PlayerState.onGamePhaseChanged.AddListener(OnGamePhaseChanged);
        submitButton.onClick.AddListener(OnSubmitButtonClicked);
        restartButton.onClick.AddListener(OnRestartRequested);
        messagePanelMaxHeight = messageContainerTransform.rect.height;
        ResetMessagePanel();

        OnGamePhaseChanged(GamePhase.Wait, GamePhase.Wait);
    }

    void OnLocalPlayerInitialized(PlayerState placerState)
    {
        if (placerState.isLocalPlayer)
        {
            localPlayerState = placerState;
            Initialize();
        }
    }

    public void Initialize()
    {
        List<StaticShipData> shipDatas = localPlayerState.GetShipDataList();

        if (shipButtons.Count != 0)
        {
            foreach(UIShipButton btn in shipButtons)
            {
                Destroy(btn.gameObject);
            }

            shipButtons.Clear();
        }

        for ( int i = 0; i < (int) ShipType.Count; i++)
        {
            GameObject cellGameObject = GameObject.Instantiate(shipButtonPrefab, shipButtonParent);
            UIShipButton buttonObj = cellGameObject.GetComponent<UIShipButton>();
            shipButtons.Add(buttonObj);
            buttonObj.Initialize(this, shipDatas[i]);
        }
    }

    public void UpdateShipButtons(PlayerState player, Vector2Int coords, RuntimeShipData shipInstanceData, Orientation orientation)
    {
        if(player.isLocalPlayer)
        {
            for (int i = 0; i < (int)ShipType.Count; i++)
            {
                UIShipButton buttonObj = shipButtons[i];
                buttonObj.UpdateShipCount(localPlayerState.GetLocalGameState().shipManager.availableShipCounts[i]);
            }
        }
    }

    public void OnShipButtonClicked(ShipType type)
    {
        localPlayerState.OnShipTypeSelected(type);
    }
    public void OnSubmitButtonClicked()
    {
        localPlayerState.OnSubmitSignalReceived();
    }
    private void OnRestartRequested()
    {
        localPlayerState.CmdRequestRestart();
        OnGamePhaseChanged(GamePhase.Wait, GamePhase.Wait);
    }

    public void OnGamePhaseChanged(GamePhase oldPhase, GamePhase newPhase)
    {
        switch(newPhase)
        {
            case GamePhase.Wait:
                {
                    gameOverCanvas.gameObject.SetActive(true);
                    gamePlayCanvas.gameObject.SetActive(false);

                    gameOverTitleText.text = "Waiting for players...";
                    restartButton.gameObject.SetActive(false);
                }
                break;
            case GamePhase.Build:
                {
                    gamePlayCanvas.gameObject.SetActive(true);
                    gameOverCanvas.gameObject.SetActive(false);

                    gamePhaseText.text = "Build";
                    hintText.text = "Toggle orientation:\r\n\r\nR / middle m. b.\r\n\r\n";
                    hintText.text += "Submit to end turn.\r\n\r\n";
                    shipButtonParent.gameObject.SetActive(true);
                }
                break;
            case GamePhase.Combat:
                {
                    gamePhaseText.text = "Fight";
                    hintText.text = "Submit to end turn.\r\n\r\n";
                    shipButtonParent.gameObject.SetActive(false);
                }
                break;
            case GamePhase.GameOver:
                {
                    SyncedGameState syncedGameState = localPlayerState.GetSyncedGameState();
                    restartButton.gameObject.SetActive(true);
                    gameOverCanvas.gameObject.SetActive(true);
                    gamePlayCanvas.gameObject.SetActive(false);

                    gameOverTitleText.text = syncedGameState.gameOverState == GameOverState.Lose ? "You lost! " :
                        syncedGameState.gameOverState == GameOverState.Win ? "You won! " : " It's a tie...";
                }
                break;
        }
    }

    private void ScaleMessageY(float heightMultiplier)
    {
        Vector2 currentSize = messageContainerTransform.sizeDelta;
        messageContainerTransform.sizeDelta = new Vector2(currentSize.x, messagePanelMaxHeight * heightMultiplier);

        if(messageContainerTransform.rect.height < messageText.preferredHeight)
        {
            messageText.enabled = false;
        }
        else
        {
            messageText.enabled = true;
        }
    }

    private void OnLogMessage(string message)
    {
        if(messageLogCoroutine != null)
        {
            StopCoroutine(messageLogCoroutine);
            messageLogCoroutine = null;
        }

        ResetMessagePanel();

        messageText.text = message;
        messageLogCoroutine = StartCoroutine(LogMessageCoroutine());
    }

    private IEnumerator LogMessageCoroutine()
    {
        float progress = 0.0f;

        while (!Animation.AnimateUp(progress, 1.0f, ScaleMessageY))
        {
            progress += messageAnimateSpeed * Time.deltaTime;
            debugProgress = progress;
            yield return null;
        }

        float time = 0.0f;
        while (time <= messageStayDuration)
        {
            time += Time.deltaTime;
            yield return null;
        }

        while (!Animation.AnimateDown(progress, 0.0f, ScaleMessageY))
        {
            progress -= messageAnimateSpeed * Time.deltaTime;
            debugProgress = progress;
            yield return null;
        }

        ResetMessagePanel();

        messageLogCoroutine = null;
    }

    private void ResetMessagePanel()
    {
        ScaleMessageY(0.0f);
        messageText.enabled = false;
    }
}
