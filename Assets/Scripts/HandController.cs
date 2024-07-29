using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Handles / Holds information about a Player's cards
public class HandController
{

  //
  PlayerController _playerController;

  // Card FX
  static Transform _cardFx_Container;
  static RectTransform _cardFx_Selected;

  // Each object references a card in the player's hand
  struct SimpleCardReference
  {
    public int HandIndex;
    public bool HasCard { get { return HandIndex > -1; } }
    public CardController.CardData CardData { get { return cardHandData.Data; } }
    public GameObject GameObject { get { return cardHandData.GameObject; } }

    HandController playerHand;
    CardController.CardHandData cardHandData { get { return playerHand._cards[HandIndex]; } }
    public SimpleCardReference(HandController playerHand)
    {
      HandIndex = -1;

      this.playerHand = playerHand;
    }
  }
  SimpleCardReference _cardFocused, _cardSelected;

  // Create empty player hand
  List<CardController.CardHandData> _cards;
  public HandController(PlayerController playerController)
  {
    _playerController = playerController;

    _cards = new();

    _cardFocused = new(this);
    _cardSelected = new(this);

    //
    _cardFx_Container = GameObject.Find("PlayerHand").transform.GetChild(0);
    _cardFx_Selected = _cardFx_Container.GetChild(0) as RectTransform;
    _cardFx_Selected.gameObject.SetActive(false);
  }

  //
  public void Update()
  {

    var mousePos = Input.mousePosition;
    var mousePosTranslated = mousePos.x - 1920f * 0.5f;

    var mousePosForCardPlay = 350f;

    //
    var cardChildStartOffset = 1;

    // Set card positions
    var numCards = _cards.Count;
    var cardOffsetWidth = Mathf.Clamp(150f - 4f * numCards, 50f, 150f);
    var focusedCardIndex = _cardFocused.HandIndex;
    _cardFocused.HandIndex = -1;
    for (var i = 0; i < _cards.Count; i++)
    {
      var cardStruct = _cards[i];
      var cardObject = cardStruct.GameObject;

      // Maths :(
      var cardOffset = cardOffsetWidth * i - cardOffsetWidth * 0.5f * (numCards - 1);
      var desiredCardPosition = new Vector3(cardOffset, -Mathf.Abs(cardOffset) * 0.2f, 0f);
      var desiredCardRotation = new Vector3(0f, 0f, -desiredCardPosition.x * 0.05f);

      // Check focused card
      if (
        mousePos.y < (focusedCardIndex == i ? 400f : 250f) &&
        mousePosTranslated >= desiredCardPosition.x - cardOffsetWidth * 0.5f && mousePosTranslated <= desiredCardPosition.x + cardOffsetWidth * 0.5f
      )
        _cardFocused.HandIndex = i;
      if ((_cardFocused.HandIndex == i && !_cardSelected.HasCard) || _cardSelected.HandIndex == i)
      {
        desiredCardPosition.y = 60f;
        desiredCardRotation = Vector3.zero;

        (cardObject.transform as RectTransform).SetSiblingIndex(cardChildStartOffset + numCards);
      }
      else
        (cardObject.transform as RectTransform).SetSiblingIndex(cardChildStartOffset + i);

      // Card about to be played
      if (_cardSelected.HandIndex == i)
      {

        if (mousePos.y > mousePosForCardPlay)
        {
          desiredCardPosition.y = 120f;

          _cardFx_Selected.SetParent(_cardSelected.GameObject.transform);
          _cardFx_Selected.SetSiblingIndex(0);
          _cardFx_Selected.localRotation = Quaternion.identity;
          _cardFx_Selected.localPosition = Vector3.zero;
          _cardFx_Selected.gameObject.SetActive(true);
        }
        else
        {
          _cardFx_Selected.gameObject.SetActive(false);
        }
      }

      // Set pos / rot
      cardObject.transform.localPosition += (desiredCardPosition - cardObject.transform.localPosition) * Time.deltaTime * 5f;
      cardObject.transform.rotation = Quaternion.Lerp(cardObject.transform.rotation, Quaternion.Euler(desiredCardRotation), Time.deltaTime * 5f);
    }

    // Handle input
    {

      if (_cardFocused.HasCard)
      {
        // Select focused card
        if (Input.GetMouseButtonDown(0))
        {
          OnCardSelected();
        }

        // View focused card
        if (Input.GetMouseButtonUp(1))
        {
          _playerController._Deck.ShowCardViewer(_cardFocused.CardData.CardId);
        }
      }

      // Play selected card
      if (_cardSelected.HasCard)
        if (Input.GetMouseButtonUp(0))
        {
          if (mousePos.y > mousePosForCardPlay)
            OnCardPlayed();
          else
          {
            _cardSelected.HandIndex = -1;
          }
        }

    }
  }

  //
  void OnCardSelected()
  {
    _cardSelected.HandIndex = _cardFocused.HandIndex;

    Debug.Log($"Card selected: {_cardSelected.CardData.TextTitle}");
  }

  void OnCardPlayed()
  {
    Debug.Log($"Card played: {_cardSelected.CardData.TextTitle}");

    if (_cardFx_Selected.parent == _cardSelected.GameObject.transform)
      _cardFx_Selected.SetParent(_cardFx_Container);

    RemoveCard(_cardSelected.HandIndex);
    _cardSelected.HandIndex = -1;

    _cardFx_Selected.gameObject.SetActive(false);
  }

  // Add a card to the player's hand by Id
  public void AddCard(int cardId)
  {
    var cardBase = CardController.SpawnCardBase(
      cardId,
      CardController.s_Singleton._CardBase.transform.parent,
      _playerController._Deck._DeckIcon.position
    );

    //
    _cards.Add(new CardController.CardHandData()
    {
      Id = cardId,
      GameObject = cardBase
    });
  }

  // Remove a card from the player's hand by hand index and place into the discard pile
  void RemoveCard(int handIndex)
  {

    var card = _cards[handIndex];
    _cards.RemoveAt(handIndex);

    _playerController._Deck.DiscardCard(card);
  }

}
