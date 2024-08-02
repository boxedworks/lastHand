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

      Deck = CardData.CardType.BEAST,

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

      Deck = CardData.CardType.GOBLIN,

      BehaviorPattern = "f,frontline:deploy(1)",

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

      Deck = CardData.CardType.GOBLIN,

      BehaviorPattern = "s,attack(randomEnemy)",

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

      Deck = CardData.CardType.GOBLIN,

      BehaviorPattern = "f,start:buff(allSurrounding:goblin)",

      CardInstanceData = new CardInstanceData()
      {
        Cost = 3,

        Health = 5,
        Attack = 1
      }
    });


    RegisterCard(11, new CardData()
    {
      TextTitle = "Footsoldier",
      TextDescription = "",

      Deck = CardData.CardType.KNIGHT,

      BehaviorPattern = "f",

      CardInstanceData = new CardInstanceData()
      {
        Cost = 1,

        Health = 1,
        Attack = 1
      }
    });
    RegisterCard(12, new CardData()
    {
      TextTitle = "Guard",
      TextDescription = "",

      Deck = CardData.CardType.KNIGHT,

      BehaviorPattern = "f",

      CardInstanceData = new CardInstanceData()
      {
        Cost = 2,

        Health = 3,
        Attack = 1
      }
    });
    RegisterCard(19, new CardData()
    {
      TextTitle = "Prepare",
      TextDescription = "",

      Deck = CardData.CardType.KNIGHT,

      BehaviorPattern = "spell,target:health(+3)",

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

    // Flavor text
    cardBase.transform.GetChild(0).GetChild(1).GetChild(0).GetChild(1).GetComponent<TMPro.TextMeshProUGUI>().text = cardData.TextTitle;
    cardBase.transform.GetChild(0).GetChild(1).GetChild(2).GetChild(1).GetComponent<TMPro.TextMeshProUGUI>().text = cardData.TextDescription;

    // Img
    cardBase.transform.GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetComponent<Image>().sprite = Resources.Load<Sprite>($"CardImages/{cardData.CardId}");

    // Cost
    cardBase.transform.GetChild(0).GetChild(2).GetChild(2).GetComponent<TMPro.TextMeshProUGUI>().text = $"{cardData.CardInstanceData.Cost}";

    // Attack / health
    cardBase.transform.GetChild(0).GetChild(3).GetChild(2).GetComponent<TMPro.TextMeshProUGUI>().text = $"{cardData.CardInstanceData.Attack} / {cardData.CardInstanceData.Health}";
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
    public enum CardType
    {
      NOT_SET,

      BEAST,
      GOBLIN,
      KNIGHT,
    }
    public CardType Deck;

    /// Card behaviors
    // "m1b1:move-f"
    public string BehaviorPattern;
    public bool IsSpell { get { return BehaviorPattern.Contains("spell,"); } }
    public bool HasStartEffect { get { return BehaviorPattern.Contains("start:"); } }

    //
    public CardInstanceData CardInstanceData;
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
    public CardData Data { get { return CardController.GetCardData(Id); } }

    public GameObject GameObject;
  }

  //

}
