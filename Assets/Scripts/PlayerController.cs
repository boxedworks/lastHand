using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

using System.Linq;

public class PlayerController : MonoBehaviour
{
  public static List<PlayerController> s_Players;
  public static PlayerController s_LocalPlayer { get { return s_Players[0]; } }


  public class OwnerController
  {
    public int _OwnerId;

    public HandController _Hand;
    public DeckController _Deck;

    //
    public int _Mana;
    public int _Health;

    //
    public OwnerController(int ownerId)
    {
      _OwnerId = ownerId;
      _Mana = 4;

      _Hand = new(this);
      _Deck = new(this);
    }

    //
    void UpdateHealthUi()
    {
      if (_OwnerId == 0)
        GameObject.Find("SystemHealth").transform.GetChild(1).GetComponent<TMPro.TextMeshPro>().text = $"{_Health}";
      else
        GameObject.Find("PlayerHealth").transform.GetChild(1).GetComponent<TMPro.TextMeshPro>().text = $"{_Health}";
    }
    public void SetHealth(int health)
    {
      _Health = health;
      UpdateHealthUi();
    }

    //
    public void OnCardPlayed(CardController.CardData cardData, Vector2Int atPos)
    {
      _Mana -= CardController.GetCardManaCost(_OwnerId, cardData, atPos);
      if (_OwnerId != 0)
        _Deck.UpdateManaDisplay();
    }

    //
    public void OnTurnEnd()
    {
      if (ObjectController._IsActionsHappening) return;

      GameController.s_Singleton.OnTurnsEnded();

      _Mana = 4;
      _Deck.UpdateManaDisplay();
    }

    //
    public bool OwnerIdIsAlly(int otherOwnerId)
    {
      return _OwnerId == 0 ? otherOwnerId == 0 : otherOwnerId != 0;
    }
    public bool OwnerIdIsAlly(OwnerController otherOwner)
    {
      return OwnerIdIsAlly(otherOwner._OwnerId);
    }
        public bool OwnerIdIsAlly(ObjectController.CardObject otherObject)
    {
      return OwnerIdIsAlly(otherObject._OwnerId);
    }

    //
    public static OwnerController GetOwnerController(int ownerId)
    {
      return ownerId == 0 ? GameController.s_Singleton._EnemyController._OwnerController : s_Players[0]._OwnerController;
    }
  }
  public OwnerController _OwnerController;

  //
  public Vector2Int _TileHovered { get { return TilemapController.s_Singleton._TileHovered; } }

  //
  public class TilemapController
  {

    public static TilemapController s_Singleton;

    public Vector2Int _TileHovered, _TileSelected;

    PlayerController _playerController;

