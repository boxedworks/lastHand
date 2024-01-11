using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ObjectController
{

  //
  public static ObjectController s_Singleton;

  //
  Dictionary<Vector2Int, CardObject> _tileMap;
  Vector2Int _tileMapSize;
  public static Vector2Int s_TileMapSize { get { return s_Singleton._tileMapSize; } }
  public ObjectController()
  {
    s_Singleton = this;

    //
    _tileMap = new();
    _tileMapSize = new Vector2Int(10, 10);
    var uiElementsBase = GameObject.Instantiate(GameObject.Find("TileMapUI").transform.GetChild(0).gameObject).transform;
    uiElementsBase.parent = GameObject.Find("TileMapUI").transform;
    uiElementsBase.gameObject.SetActive(true);
    for (var x = 1; x < _tileMapSize.x; x++)
    {
      var uiTile = GameObject.Instantiate(uiElementsBase.GetChild(0).gameObject).transform;
      uiTile.parent = uiElementsBase;
    }
    for (var y = 1; y < _tileMapSize.y; y++)
    {
      var uiElementsRow = GameObject.Instantiate(uiElementsBase.gameObject).transform;
      uiElementsRow.parent = uiElementsBase.parent;
    }
  }

  //
  static bool TileOccupied(Vector2Int position)
  {
    var tileMap = s_Singleton._tileMap;
    return tileMap.ContainsKey(position);
  }
  static bool TileOccupied(Vector2Int position, CardObject cardObject)
  {
    var tileMap = s_Singleton._tileMap;
    if (!tileMap.ContainsKey(position))
      return false;
    return tileMap[position]._Id != cardObject._Id;
  }

  //
  public static CardObject GetCardObject(Vector2Int position)
  {
    var tileMap = s_Singleton._tileMap;
    if (!tileMap.ContainsKey(position)) return null;
    return tileMap[position];
  }

  //
  static void SetPosition(Vector2Int position, CardObject cardObject)
  {
    var tileMap = s_Singleton._tileMap;
    tileMap[position] = cardObject;
  }
  public static bool CanSetPosition(Vector2Int position, Vector2Int[] positionLocalOffsets)
  {
    foreach (var offset in positionLocalOffsets)
      if (TileOccupied(position + offset))
        return false;

    return true;
  }

  //
  public UnityEngine.UI.Image GetTileMapImage(Vector2Int position)
  {
    return GameObject.Find("TileMapUI").transform.GetChild(0).GetChild(position.y).GetChild(position.x).GetComponent<UnityEngine.UI.Image>();
  }

  // Objects on the tilemap that can be affected by cards
  public class CardObject
  {
    public static int s_id;
    public int _Id;

    public Vector2Int _Position;
    Vector2Int[] _positionLocalOffsets;

    GameObject _gameObject;

    public CardObject(Vector2Int spawnPosition)
    {
      _Id = s_id++;

      // Grab model
      _gameObject = GameObject.Instantiate(Resources.Load("CardObjects/Placeholder")) as GameObject;

      // Set position on tilemap
      _Position = new Vector2Int(-100, -100);
      _positionLocalOffsets = new Vector2Int[] { Vector2Int.zero };
      SetPosition(spawnPosition);
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
    public void SetPosition(Vector2Int position)
    {

      //
      if (position == _Position) return;

      // Check can move per offset
      if (!CanSetPosition(position)) return;

      // Move per offset
      foreach (var offset in _positionLocalOffsets)
      {
        ObjectController.SetPosition(_Position + offset, null);
        ObjectController.SetPosition(position + offset, this);
      }

      // Set position
      _Position = position;
      _gameObject.transform.position = new Vector3(position.x - 5f * 5f, 0f, position.y - 4.5f * 5f);
    }
  }


}
