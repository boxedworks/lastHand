using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

using System.Linq;

public class PlayerController : MonoBehaviour
{
  public static List<PlayerController> s_Players;

  public int _OwnerId;

  public HandController _Hand;
  public DeckController _Deck;

  public Vector2Int _TileHovered { get { return _tilemapController._TileHovered; } }

  //
  TilemapController _tilemapController;
  public class TilemapController
  {

    public Vector2Int _TileHovered, _TileSelected;

    public TilemapController()
    {
      _TileHovered = _TileSelected = new Vector2Int(-1, -1);
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
          tilePos.x > -1 && tilePos.x < ObjectController.s_TileMapSize.x && tilePos.y > -1 && tilePos.y < ObjectController.s_TileMapSize.y
          )
        {

          // New hover
          _TileHovered = tilePos;

          if (_TileHovered != _TileSelected)
          {
            var img = ObjectController.s_Singleton.GetTileMapImage(tilePos);
            img.color = Color.gray;
          }

          // Selection
          if (Input.GetMouseButtonUp(0))
          {

            var oldSelected = _TileSelected;
            _TileSelected = tilePos;

            if (oldSelected.x != -1 && oldSelected != _TileSelected)
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
            {
              Debug.Log($"Selected cardObject: {cardObject._Id} {cardObject._CardData.TextTitle}");

              DeckController.ShowCardObjectData(cardObject._CardData);
            }
          }
        }

        // Tile out of bounds
        else
          _TileHovered = new Vector2Int(-1, -1);

        // Old hover
        if (oldHover.x != -1 && oldHover != _TileHovered && oldHover != _TileSelected)
        {
          var img = ObjectController.s_Singleton.GetTileMapImage(oldHover);
          img.color = Color.white;
        }
      }
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

    _OwnerId = 1;

    _Deck = new(this);
    _Hand = new(this);
    _tilemapController = new();
  }

  // Update is called once per frame
  void Update()
  {

    // Handle card positions
    _Deck.Update();
    if (_Deck._GameInteractive)
    {
      _Hand.Update();

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
  }
  Vector3 _middleMouseDownPos, _cameraSavePos;

  //
  public void OnTurnEnd()
  {
    GameController.s_Singleton.OnTurnsEnded();
  }
}
