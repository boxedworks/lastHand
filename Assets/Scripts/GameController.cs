using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Mirror;

using System.Linq;

public class GameController : MonoBehaviour
{
  public static GameController s_Singleton;

  public BeatController _beatController;

  public EnemyController _EnemyController;

  // Start is called before the first frame update
  void Start()
  {
    s_Singleton = this;

    new CardController();
    new ObjectController();

    //_beatController = new();

    //
    GameObject.Find("EndTurnButton").GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() =>
    {
      PlayerController.s_Players[0]._OwnerController.OnTurnEnd();
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
  public void OnTurnsEnded()
  {
    ObjectController.s_Singleton.HandleCardObjects();
  }

  //
  public class EnemyController
  {

    //
    public PlayerController.OwnerController _OwnerController;

    //
    public EnemyController()
    {
      new ObjectController.CardObject(0, new Vector2Int(4, 5), CardController.GetCardData(1));
      new ObjectController.CardObject(0, new Vector2Int(2, 5), CardController.GetCardData(2));
      new ObjectController.CardObject(0, new Vector2Int(1, 4), CardController.GetCardData(3));
      new ObjectController.CardObject(0, new Vector2Int(2, 4), CardController.GetCardData(6));

      //
      _OwnerController = new(0);
      _OwnerController.SetHealth(10);
    }

    //
    public void Update()
    {

    }

    //
    public void OnTurnBegin()
    {

      // Move units

      // Give mana
      _OwnerController._Mana = 4;

      // Draw 3 cards
      for (var i = 0; i < 3; i++)
        _OwnerController._Deck.DrawCard();

    }

    //
    public bool DecideMove()
    {

      // Gather list of possible moves
      var actionsTotal = new List<(int manaCost, System.Action action)>();

      // Gather taps
      var cardObjects = ObjectController.GetCardObjects(0).Where(x => x._CardData.HasTapEffect && !x._IsTapped).ToList();
      foreach (var cardObject in cardObjects)
      {
        actionsTotal.Add((manaCost: 0, action: () =>
        {
          ObjectController.TryTap(cardObject);
        }
        ));
      }

      // Gather cards in hand
      var hand = _OwnerController._Hand.GetCards();
      Debug.Log("Card count: " + hand.Count);
      var cardIndex = -1;
      foreach (var card in hand)
      {
        cardIndex++;

        Debug.Log($"Checking card action {card.CardData.TextTitle}");

        // Spell
        if (card.CardData.IsSpell)
        {

        }

        // Unit / object
        else
        {

          // Check if can play unit
          var baseManaCost = card.CardData.CardInstanceData.Cost;
          var availableTiles = ObjectController.s_Singleton._TileMapPositionsAll
            .Where(t => ObjectController.GetCardObject(t) == null)
            .Where(t => PlayerController.TilemapController.IsTileOnCorrectBattlefield(_OwnerController._OwnerId, t))
            .Where(t => !card.CardData.IsObject ? true : !ObjectController.IsDeployTile(t))
            .Where(t => CardController.GetCardManaCost(_OwnerController._OwnerId, card.CardData, t) <= _OwnerController._Mana)
            .ToList();
          if (availableTiles.Count == 0)
            continue;

          var cardData = card.CardData;
          var randomPos = availableTiles[Random.Range(0, availableTiles.Count)];
          var manaCost = CardController.GetCardManaCost(_OwnerController._OwnerId, cardData, randomPos);
          actionsTotal.Add((manaCost: manaCost, action: () =>
          {

            Debug.Log($"Playing card {card.CardData.TextTitle}[{manaCost}] : {randomPos}");

            // Play card in random pos
            _OwnerController._Hand.PlayCard(cardIndex, cardData, randomPos);

          }
          ));
        }
      }

      //
      if (actionsTotal.Count == 0)
      {

        return false;

      }

      // Execute action
      else
      {

        actionsTotal[Random.Range(0, actionsTotal.Count)].action.Invoke();
        return true;
      }
    }

    /*/
    IEnumerator PlayUnitCard()
    {

    }*/

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
