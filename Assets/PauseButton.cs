using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PauseButton : MonoBehaviour, IPointerDownHandler
{
    public GameUI _gameUI;

    public void OnPointerDown(PointerEventData pointerEventData)
    {
        _gameUI.paused = true;
    }
}