    public TilemapController(PlayerController playerController)
    {
      s_Singleton = this;

      _playerController = playerController;

      _TileHovered = _TileSelected = new Vector2Int(-1, -1);

      _ViewedObjects = new ObjectController.CardObject[2];

      // Create tilemap UI
      var uiElemntsBaseRef = GameObject.Find("TileMapUI").transform.GetChild(0).GetChild(0);
      var uiElementsBase = GameObject.Instantiate(uiElemntsBaseRef.gameObject, uiElemntsBaseRef.parent).transform;
      uiElementsBase.gameObject.SetActive(true);
      for (var x = 1; x < ObjectController.s_TileMapSize.x; x++)
      {
        var uiTile = GameObject.Instantiate(uiElementsBase.GetChild(0).gameObject, uiElementsBase).transform;
      }
      for (var y = 1; y < ObjectController.s_TileMapSize.y; y++)
      {
        var uiElementsRow = GameObject.Instantiate(uiElementsBase.gameObject, uiElementsBase.parent).transform;
      }

      //
      for (var y = 0; y < ObjectController.s_TileMapSize.y; y++)
        for (var x = 0; x < ObjectController.s_TileMapSize.x; x++)
        {
          var tilePos = new Vector2Int(x, y);
          UpdateTileColors();
        }

      //
      ClearBuffTile();
      ClearAttackTile();
      ClearAttackingOwnerIndicator();
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
        var oldHover = _TileHovered;

        // Translate and sanitize selected tile
        var tileMapSize = ObjectController.s_TileMapSize;
        var tilePos = new Vector2Int(
          Mathf.RoundToInt(hitpoint.x / 5f + tileMapSize.x / 2f - 0.5f),
          Mathf.RoundToInt(hitpoint.z / 5f + tileMapSize.y / 2f - 0.5f)
        );

        var xMin = GetTileGameObjectPosition(new Vector2Int(0, 0)).x - ObjectController.s_TileMapGameObjectSize.x / 2f;
        var xMax = GetTileGameObjectPosition(new Vector2Int(ObjectController.s_TileMapSize.x - 1, 0)).x + ObjectController.s_TileMapGameObjectSize.x / 2.1f;

        var yMin = GetTileGameObjectPosition(new Vector2Int(0, 0)).y - ObjectController.s_TileMapGameObjectSize.y / 2f;
        var yMax = GetTileGameObjectPosition(new Vector2Int(0, ObjectController.s_TileMapSize.y - 1)).y + ObjectController.s_TileMapGameObjectSize.y / 2.1f;

        if (
          hitpoint.x > xMin && hitpoint.x < xMax && hitpoint.y > yMin && hitpoint.y < yMax &&
          ObjectController.IsPosWithinTilemap(tilePos)
          )
        {

          // New hover
          var lastHover = _TileHovered;
          _TileHovered = tilePos;

          // Check change
          if (lastHover != _TileHovered)
          {

            // Update hand costs
            _playerController._OwnerController._Hand.UpdateHandManaCosts(_TileHovered);
          }

          //
          if (_TileHovered != _TileSelected)
          {
            var img = ObjectController.GetTileMapImage(tilePos);
            img.color = Color.gray;
          }

          // Selection
          if (!_playerController._OwnerController._Hand._HasSelectedCard && !ObjectController._IsActionsHappening && Input.GetMouseButtonUp(0))
          {

            SelectTile(tilePos);

            //
            var cardObject = ObjectController.GetCardObject(tilePos);
            if (cardObject != null)
            {

              if (_ViewedObjects[0] != cardObject)
              {
                SetViewedObject(0, cardObject);
              }

              // Tap
              else
              {
                if (cardObject._OwnerId == _playerController._OwnerController._OwnerId)
                  ObjectController.TryTap(cardObject);
              }

            }
          }
        }

        // Tile out of bounds
        else
          _TileHovered = new Vector2Int(-1, -1);

        // Old hover
        if (oldHover.x != -1 && oldHover != _TileHovered && oldHover != _TileSelected)
        {
          UpdateTileColors();
        }
      }
    }

    //
    public void SelectTile(Vector2Int tilePos)
    {
      _TileSelected = tilePos;
      UpdateTileColors();
    }

    public void ClearSelectedTile()
    {
      _TileSelected.x = -1;
      UpdateTileColors();
    }

    //
    Vector2Int _attackIndicator;
    public void SetAttackIndicator(Vector2Int tilePos)
    {
      _attackIndicator = tilePos;
      UpdateTileColors();
    }
    public void ClearAttackTile()
    {
      _attackIndicator.x = -1;
      UpdateTileColors();
    }

    //
    Vector2Int _buffIndicator;
    public void SetBuffIndicator(Vector2Int tilePos)
    {
      _buffIndicator = tilePos;
      UpdateTileColors();
    }
    public void ClearBuffTile()
    {
      _buffIndicator.x = -1;
      UpdateTileColors();
    }

    //
    Vector2Int _attackingOwnerPos;
    int _attackingOwnerDir;
    public void SetAttackingOwnerIndicator(Vector2Int tilePos, int ownerId)
    {
      _attackingOwnerPos = tilePos;
      _attackingOwnerDir = ownerId == 0 ? -1 : 1;
      UpdateTileColors();
    }
    public void ClearAttackingOwnerIndicator()
    {
      _attackingOwnerPos.x = -1;
      UpdateTileColors();
    }
    //
    void UpdateTileColor(Vector2Int tilePos)
    {

      var img = ObjectController.GetTileMapImage(tilePos);


      // Check attack
      if (_attackIndicator == tilePos)
        img.color = Color.red;

      // Check attacking owner
      else if (tilePos.x == _attackingOwnerPos.x && (_attackingOwnerDir == 1 ? tilePos.y > _attackingOwnerPos.y : tilePos.y < _attackingOwnerPos.y))
        img.color = Color.red;

      // Check buff
      else if (_buffIndicator == tilePos)
        img.color = Color.yellow;

      // Check selected
      else if (_TileSelected == tilePos)
        img.color = Color.green;

      // Check hover
      else if (_TileHovered == tilePos)
        img.color = Color.gray;

      // None
      else
        img.color = ObjectController.IsDeployTile(tilePos) ? Color.white * 0.75f : Color.white;
    }
    void UpdateTileColors()
    {
      foreach (var pos in ObjectController.s_Singleton._TileMapPositionsAll)
      {
        UpdateTileColor(pos);
      }
    }

