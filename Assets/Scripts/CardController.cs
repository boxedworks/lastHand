using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardController
{

  public GameObject _CardBase;

  //
  public static CardController s_Singleton;
  public CardController()
  {
    s_Singleton = this;

    //
    _CardBase = GameObject.Find("PlayerHand").transform.Find("CardBase").gameObject;
    _CardBase.SetActive(false);

    // Create card data
    _cardData = new();
    void RegisterCard(int cardId, CardData cardData)
    {
      //var cardId = _cardData.Count;
      cardData.CardId = cardId;
      _cardData.Add(cardId, cardData);
    }

    // Test cards
    RegisterCard(1, new CardData()
    {
      TextTitle = "Bat",
      TextDescription = "",

      Deck = CardData.DeckType.BEAST,

      BehaviorPattern = "f",

      CardInstanceData = new CardInstanceData()
      {
        Cost = 1,

        Health = 1,
        Attack = 1
      }
    });
    RegisterCard(2, new CardData()
    {
      TextTitle = "Goblin Scout",
      TextDescription = "",

      Deck = CardData.DeckType.GOBLIN,

      BehaviorPattern = "f;tap:move;battlecross:deploy(self)",

      CardInstanceData = new CardInstanceData()
      {
        Cost = 1,

        Health = 1,
        Attack = 1
      }
    });
    RegisterCard(3, new CardData()
    {
      TextTitle = "Goblin Archer",
      TextDescription = "",

      Deck = CardData.DeckType.GOBLIN,

      BehaviorPattern = "0;start:attack(randomEnemy)",

      CardInstanceData = new CardInstanceData()
      {
        Cost = 2,

        Health = 1,
        Attack = 1
      }
    });
    RegisterCard(6, new CardData()
    {
      TextTitle = "Goblin Warrior",
      TextDescription = "",

      Deck = CardData.DeckType.GOBLIN,

      BehaviorPattern = "f;tap:buff(1,0,allSurrounding:ally)",

      CardInstanceData = new CardInstanceData()
      {
        Cost = 3,

        Health = 5,
        Attack = 1
      }
    });


    RegisterCard(19, new CardData()
    {
      TextTitle = "Footsoldier",
      TextDescription = "",

      Deck = CardData.DeckType.KNIGHT,

      BehaviorPattern = "f;tap:move",

      CardInstanceData = new CardInstanceData()
      {
        Cost = 1,

        Health = 1,
        Attack = 1
      }
    });
    RegisterCard(20, new CardData()
    {
      TextTitle = "Guard",
      TextDescription = "",

      Deck = CardData.DeckType.KNIGHT,

      BehaviorPattern = "f;tap:buff(0,1,allSurrounding:ally)",

      CardInstanceData = new CardInstanceData()
      {
        Cost = 2,

        Health = 3,
        Attack = 1
      }
    });
    RegisterCard(22, new CardData()
    {
      TextTitle = "Spearman",
      TextDescription = "",

      Deck = CardData.DeckType.KNIGHT,

      BehaviorPattern = "f;range(1)",

      CardInstanceData = new CardInstanceData()
      {
        Cost = 2,

        Health = 3,
        Attack = 1
      }
    });

    RegisterCard(28, new CardData()
    {
      TextTitle = "Supply Crate",
      TextDescription = "",

      Deck = CardData.DeckType.KNIGHT,

      BehaviorPattern = "object:buff(1,1,self)",

      CardInstanceData = new CardInstanceData()
      {
        Cost = 1,

        Health = 0,
        Attack = 0
      }
    });

    RegisterCard(34, new CardData()
    {
      TextTitle = "Prepare",
      TextDescription = "",

      Deck = CardData.DeckType.KNIGHT,

      BehaviorPattern = "spell:buff(0,3,self)",

      CardInstanceData = new CardInstanceData()
      {
        Cost = 1,

        Health = 0,
        Attack = 0
      }
    });

    //
  }

  //
  public static int GetCardIdByName(string name)
  {
    foreach (var cardData in s_Singleton._cardData)
      if (cardData.Value.TextTitle.ToLower() == name.ToLower())
        return cardData.Value.CardId;
    return 1;
  }

  //
  public static void PlayCardAt(int ownerId, CardData cardData, Vector2Int atPos)
  {


    // Check spell
    if (cardData.IsSpell)
    {
      ObjectController.TrySpell(ObjectController.GetCardObject(atPos), cardData);
    }

    // Unit
    else
    {
      new ObjectController.CardObject(ownerId, atPos, cardData);
    }

  }

  //
  public static GameObject SpawnCardBase(int cardId, Transform parent, Vector3 spawnPosition)
  {
    var cardBase = GameObject.Instantiate(s_Singleton._CardBase);
    (cardBase.transform as RectTransform).SetParent(parent);

    // Set flavor texts
    SetCardBaseData(cardBase, cardId);

    cardBase.transform.position = spawnPosition;
    cardBase.SetActive(true);

    return cardBase;
  }
  public static void SetCardBaseData(GameObject cardBase, int cardId)
  {
    var cardData = GetCardData(cardId);

    SetCardBaseData(cardBase, cardData);
  }
  public static void SetCardBaseData(GameObject cardBase, CardData cardData)
  {
    cardBase.name = $"{cardData.CardId}";

    var cardBaseRoot = cardBase.transform.GetChild(0);
    if (cardBaseRoot.name != "Border")
      cardBaseRoot = cardBase.transform.GetChild(1);

    // Flavor text
    cardBaseRoot.GetChild(1).GetChild(0).GetChild(1).GetComponent<TMPro.TextMeshProUGUI>().text = cardData.TextTitle;
    cardBaseRoot.GetChild(1).GetChild(2).GetChild(1).GetComponent<TMPro.TextMeshProUGUI>().text = cardData.BehaviorPattern;//cardData.TextDescription;

    // Img
    cardBaseRoot.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetComponent<Image>().sprite = Resources.Load<Sprite>($"CardImages/{cardData.CardId}");

    // Cost
    cardBaseRoot.GetChild(2).GetChild(2).GetComponent<TMPro.TextMeshProUGUI>().text = $"{cardData.CardInstanceData.Cost}";

    // Attack / health
    cardBaseRoot.GetChild(3).GetChild(2).GetComponent<TMPro.TextMeshProUGUI>().text = $"{cardData.CardInstanceData.Attack} / {cardData.CardInstanceData.Health}";

    // Mana color
    cardBaseRoot.GetChild(2).GetChild(1).GetComponent<Image>().color = Color.magenta;

    // Card color
    var cardColor = GetDeckColor(cardData.Deck);
    cardBaseRoot.GetChild(0).GetComponent<Image>().color = cardColor;
    cardBaseRoot.GetChild(1).GetChild(0).GetChild(0).GetComponent<Image>().color = cardColor;
    cardBaseRoot.GetChild(1).GetChild(1).GetChild(0).GetComponent<Image>().color = cardColor;
    cardBaseRoot.GetChild(1).GetChild(2).GetComponent<Image>().color = cardColor;
    cardBaseRoot.GetChild(3).GetChild(1).GetComponent<Image>().color = cardColor;
  }

  //
  public static int GetCardManaCost(int ownerId, CardData cardData, Vector2Int atPos)
  {

    var baseMana = cardData.CardInstanceData.Cost;

    // Spell cost
    if (cardData.IsSpell)
      return baseMana;

    // Unit cost + terrain
    var tileManaMod = ownerId == 0 ?
      ObjectController.s_TileMapSize.y - 1 - atPos.y :
      atPos.y;

    // Check object
    if (cardData.IsObject)
      return baseMana + (ObjectController.s_TileMapSize.y / 2 - 1) - tileManaMod;

    // Unit
    return baseMana + tileManaMod;
  }

  //
  public static Color GetDeckColor(CardData.DeckType deckType)
  {
    return deckType switch
    {
      CardData.DeckType.KNIGHT => new Color(0.42f, 0.42f, 0.42f),
      CardData.DeckType.GOBLIN => new Color(0.09f, 0.48f, 0.09f),
      CardData.DeckType.BEAST => new Color(0.31f, 0.2f, 0.2f),

      _ => Color.black,
    };
  }

  //
  [System.Serializable]
  public class CardData
  {

    // Unique card id
    public int CardId;

    // Flavor text
    public string TextTitle, TextDescription;

    //
    public enum DeckType
    {
      NOT_SET,

      BEAST,
      GOBLIN,
      KNIGHT,
    }
    public DeckType Deck;

    /// Card behaviors
    // "m1b1:move-f"
    public string BehaviorPattern;
    public bool IsSpell { get { return BehaviorPattern.Contains("spell:"); } }
    public bool IsObject { get { return BehaviorPattern.Contains("object:"); } }
    public bool HasStartEffect { get { return BehaviorPattern.Contains("start:"); } }
    public bool HasTapEffect { get { return BehaviorPattern.Contains("tap:"); } }
    public bool HasBattleCrossEffect { get { return BehaviorPattern.Contains("battlecross:"); } }
    public bool IsStationary { get { return BehaviorPattern.StartsWith("0"); } }
    public bool HasExtraRange { get { return BehaviorPattern.Contains("range(1)"); } }

    //
    public CardInstanceData CardInstanceData;

    //
    public static CardData Clone(CardData cardData)
    {
      return new()
      {
        CardId = cardData.CardId,

        TextTitle = cardData.TextTitle,
        TextDescription = cardData.TextDescription,

        Deck = cardData.Deck,

        BehaviorPattern = cardData.BehaviorPattern,

        CardInstanceData = new()
        {
          Health = cardData.CardInstanceData.Health,
          Attack = cardData.CardInstanceData.Attack,

          Cost = cardData.CardInstanceData.Cost
        }
      };
    }
  }
  Dictionary<int, CardData> _cardData;

  // Data held by a card that can be differerent per-card
  [System.Serializable]
  public class CardInstanceData
  {

    // Card play cost
    public int Cost;

    //
    public int Health, Attack;

    //
    public Dictionary<string, string> CardEffects;

  }

  //
  public static CardData GetCardData(int cardId)
  {
    return s_Singleton._cardData[cardId];
  }

  //
  public static void PlayCard(int cardId)
  {

  }

  // Each object represents a card
  public struct CardHandData
  {
    public int Id;
    public CardData Data { get { return GetCardData(Id); } }

    public GameObject GameObject;
  }

  //

}
