using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class GameUI : MonoBehaviour
{
    public float time = 25;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI levelText;
    public Material redMaterial;
    public GameObject pauseCanvas;
    public GameObject loseCanvas;
    public GameObject winCanvas;

    public bool paused = false;
    public bool stopped = false;
    public bool canThrow = true;
}
