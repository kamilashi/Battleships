using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [Header("Setup")]
    public GameObject shipButtonPrefab;
    public RectTransform shipButtonParent;
    public Button submitButton;
    public TMPro.TMP_Text gamePhaseText;
    public TMPro.TMP_Text hintText;

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
        messagePanelMaxHeight = messageContainerTransform.rect.height;
        ResetMessagePanel();
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

        for( int i = 0; i < (int) ShipType.Count; i++)
        {
            GameObject cellGameObject = GameObject.Instantiate(shipButtonPrefab, shipButtonParent);
            UIShipButton buttonObj = cellGameObject.GetComponent<UIShipButton>();
            shipButtons.Add(buttonObj);
            buttonObj.Initialize(this, shipDatas[i]);
        }

        OnGamePhaseChanged(GamePhase.Build, GamePhase.Build);
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

    public void OnGamePhaseChanged(GamePhase oldPhase, GamePhase newPhase)
    {
        switch(newPhase)
        {
            case GamePhase.Build:
                {
                    gamePhaseText.text = "Build";
                    hintText.text = "Toggle orientation:\r\n\r\nR / middle m. b.\r\n\r\n";
                    hintText.text += "Submit to end turn.\r\n\r\n";
                }
                break;
            case GamePhase.Combat:
                {
                    gamePhaseText.text = "Fight";
                    hintText.text = "Submit to end turn.\r\n\r\n";
                }
                break;
        }
    }

    private bool AnimateUp(float parameter, float endValue, Action<float> animation)
    {
        animation.Invoke(parameter);

        if (parameter >= endValue) 
        {
            return true;
        }

        return false;
    }
    private bool AnimateDown(float parameter, float endValue, Action<float> animation)
    {
        animation.Invoke(parameter);

        if (parameter <= endValue)
        {
            return true;
        }

        return false;
    }

    private void EditMessageHeight(float heightMultiplier)
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

        while (!AnimateUp(progress, 1.0f, EditMessageHeight))
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

        while (!AnimateDown(progress, 0.0f, EditMessageHeight))
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
        EditMessageHeight(0.0f);
        messageText.enabled = false;
    }
}
