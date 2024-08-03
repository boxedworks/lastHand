using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Mirror;

public class GameController : MonoBehaviour
{
  public static GameController s_Singleton;

  public BeatController _beatController;

  EnemyController _enemyController;

  // Start is called before the first frame update
  void Start()
  {
    s_Singleton = this;

    new CardController();
    new ObjectController();

    _enemyController = new();

    //_beatController = new();

    //
    GameObject.Find("EndTurnButton").GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() =>
    {
      PlayerController.s_Players[0].OnTurnEnd();
    });

    // Start network
#if UNITY_EDITOR
    NetworkManager.singleton.StartHost();
#else
    NetworkManager.singleton.StartClient();
#endif
  }


  // Update is called once per frame
  void Update()
  {
    //_beatController.Update();

    ObjectController.s_Singleton.Update();
  }

  //
  public void OnTurnsEnded(){
    ObjectController.s_Singleton.HandleCardObjects();
  }

  //
  public class EnemyController
  {

    //
    public EnemyController()
    {
      new ObjectController.CardObject(0, new Vector2Int(4, 7), CardController.GetCardData(1));
      new ObjectController.CardObject(0, new Vector2Int(2, 7), CardController.GetCardData(2));
      new ObjectController.CardObject(0, new Vector2Int(1, 6), CardController.GetCardData(3));
      new ObjectController.CardObject(0, new Vector2Int(2, 6), CardController.GetCardData(6));
    }

    //
    public void Update()
    {

    }

    //
    public void OnTurnEnd()
    {

      // Move units


      //


    }

  }

  public class BeatController
  {

    //
    int _beatCount = 0; // Counter to keep track of the beats.
    int _beatLocal { get { return _beatCount % 4; } }
    int _currentMeasure;

    // UI components
    Transform _menuBeatCounter, _beatDisplays;
    TMPro.TextMeshProUGUI _textMeasureCounter;


    public BeatController()
    {
      _menuBeatCounter = GameObject.Find("BeatCounter").transform;
      _beatDisplays = _menuBeatCounter.GetChild(0).GetChild(1);
      _textMeasureCounter = _menuBeatCounter.GetChild(0).GetChild(0).GetChild(0).GetComponent<TMPro.TextMeshProUGUI>();
    }

    public void Update()
    {
      float bpm = 80; // Replace with the actual BPM of the song.
      float beatInterval = 60.0f / bpm; // Time between beats in seconds.

      //AudioSource audioSource = GetComponent<AudioSource>();
      //float songTime = audioSource.time; // Get the current playback time of the song.
      float songTime = Time.time; // Get the current playback time of the song.

      // Calculate the current beat number based on the song's time and beat interval.
      int currentBeat = (int)(songTime / beatInterval);
      int previousBeat = _beatCount; // Previous beat count stored in beatCount.

      // Check if a new beat has been reached
      if (currentBeat != previousBeat)
      {

        // Set current measure
        _currentMeasure = (currentBeat - 1) / 4;
        _textMeasureCounter.text = $"{(_currentMeasure % 4) + 1}";

        // Set local beat UI
        UnityEngine.UI.Image GetUIBeatDisplay(int index)
        {
          return _beatDisplays.GetChild(index).GetComponent<UnityEngine.UI.Image>();
        }
        if (_beatLocal == 0)
        {
          GetUIBeatDisplay(0).color = Color.white;
          GetUIBeatDisplay(1).color = Color.gray;
          GetUIBeatDisplay(2).color = Color.gray;
          GetUIBeatDisplay(3).color = Color.gray;
        }
        else
        {
          GetUIBeatDisplay(_beatLocal).color = Color.white;
        }

        //
        _beatCount = currentBeat; // Update the beat counter.
        Debug.Log(currentBeat);
      }
    }

  }
}
