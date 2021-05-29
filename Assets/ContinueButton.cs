using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ContinueButton : MonoBehaviour, IPointerDownHandler
{
    public GameUI _gameUI;

    public void OnPointerDown(PointerEventData pointerEventData)
    {
        _gameUI.paused = false;
        _gameUI.canThrow = false;
    }
}