    //
    public CardController.CardData _ForcedViewedCardObject0;
    public ObjectController.CardObject[] _ViewedObjects;
    public static void SetViewedObject(int index, ObjectController.CardObject cardObject)
    {
      s_Singleton._ViewedObjects[index] = cardObject;
      if (s_Singleton._ViewedObjects[index] != null)
      {
        DeckController.ShowCardObjectData(index, cardObject._CardData);
        if (index == 0)
        {
          s_Singleton._ForcedViewedCardObject0 = null;
          s_Singleton.SelectTile(cardObject._Position);
        }
      }
    }
    public static void UpdateViewedObject(int index)
    {

      // Check forced view
      if (index == 0 && s_Singleton._ForcedViewedCardObject0 != null)
      {
        DeckController.ShowCardObjectData(index, s_Singleton._ForcedViewedCardObject0);
        return;
      }

      //
      var cardObject = s_Singleton._ViewedObjects[index];
      if (cardObject != null)
      {
        DeckController.ShowCardObjectData(index, cardObject._CardData);
        if (index == 0)
          s_Singleton.SelectTile(cardObject._Position);
      }
    }
    public static void HideViewedObject(int index)
    {
      s_Singleton._ViewedObjects[index] = null;
      DeckController.HideCardObjectData(index);
    }

    public static void UpdateViewedObjects()
    {
      for (var i = 0; i < s_Singleton._ViewedObjects.Length; i++)
      {
        UpdateViewedObject(i);
      }
    }

    //
    public static void ForceViewedCardData(CardController.CardData cardData)
    {
      s_Singleton._ForcedViewedCardObject0 = cardData;
      DeckController.ShowCardObjectData(0, cardData);
    }

    //
    public static bool IsTileOnCorrectBattlefield(int ownerId, Vector2Int tilePos)
    {
      var battlefieldYRange = ownerId == 0 ?
        new Vector2Int(ObjectController.s_TileMapSize.y / 2, ObjectController.s_TileMapSize.y - 1) :
        new Vector2Int(0, ObjectController.s_TileMapSize.y / 2 - 1);
      if (tilePos.y < battlefieldYRange.x || tilePos.y > battlefieldYRange.y)
        return false;

      return true;
    }

    //
    public void OnTurnEnd()
    {



    }

    //
    public static Vector2 GetTileGameObjectPosition(Vector2Int tilePos)
    {
      var tilemapSize = ObjectController.s_TileMapSize;
      var gameObjectSize = ObjectController.s_TileMapGameObjectSize;
      return new Vector2(
        tilePos.x * gameObjectSize.x + gameObjectSize.x / 2f - tilemapSize.x / 2f * gameObjectSize.x,
        tilePos.y * gameObjectSize.y + gameObjectSize.y / 2f - tilemapSize.y / 2f * gameObjectSize.y
      );
    }
  }

  // Start is called before the first frame update
  void Start()
  {
    if (s_Players == null) s_Players = new();
    s_Players.Add(this);

    //
    _OwnerController = new(s_Players.Count);
    _OwnerController.SetHealth(10);
    _OwnerController._Deck.UpdateManaDisplay();

    //
    new TilemapController(this);

    //
    GameController.s_Singleton._EnemyController = new();
  }



  // Update is called once per frame
  void Update()
  {

    // Handle card positions
    _OwnerController._Deck.Update();
    if (_OwnerController._Deck._GameInteractive)
    {
      _OwnerController._Hand.Update();

      //
      TilemapController.s_Singleton.Update();

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
  }
  Vector3 _middleMouseDownPos, _cameraSavePos;
}
