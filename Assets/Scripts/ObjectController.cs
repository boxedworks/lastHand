using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Linq;
using System;

public class ObjectController
{

  //
  public static ObjectController s_Singleton;

  //
  List<CardObject> _objectsAll;
  Dictionary<Vector2Int, CardObject> _objectsTileMap;
  Dictionary<int, List<CardObject>> _objectsOwner;
  Vector2Int _tileMapSize;
  public static Vector2Int s_TileMapSize { get { return s_Singleton._tileMapSize; } }
  public static Vector2 s_TileMapGameObjectSize = new Vector2(5f, 5f);

  public ObjectController()
  {
    s_Singleton = this;

    //
    _objectsAll = new();
    _objectsTileMap = new();
    _objectsOwner = new();

    _tileMapSize = new Vector2Int(6, 8);
    var uiElemntsBaseRef = GameObject.Find("TileMapUI").transform.GetChild(0).GetChild(0);
    var uiElementsBase = GameObject.Instantiate(uiElemntsBaseRef.gameObject, uiElemntsBaseRef.parent).transform;
    uiElementsBase.gameObject.SetActive(true);
    for (var x = 1; x < _tileMapSize.x; x++)
    {
      var uiTile = GameObject.Instantiate(uiElementsBase.GetChild(0).gameObject, uiElementsBase).transform;
    }
    for (var y = 1; y < _tileMapSize.y; y++)
    {
      var uiElementsRow = GameObject.Instantiate(uiElementsBase.gameObject, uiElementsBase.parent).transform;
    }
  }

  //
  public void Update()
  {
    //
    if (_takingTurn)
    {

      if (_turnObjects.Count == 0)
      {
        _takingTurn = false;
      }
      else
      {
        if (_currentTurnObject == null)
        {
          var nextObject = _turnObjects.Dequeue();

          // Move
          if (nextObject.CanMove())
          {
            _currentTurnObject = GameController.s_Singleton.StartCoroutine(nextObject.SmoothMove(() =>
            {
              _currentTurnObject = null;
            }));
          }

          // Attack if something in front
          else
          {
            _currentTurnObject = GameController.s_Singleton.StartCoroutine(nextObject.SmoothStuck(() =>
            {
              _currentTurnObject = null;
            }));
          }
        }
      }

    }
  }

  //
  static bool TileOccupied(Vector2Int position)
  {
    var tileMap = s_Singleton._objectsTileMap;
    return tileMap.ContainsKey(position);
  }
  static bool TileOccupied(Vector2Int position, CardObject cardObject)
  {
    var tileMap = s_Singleton._objectsTileMap;
    if (!tileMap.ContainsKey(position))
      return false;
    return tileMap[position]._Id != cardObject._Id;
  }

  //
  public static CardObject GetCardObject(Vector2Int position)
  {
    var objects = s_Singleton._objectsTileMap;
    if (!objects.ContainsKey(position)) return null;
    return objects[position];
  }
  public static List<CardObject> GetCardObjects(int ownerId)
  {
    var objects = s_Singleton._objectsOwner;
    if (!objects.ContainsKey(ownerId)) return null;
    return objects[ownerId];
  }

  //
  public static void RegisterCardObject(CardObject cardObject)
  {
    var objects = s_Singleton._objectsOwner;
    var ownerId = cardObject._OwnerId;

    if (!objects.ContainsKey(ownerId)) objects.Add(ownerId, new());
    objects[ownerId].Add(cardObject);
    s_Singleton._objectsAll.Add(cardObject);
  }

  //
  public static void UnregisterCardObject(CardObject cardObject)
  {
    s_Singleton._objectsTileMap.Remove(cardObject._Position);
    s_Singleton._objectsOwner[cardObject._OwnerId].Remove(cardObject);
    s_Singleton._objectsAll.Remove(cardObject);
  }

  //
  static void SetPosition(Vector2Int position, CardObject cardObject)
  {
    var objects = s_Singleton._objectsTileMap;
    if (cardObject == null)
      objects.Remove(position);
    else
      objects[position] = cardObject;
  }
  public static bool CanSetPosition(Vector2Int position, Vector2Int[] positionLocalOffsets)
  {
    foreach (var offset in positionLocalOffsets)
      if (TileOccupied(position + offset))
        return false;

    return true;
  }

  //
  bool _takingTurn;
  Queue<CardObject> _turnObjects;
  public Coroutine _currentTurnObject;
  public void HandleCardObjects()
  {


    // Execute all card actions in order; system, all others
    var cardObjects = new List<CardObject>();
    _takingTurn = true;

    // Order system by x-y
    var systemEntities = GetCardObjects(0);
    systemEntities = systemEntities.OrderBy(c => c._Position.y).ThenBy(c => c._Position.x).ToList();
    foreach (var cardObject in systemEntities)
      cardObjects.Add(cardObject);

    // Order player(s) by x-y
    var playerEntitiesAll = new List<CardObject>();
    for (var i = 1; i < _objectsOwner.Count; i++)
    {
      var playerEntities = GetCardObjects(i);

      foreach (var cardObject in playerEntities)
        playerEntitiesAll.Add(cardObject);
    }
    playerEntitiesAll = playerEntitiesAll.OrderBy(c => -c._Position.y).ThenBy(c => c._Position.x).ToList();
    foreach (var cardObject in playerEntitiesAll)
      cardObjects.Add(cardObject);

    _turnObjects = new(cardObjects);
  }

