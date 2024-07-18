using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

  // Handles / Holds information about a Player's cards
  class PlayerHand
  {

    // Card FX
    static RectTransform _cardFx_Selected;

    // Each object references a card in the player's hand
    struct SimpleCardReference
    {
      public int HandIndex;
      public bool HasCard { get { return HandIndex > -1; } }
      public CardController.CardData CardData { get { return cardHandData.Data; } }
      public GameObject GameObject { get { return cardHandData.GameObject; } }

      PlayerHand playerHand;
      CardHandData cardHandData { get { return playerHand._cards[HandIndex]; } }
      public SimpleCardReference(PlayerHand playerHand)
      {
        HandIndex = -1;

        this.playerHand = playerHand;
      }
    }
    SimpleCardReference _cardFocused, _cardSelected;

    // Each object represents a card in the player's hand
    struct CardHandData
    {
      public int Id;
      public CardController.CardData Data { get { return CardController.GetCardData(Id); } }

      public GameObject GameObject;
    }

    // Create empty player hand
    List<CardHandData> _cards;
    public PlayerHand()
    {
      _cards = new();

      _cardFocused = new(this);
      _cardSelected = new(this);

      //
      _cardFx_Selected = GameObject.Find("CFX_Selected").transform as RectTransform;
      _cardFx_Selected.gameObject.SetActive(false);
    }

    //
    public void Update()
    {

      var mousePos = Input.mousePosition;
      var mousePosTranslated = mousePos.x - 1920f * 0.5f;

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
          desiredCardPosition += new Vector3(0f, 60f, 0f);
          desiredCardRotation = Vector3.zero;

          (cardObject.transform as RectTransform).SetSiblingIndex(cardChildStartOffset + numCards);
        }
        else
          (cardObject.transform as RectTransform).SetSiblingIndex(cardChildStartOffset + i);

        // Card about to be played
        if (_cardSelected.HandIndex == i)
        {

          if (mousePos.y > 250f)
          {
            desiredCardPosition += new Vector3(0f, 80f, 0f);

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
        if (Input.GetKeyDown(KeyCode.Space))
        {
          AddCard(0);
        }

        if (_cardFocused.HasCard)
          if (Input.GetMouseButtonDown(0))
          {
            _cardSelected.HandIndex = _cardFocused.HandIndex;
            OnCardSelected();
          }

        if (_cardSelected.HasCard)
          if (Input.GetMouseButtonUp(0))
          {
            OnCardPlayed();
            _cardSelected.HandIndex = -1;

            _cardFx_Selected.gameObject.SetActive(false);
          }
      }
    }

    //
    void OnCardSelected()
    {
      Debug.Log($"Card selected: {_cardSelected.CardData.TextTitle}");
    }

    void OnCardPlayed()
    {
      Debug.Log($"Card played: {_cardSelected.CardData.TextTitle}");
    }

    // Add a card to the player's hand by Id
    void AddCard(int cardId)
    {
      var cardBase = GameObject.Instantiate(CardController.s_Singleton._CardBase);
      (cardBase.transform as RectTransform).SetParent(CardController.s_Singleton._CardBase.transform.parent);

      // Set flavor texts
      var cardData = CardController.GetCardData(cardId);

      cardBase.transform.GetChild(0).GetChild(1).GetChild(0).GetChild(1).GetComponent<TMPro.TextMeshProUGUI>().text = cardData.TextTitle;
      cardBase.transform.GetChild(0).GetChild(1).GetChild(2).GetChild(1).GetComponent<TMPro.TextMeshProUGUI>().text = cardData.TextDescription;
      cardBase.transform.GetChild(0).GetChild(2).GetChild(2).GetComponent<TMPro.TextMeshProUGUI>().text = $"{cardData.Cost}";

      cardBase.transform.position = Vector3.zero;
      cardBase.SetActive(true);

      //
      _cards.Add(new CardHandData()
      {
        Id = cardId,
        GameObject = cardBase
      });
    }

  }
  PlayerHand _hand;

  //
  class CardDeck
  {



  }

  //
  TilemapController _tilemapController;
  class TilemapController
  {

    Vector2Int _tileHovered, _tileSelected;

    public TilemapController()
    {
      _tileHovered = _tileSelected = new Vector2Int(-1, -1);
    }

    //
    public void Update()
    {
      //
      //Camera.main.ui
      var mousepos = Input.mousePosition;
      var mouseray = Camera.main.ScreenPointToRay(mousepos);
      var raycasthit = new RaycastHit();
      if (Physics.Raycast(mouseray, out raycasthit, 100f, LayerMask.GetMask("Default")))
      {
        var hitpoint = raycasthit.point;

        //
        var oldHover = _tileHovered;

        // Translate and sanitize selected tile
        var tileMapSize = ObjectController.s_TileMapSize;
        var tilePos = new Vector2Int((int)Mathf.RoundToInt(hitpoint.x / 5f + tileMapSize.x / 2f - 0.5f), (int)Mathf.RoundToInt(hitpoint.z / 5f + tileMapSize.y / 2f - 0.5f));
        if (tilePos.x > -1 && tilePos.x < tileMapSize.x && tilePos.y >= 0 && tilePos.y < tileMapSize.x)
        {

          // New hover
          _tileHovered = tilePos;

          if (_tileHovered != _tileSelected)
          {
            var img = ObjectController.s_Singleton.GetTileMapImage(tilePos);
            img.color = Color.gray;
          }

          // Selection
          if (Input.GetMouseButtonUp(0))
          {

            var oldSelected = _tileSelected;
            _tileSelected = tilePos;

            if (oldSelected.x != -1 && oldSelected != _tileSelected)
            {
              var img = ObjectController.s_Singleton.GetTileMapImage(oldSelected);
              img.color = Color.white;
            }

            {
              var img = ObjectController.s_Singleton.GetTileMapImage(tilePos);
              img.color = Color.green;
            }

            var cardObject = ObjectController.GetCardObject(tilePos);
            if (cardObject != null)
              Debug.Log($"Selected cardObject: {cardObject._Id}");

          }
        }

        // Tile out of bounds
        else
          _tileHovered = new Vector2Int(-1, -1);

        // Old hover
        if (oldHover.x != -1 && oldHover != _tileHovered && oldHover != _tileSelected)
        {
          var img = ObjectController.s_Singleton.GetTileMapImage(oldHover);
          img.color = Color.white;
        }
      }
    }
  }

  // Start is called before the first frame update
  void Start()
  {
    _hand = new();
    _tilemapController = new();
  }

  // Update is called once per frame
  void Update()
  {

    // Handle card positions
    _hand.Update();

    //
    _tilemapController.Update();

    // Move camera
    {

      // Arrow keys
      foreach (var inputPair in new (KeyCode keyCode, Vector3 direction)[] {
      (keyCode: KeyCode.LeftArrow, direction: new Vector3(-1f, 0f, 0f)),
      (keyCode: KeyCode.RightArrow, direction: new Vector3(1f, 0f, 0f)),
      (keyCode: KeyCode.UpArrow, direction: new Vector3(0f, 0f, 1f)),
      (keyCode: KeyCode.DownArrow, direction: new Vector3(0f, 0f, -1f)),
    })
      {
        if (Input.GetKey(inputPair.keyCode))
        {
          Camera.main.transform.position += inputPair.direction * Time.deltaTime * 15f;

          _middleMouseDownPos = Input.mousePosition;
          _cameraSavePos = Camera.main.transform.position;
        }
      }

      // Middle mouse
      if (Input.GetMouseButtonDown(2))
      {
        _middleMouseDownPos = Input.mousePosition;
        _cameraSavePos = Camera.main.transform.position;
      }
      if (Input.GetMouseButton(2))
      {
        var mouseDiff = Input.mousePosition - _middleMouseDownPos;
        Camera.main.transform.position = _cameraSavePos + -new Vector3(mouseDiff.x, 0f, mouseDiff.y) * 0.05f;
      }
    }
  }
  Vector3 _middleMouseDownPos, _cameraSavePos;
}
