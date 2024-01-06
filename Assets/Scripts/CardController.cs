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
    _CardBase = GameObject.Find("CardBase");
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
      Cost = 1
    });
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
}
