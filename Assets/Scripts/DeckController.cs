using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class DeckController
{

  PlayerController.OwnerController _ownerController;

  public RectTransform _DeckIcon, _DiscardIcon, _BurnIcon, _DeckViewer, _CardViewer;
  TMPro.TextMeshProUGUI _textDeckCount, _textDiscardCount, _textBurnCount, _textManaDisplay;

  RectTransform _cardFx_deckSelect, _cardFx_discardSelect, _cardFx_burnSelect, _cardFx_deckViewerSelect, _cardFx_deckViewerBg, _cardFx_cardViewerBg;

  List<CardController.CardHandData> _cardsAll, _cardsDeck, _cardsDiscard, _cardsBurn, _animatingCardsReshuffle;

  List<GameObject> _animatingCardsDiscard, _animatingCardsBurn;

  public bool _GameInteractive { get { return !_displayingDeck && !_displayingCard; } }
  public DeckController(PlayerController.OwnerController ownerController)
  {
    _ownerController = ownerController;

    _cardsAll = new();
    _cardsDeck = new();
    _cardsDiscard = new();
    _cardsBurn = new();

    _animatingCardsDiscard = new();
    _animatingCardsReshuffle = new();
    _animatingCardsBurn = new();

    // UI
    if (_ownerController._OwnerId == 1)
    {
      var deckColor = CardController.GetDeckColor(CardController.CardData.DeckType.KNIGHT);

      _DeckIcon = GameObject.Find("PlayerDeck").transform.Find("CardBase") as RectTransform;
      _DeckIcon.GetChild(0).GetChild(0).GetComponent<Image>().color = deckColor;
      _DeckIcon.GetChild(0).GetChild(1).GetChild(1).GetComponent<Image>().color = deckColor;
      _cardFx_deckSelect = _DeckIcon.parent.GetChild(0).GetChild(0) as RectTransform;
      _textDeckCount = _DeckIcon.GetChild(0).GetChild(1).GetChild(2).GetComponent<TMPro.TextMeshProUGUI>();
      _textDeckCount.text = $"{_cardsDeck.Count}";

      _DiscardIcon = GameObject.Find("PlayerDiscard").transform.Find("CardBase") as RectTransform;
      _DiscardIcon.GetChild(0).GetChild(0).GetComponent<Image>().color = deckColor;
      _DiscardIcon.GetChild(0).GetChild(1).GetChild(1).GetComponent<Image>().color = deckColor;
      _cardFx_discardSelect = _DiscardIcon.parent.GetChild(0).GetChild(0) as RectTransform;
      _textDiscardCount = _DiscardIcon.GetChild(0).GetChild(1).GetChild(2).GetComponent<TMPro.TextMeshProUGUI>();
      _textDiscardCount.text = $"{_cardsDiscard.Count}";

      _BurnIcon = GameObject.Find("PlayerBurn").transform.Find("CardBase") as RectTransform;
      //_BurnIcon.GetChild(0).GetChild(0).GetComponent<Image>().color = deckColor;
      //_BurnIcon.GetChild(0).GetChild(1).GetChild(1).GetComponent<Image>().color = deckColor;
      _cardFx_burnSelect = _BurnIcon.parent.GetChild(0).GetChild(0) as RectTransform;
      _textBurnCount = _BurnIcon.GetChild(0).GetChild(1).GetChild(2).GetComponent<TMPro.TextMeshProUGUI>();
      _textBurnCount.text = $"{_cardsBurn.Count}";

      _DeckViewer = GameObject.Find("DeckViewer").transform as RectTransform;
      _cardFx_deckViewerBg = _DeckViewer.GetChild(0).GetChild(0) as RectTransform;
      _cardFx_deckViewerSelect = _DeckViewer.GetChild(0).GetChild(1) as RectTransform;

      _CardViewer = GameObject.Find("CardViewer").transform as RectTransform;
      _cardFx_cardViewerBg = _CardViewer.GetChild(0).GetChild(0) as RectTransform;

      _textManaDisplay = GameObject.Find("ManaDisplay").transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<TMPro.TextMeshProUGUI>();
    }

    // Create simple deck
    for (var i = 0; i < 4; i++)
      _cardsDeck.Add(new CardController.CardHandData(CardController.GetCardIdByName("footsoldier")));
    for (var i = 0; i < 2; i++)
      _cardsDeck.Add(new CardController.CardHandData(CardController.GetCardIdByName("guard")));
    for (var i = 0; i < 1; i++)
      _cardsDeck.Add(new CardController.CardHandData(CardController.GetCardIdByName("spearman")));
    /*for (var i = 0; i < 1; i++)
      _cardsDeck.Add(new CardController.CardHandData()
      {
        Id = 13
      });*/
    for (var i = 0; i < 2; i++)
      _cardsDeck.Add(new CardController.CardHandData(CardController.GetCardIdByName("prepare")));
    for (var i = 0; i < 1; i++)
      _cardsDeck.Add(new CardController.CardHandData(CardController.GetCardIdByName("supply crate")));
    for (var i = 0; i < 1; i++)
      _cardsDeck.Add(new CardController.CardHandData(CardController.GetCardIdByName("whetstone")));
    for (var i = 0; i < 1; i++)
      _cardsDeck.Add(new CardController.CardHandData(CardController.GetCardIdByName("retreat")));

    if (_textDeckCount)
      _textDeckCount.text = $"{_cardsDeck.Count}";

    ShuffleDeck();
  }

  public void Update()
  {

    // Move discarded cards to discard pile
    for (var i = _animatingCardsDiscard.Count - 1; i >= 0; i--)
    {
      var cardObject = _animatingCardsDiscard[i];

      var desiredPosition = _DiscardIcon.position;
      cardObject.transform.position += (desiredPosition - cardObject.transform.position) * Time.deltaTime * 5f;

      if ((desiredPosition - cardObject.transform.position).magnitude < 20f)
      {
        GameObject.Destroy(cardObject);
        _animatingCardsDiscard.RemoveAt(i);
      }
    }

    // Move burn cards to burn pile
    for (var i = _animatingCardsBurn.Count - 1; i >= 0; i--)
    {
      var cardObject = _animatingCardsBurn[i];

      var desiredPosition = _BurnIcon.position;
      cardObject.transform.position += (desiredPosition - cardObject.transform.position) * Time.deltaTime * 5f;

      if ((desiredPosition - cardObject.transform.position).magnitude < 20f)
      {
        GameObject.Destroy(cardObject);
        _animatingCardsBurn.RemoveAt(i);
      }
    }

    // Reshuffle card animation
    if (_reshuffling)
    {
      if (_reshuffleAmount > 0 && Time.time - _lastReshuffleTime > 0.1f)
      {

        _lastReshuffleTime = Time.time;

        var cardDiscarded = _cardsDiscard[_reshuffleAmount - 1];
        _cardsDiscard.RemoveAt(_reshuffleAmount - 1);
        _textDiscardCount.text = $"{_cardsDiscard.Count}";

        var cardBase = CardController.SpawnCardBase(
          cardDiscarded.CardData,
          _DeckIcon.parent,
          _DiscardIcon.position
        );

        _animatingCardsReshuffle.Add(new CardController.CardHandData(cardDiscarded.CardData)
        {
          GameObject = cardBase
        });
        _reshuffleAmount--;
      }

      // Move reshuffle cards to draw pile
      for (var i = _animatingCardsReshuffle.Count - 1; i >= 0; i--)
      {
        var cardObject = _animatingCardsReshuffle[i];

        var desiredPosition = _DeckIcon.position;
        cardObject.GameObject.transform.position += (desiredPosition - cardObject.GameObject.transform.position) * Time.deltaTime * 12f;

        if ((desiredPosition - cardObject.GameObject.transform.position).magnitude < 20f)
        {
          GameObject.Destroy(cardObject.GameObject);
          _animatingCardsReshuffle.RemoveAt(i);

          _cardsDeck.Add(new CardController.CardHandData(cardObject.CardData));
          _textDeckCount.text = $"{_cardsDeck.Count}";
        }
      }

      //
      if (_reshuffleAmount == 0 && _animatingCardsReshuffle.Count == 0)
      {
        _reshuffling = false;

        ShuffleDeck();
        DrawCard();
      }
    }

    // Check card display
    if (_displayingCard)
    {
      if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1))
      {
        HideCardViewer();
      }
    }
    else
    {

      // Check deck display
      if (_displayingDeck)
      {

        // Move deck display
        if (_deckViewerRowHeight <= 3)
          _deckViewerYPosition = _deckViewerRowHeight switch
          {
            1 => -280f,
            2 => -140f,
            _ => 35f
          };
        else
        {
          var scrollDelta = Input.mouseScrollDelta;
          _deckViewerYPosition = Mathf.Clamp(_deckViewerYPosition + scrollDelta.y * -70f, 35f, 35f + (_deckViewerRowHeight - 3) * 320.5f);
        }

        var deckViewer = _DeckViewer.GetChild(1) as RectTransform;
        deckViewer.anchoredPosition += (new Vector2(50f, _deckViewerYPosition) - deckViewer.anchoredPosition) * Time.deltaTime * 5f;

        // Check card select
        CardController.CardHandData cardHover = null;
        foreach (var card in _deckViewerCards)
        {
          if (RectTransformUtility.RectangleContainsScreenPoint(card.GameObject.transform as RectTransform, Input.mousePosition))
          {
            cardHover = card;

            _cardFx_deckViewerSelect.SetParent(card.GameObject.transform);
            _cardFx_deckViewerSelect.SetAsFirstSibling();
            _cardFx_deckViewerSelect.anchoredPosition = Vector2.zero;

            _cardFx_deckViewerSelect.gameObject.SetActive(true);
          }
        }
        if (cardHover == null)
        {
          _cardFx_deckViewerSelect.SetParent(_DeckViewer.GetChild(0));
          _cardFx_deckViewerSelect.gameObject.SetActive(false);
        }

        // Check hide display
        if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1))
        {

          // View card details
          if (cardHover != null)
          {
            ShowCardViewer(cardHover.CardData);
          }

          // Hide deck display
          else
          {
            HideDisplayDeck();
          }
        }
      }

      // Check deck display
      else
      {

        // Display deck
        if (RectTransformUtility.RectangleContainsScreenPoint(_DeckIcon, Input.mousePosition))
        {
          _cardFx_deckSelect.gameObject.SetActive(true);

          if (Input.GetMouseButtonUp(0))
            ShowDisplayDeck(DeckDisplayType.DECK);
        }
        else
          _cardFx_deckSelect.gameObject.SetActive(false);

        // Display discard
        if (RectTransformUtility.RectangleContainsScreenPoint(_DiscardIcon, Input.mousePosition))
        {
          _cardFx_discardSelect.gameObject.SetActive(true);

          if (Input.GetMouseButtonUp(0))
            ShowDisplayDeck(DeckDisplayType.DISCARD);
        }
        else
          _cardFx_discardSelect.gameObject.SetActive(false);

        // Display burn
        if (RectTransformUtility.RectangleContainsScreenPoint(_BurnIcon, Input.mousePosition))
        {
          _cardFx_burnSelect.gameObject.SetActive(true);

          if (Input.GetMouseButtonUp(0))
            ShowDisplayDeck(DeckDisplayType.BURN);
        }
        else
          _cardFx_burnSelect.gameObject.SetActive(false);
      }
    }

    // Handle input
    {
      if (Input.GetKeyDown(KeyCode.Space))
      {
        DrawCard();
      }
    }
  }

  // Shuffle player's deck
  public void ShuffleDeck()
  {
    _cardsDeck.Shuffle();
  }

  // Draw a random card from the deck
  bool _reshuffling;
  int _reshuffleAmount;
  float _lastReshuffleTime;
  public void DrawCard()
  {

    // Check reshuffling
    if (_reshuffling)
    {
      Debug.Log("Cannot draw. Is reshuffling.....");
      return;
    }

    // Empty deck; reshuffle
    if (_cardsDeck.Count == 0)
    {

      if (_cardsDiscard.Count > 0)
      {
        _reshuffling = true;
        _reshuffleAmount = _cardsDiscard.Count;
      }

      return;
    }

    // Gather next card
    var nextCard = _cardsDeck[0];
    _cardsDeck.RemoveAt(0);

    // Add to player hand
    _ownerController._Hand.AddCard(nextCard.CardData, HandController.CardSpawnSource.DECK);

    // Set deck count UI
    if (_ownerController._OwnerId != 0)
      _textDeckCount.text = $"{_cardsDeck.Count}";
  }

  // Add a card to the discard pile
  public void DiscardCard(CardController.CardHandData cardHandData)
  {

    // Animate card gameObject to discard pile
    if (_ownerController._OwnerId != 0)
      _animatingCardsDiscard.Add(cardHandData.GameObject);

    // Add card data to discard pile
    _cardsDiscard.Add(new CardController.CardHandData(cardHandData.CardData));

    // Increment discard number
    if (_ownerController._OwnerId != 0)
      _textDiscardCount.text = $"{_cardsDiscard.Count}";
  }
  public void BurnCard(CardController.CardHandData cardHandData)
  {

    // Animate card gameObject to discard pile
    if (_ownerController._OwnerId != 0)
      _animatingCardsBurn.Add(cardHandData.GameObject);

    // Add card data to discard pile
    _cardsBurn.Add(new CardController.CardHandData(cardHandData.CardData));

    // Increment discard number
    if (_ownerController._OwnerId != 0)
      _textBurnCount.text = $"{_cardsBurn.Count}";
  }

  //
  public static void ShowCardObjectData(int cardIndex, CardController.CardData cardData)
  {
    var cardBase = GameObject.Find("CardObjectDisplay").transform.GetChild(cardIndex).gameObject;

    CardController.SetCardBaseData(cardBase, cardData);
    cardBase.gameObject.SetActive(true);
  }
  public static void HideCardObjectData(int cardIndex)
  {
    var cardBase = GameObject.Find("CardObjectDisplay").transform.GetChild(cardIndex).gameObject;
    cardBase.gameObject.SetActive(false);
  }

  //
  bool _displayingDeck;
  float _deckViewerYPosition;
  int _deckViewerRowHeight;
  List<CardController.CardHandData> _deckViewerCards;
  public enum DeckDisplayType
  {
    DECK,
    DISCARD,
    BURN
  }
  void ShowDisplayDeck(DeckDisplayType deckDisplayType)
  {
    var cards = deckDisplayType switch
    {
      DeckDisplayType.DISCARD => _cardsDiscard,
      DeckDisplayType.BURN => _cardsBurn,

      _ => _cardsDeck
    };
    if (cards.Count == 0)
      return;

    // Order cards by ID if showing deck
    if (deckDisplayType == DeckDisplayType.DECK)
    {
      cards = cards.OrderBy(x => -x.Id).ToList();
    }

    // Create card displays
    _displayingDeck = true;
    _cardFx_deckViewerBg.gameObject.SetActive(true);

    _deckViewerCards = new();

    var cardBaseOriginal = _DeckViewer.GetChild(1).GetChild(0).GetChild(0);

    var cardRowAmount = 0;
    _deckViewerRowHeight = 0;

    for (var i = 0; i < cards.Count; i++)
    {

      // Create a new card
      var cardBase = GameObject.Instantiate(cardBaseOriginal.gameObject);
      CardController.SetCardBaseData(cardBase, cards[i].CardData);

      //
      if (cardRowAmount == 7 || i == 0)
      {
        cardRowAmount = 0;
        _deckViewerRowHeight++;

        var rowNew = GameObject.Instantiate(_DeckViewer.GetChild(1).GetChild(0).gameObject);
        (rowNew.transform as RectTransform).SetParent(_DeckViewer.GetChild(1));

        GameObject.Destroy(rowNew.transform.GetChild(0).gameObject);
        rowNew.SetActive(true);
      }

      //
      (cardBase.transform as RectTransform).SetParent(_DeckViewer.GetChild(1).GetChild(_deckViewerRowHeight));
      cardRowAmount++;

      _deckViewerCards.Add(new CardController.CardHandData(cards[i].CardData)
      {
        GameObject = cardBase
      });

      //
      cardBase.SetActive(true);
    }

  }
  void HideDisplayDeck()
  {
    _displayingDeck = false;
    _cardFx_deckViewerBg.gameObject.SetActive(false);

    for (var i = _deckViewerRowHeight; i > 0; i--)
    {
      GameObject.Destroy(_DeckViewer.GetChild(1).GetChild(i).gameObject);
    }
  }

  //
  bool _displayingCard;
  public void ShowCardViewer(CardController.CardData cardData)
  {
    _displayingCard = true;
    _cardFx_cardViewerBg.gameObject.SetActive(true);

    var cardBase = _CardViewer.GetChild(1).GetChild(0);
    CardController.SetCardBaseData(cardBase.gameObject, cardData);
    cardBase.transform.parent.gameObject.SetActive(true);
  }
  void HideCardViewer()
  {
    _displayingCard = false;
    _cardFx_cardViewerBg.gameObject.SetActive(false);

    var cardBase = _CardViewer.GetChild(1).GetChild(0);
    cardBase.transform.parent.gameObject.SetActive(false);
  }

  //
  public void UpdateManaDisplay()
  {
    _textManaDisplay.text = $"{_ownerController._Mana}";
  }
}