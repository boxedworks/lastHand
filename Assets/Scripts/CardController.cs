using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    RegisterCard(new CardData()
    {
      TextTitle = "Fireball",
      TextDescription = "Target takes 5 damage.",
      Cost = 2
    });
    RegisterCard(new CardData()
    {
      TextTitle = "Sear",
      TextDescription = "Target takes 1 damage.",
      Cost = 1
    });
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

    cardBase.name = $"{cardId}";

    cardBase.transform.GetChild(0).GetChild(1).GetChild(0).GetChild(1).GetComponent<TMPro.TextMeshProUGUI>().text = cardData.TextTitle;
    cardBase.transform.GetChild(0).GetChild(1).GetChild(2).GetChild(1).GetComponent<TMPro.TextMeshProUGUI>().text = cardData.TextDescription;
    cardBase.transform.GetChild(0).GetChild(2).GetChild(2).GetComponent<TMPro.TextMeshProUGUI>().text = $"{cardData.Cost}";
  }

  //
  [System.Serializable]
  public struct CardData
  {
    public int CardId;

    public string TextTitle, TextDescription;
    public int Cost;
  }
  Dictionary<int, CardData> _cardData;

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
}