  //
  public UnityEngine.UI.Image GetTileMapImage(Vector2Int position)
  {
    return GameObject.Find("TileMapUI").transform.GetChild(0).GetChild(position.y + 1).GetChild(position.x).GetComponent<UnityEngine.UI.Image>();
  }

  // Objects on the tilemap that can be affected by cards
  public class CardObject
  {
    public static int s_id;
    public int _Id;

    //
    public int _OwnerId;

    // Tile position
    public Vector2Int _Position;

    // Holds local position offsets; allows one object to take up multiple tiles
    Vector2Int[] _positionLocalOffsets;

    GameObject _gameObject;

    public CardController.CardData _CardData;

    public CardObject(int ownerId, Vector2Int spawnPosition, CardController.CardData cardData)
    {
      _Id = s_id++;
      _OwnerId = ownerId;
      RegisterCardObject(this);

      //
      _CardData = cardData;

      // Configure model
      _gameObject = GameObject.Instantiate(Resources.Load("CardObjects/Placeholder")) as GameObject;
      _gameObject.transform.GetChild(0).GetChild(0).GetComponent<SpriteRenderer>().color = _OwnerId switch {
        1 => Color.red,
        2 => Color.blue,

        _ => Color.gray
      };
      _gameObject.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>($"CardImages/{cardData.CardId}");

      // Set position on tilemap
      _Position = new Vector2Int(-100, -100);
      _positionLocalOffsets = new Vector2Int[] { Vector2Int.zero };
      SetPosition(spawnPosition);
    }

    //
    public void SetPosition(Vector2Int atTile)
    {

      //
      if (atTile == _Position) return;

      // Check can move per offset
      if (!CanSetPosition(atTile)) return;

      // Move per offset
      foreach (var offset in _positionLocalOffsets)
      {
        ObjectController.SetPosition(_Position + offset, null);
        ObjectController.SetPosition(atTile + offset, this);
      }

      // Set position
      _Position = atTile;

      var gamoeObjectPositon = PlayerController.TilemapController.GetTileGameObjectPosition(atTile);
      _gameObject.transform.position = new Vector3(
        gamoeObjectPositon.x,
        0f,
        gamoeObjectPositon.y
      );
    }

    //
    public bool CanSetPosition(Vector2Int position)
    {

      // Check can move per offset
      foreach (var offset in _positionLocalOffsets)
        if (TileOccupied(position + offset, this)) return false;

      return true;
    }

    //
    Vector2Int GetMovementDirection()
    {
      return new Vector2Int(0, 1 * _OwnerId == 0 ? -1 : 1);
    }

    //
    public bool CanMove()
    {
      var moveAmount = GetMovementDirection();

      var startTilePos = _Position;
      var endTilePos = startTilePos + moveAmount;

      return CanSetPosition(endTilePos);
    }
    public bool CanAttack()
    {
      var moveAmount = GetMovementDirection();

      var startTilePos = _Position;
      var endTilePos = startTilePos + moveAmount;

      var other = GetCardObject(endTilePos);
      return IsEnemy(other);
    }

    public IEnumerator SmoothMove(Action onComplete)
    {

      var moveAmount = GetMovementDirection();

      var startTilePos = _Position;
      var endTilePos = startTilePos + moveAmount;

      var startPos2 = PlayerController.TilemapController.GetTileGameObjectPosition(startTilePos);
      var endPos2 = PlayerController.TilemapController.GetTileGameObjectPosition(endTilePos);

      var startPos = new Vector3(startPos2.x, 0f, startPos2.y);
      var endPos = new Vector3(endPos2.x, 0f, endPos2.y);

      var t = 1f;
      while (t > 0f)
      {
        _gameObject.transform.position = Vector3.Lerp(startPos, endPos, 1f - t);

        yield return new WaitForSeconds(0.005f);
        t -= 0.04f;
      }
      _gameObject.transform.position = endPos;
      SetPosition(endTilePos);

      onComplete?.Invoke();
    }

    public IEnumerator SmoothStuck(Action onComplete)
    {

      var moveAmount = GetMovementDirection();

      var startTilePos = _Position;
      var endTilePos = startTilePos + moveAmount;

      var startPos2 = PlayerController.TilemapController.GetTileGameObjectPosition(startTilePos);
      var endPos2 = PlayerController.TilemapController.GetTileGameObjectPosition(endTilePos);

      var startPos = new Vector3(startPos2.x, 0f, startPos2.y);
      var endPos = new Vector3(endPos2.x, 0f, endPos2.y);

      var t = 1f;
      var size = 0.35f;
      while (t > 0f)
      {
        _gameObject.transform.position = Vector3.Lerp(startPos, endPos, -Math.Abs((1f - t) - size) + size);

        yield return new WaitForSeconds(0.005f);
        t -= 0.04f;
      }
      //_gameObject.transform.position = endPos;
      //SetPosition(endTilePos);

      onComplete?.Invoke();
    }

    //
    public bool IsEnemy(CardObject other)
    {
      return _OwnerId == 0 ? other._OwnerId > 0 : other._OwnerId == 0;
    }

    //
    public override bool Equals(object obj)
    {
      var item = obj as CardObject;
      return _Id == item._Id;
    }
    public override int GetHashCode()
    {
      return _Id.GetHashCode();
    }
  }


}
