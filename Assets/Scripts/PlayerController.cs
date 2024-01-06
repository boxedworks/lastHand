using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

  class PlayerHand
  {

    int _selectedCardIndex;
    struct CardData
    {
      public int CardId;
      public GameObject CardObject;
    }

    //
    List<CardData> _cards;
    public PlayerHand()
    {
      _cards = new();

      _selectedCardIndex = -1;
    }

    //
    public void Update()
    {

      // Handle input
      if (Input.GetKeyDown(KeyCode.Space))
      {
        CreateCard(0);
      }

      var mousePos = Input.mousePosition;
      var mousePosTranslated = mousePos.x - 1920f * 0.5f;

      // Set card positions
      var numCards = _cards.Count;
      var cardOffsetWidth = Mathf.Clamp(150f - 4f * numCards, 50f, 150f);
      var selectedCardIndex = _selectedCardIndex;
      _selectedCardIndex = -1;
      for (var i = 0; i < _cards.Count; i++)
      {
        var cardStruct = _cards[i];

        var cardId = cardStruct.CardId;
        var cardObject = cardStruct.CardObject;

        var cardOffset = cardOffsetWidth * i - cardOffsetWidth * 0.5f * (numCards - 1);
        var desiredCardPosition = new Vector3(cardOffset, -Mathf.Abs(cardOffset) * 0.2f, 0f);
        var desiredCardRotation = new Vector3(0f, 0f, -desiredCardPosition.x * 0.05f);

        // Check selected
        if (
          mousePos.y < (selectedCardIndex == i ? 400f : 250f) &&
          mousePosTranslated >= desiredCardPosition.x - cardOffsetWidth * 0.5f && mousePosTranslated <= desiredCardPosition.x + cardOffsetWidth * 0.5f
        )
          _selectedCardIndex = i;
        if (_selectedCardIndex == i)
        {
          desiredCardPosition += new Vector3(0f, 150f, 0f);
          desiredCardRotation = Vector3.zero;

          (cardObject.transform as RectTransform).SetSiblingIndex(numCards);
        }
        else
          (cardObject.transform as RectTransform).SetSiblingIndex(i);

        // Set pos / rot
        cardObject.transform.localPosition += (desiredCardPosition - cardObject.transform.localPosition) * Time.deltaTime * 5f;
        cardObject.transform.rotation = Quaternion.Lerp(cardObject.transform.rotation, Quaternion.Euler(desiredCardRotation), Time.deltaTime * 5f);
      }

    }

    //
    void CreateCard(int cardId)
    {
      var cardBase = GameObject.Instantiate(CardController.s_Singleton._CardBase);
      cardBase.transform.parent = CardController.s_Singleton._CardBase.transform.parent;

      // Set flavor texts
      var cardData = CardController.GetCardData(cardId);

      cardBase.transform.GetChild(0).GetChild(1).GetChild(0).GetChild(1).GetComponent<TMPro.TextMeshProUGUI>().text = cardData.TextTitle;
      cardBase.transform.GetChild(0).GetChild(1).GetChild(2).GetChild(1).GetComponent<TMPro.TextMeshProUGUI>().text = cardData.TextDescription;
      cardBase.transform.GetChild(0).GetChild(2).GetChild(2).GetComponent<TMPro.TextMeshProUGUI>().text = $"{cardData.Cost}";

      cardBase.transform.position = Vector3.zero;
      cardBase.SetActive(true);

      //
      _cards.Add(new CardData()
      {
        CardId = cardId,
        CardObject = cardBase
      });
    }

  }
  PlayerHand _hand;

  // Start is called before the first frame update
  void Start()
  {
    _hand = new();
  }

  // Update is called once per frame
  void Update()
  {

    // Handle card positions
    _hand.Update();
  }
}
