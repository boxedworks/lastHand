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
    void RegisterCard(CardData cardData)
    {
      var cardId = _cardData.Count;
      cardData.CardId = cardId;
      _cardData.Add(cardId, cardData);
    }

    // Test cards
    RegisterCard(new CardData()
    {
      TextTitle = "Bat",
      TextDescription = "",

      BehaviorPattern = "f",

      CardInstanceData = new CardInstanceData()
      {
        Cost = 1,

        Health = 1,
        Attack = 1
      }
    });

    RegisterCard(new CardData()
    {
      TextTitle = "Sear",
      TextDescription = "Target takes 1 damage.",

      CardInstanceData = new CardInstanceData()
      {
        Cost = 1,

        Attack = 1
      }
    });
    RegisterCard(new CardData()
    {
      TextTitle = "Fireball",
      TextDescription = "Target takes 5 damage.",

      CardInstanceData = new CardInstanceData()
      {
        Cost = 2,

        Attack = 5
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
  public struct CardData
  {

    // Unique card id
    public int CardId;

    // Flavor text
    public string TextTitle, TextDescription;

    /// Card behaviors
    // "m1b1:move-f"
    public string BehaviorPattern;

    //
    public CardInstanceData CardInstanceData;
  }
  Dictionary<int, CardData> _cardData;

  // Data held by a card that can be differerent per-card
  [System.Serializable]
  public struct CardInstanceData
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
