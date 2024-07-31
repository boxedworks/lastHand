using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
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
          (int)Mathf.RoundToInt(hitpoint.x / 5f + tileMapSize.x / 2f - 0.5f),
          (int)Mathf.RoundToInt(hitpoint.z / 5f + tileMapSize.y / 2f - 0.5f)
        );
        if (tilePos.x > -1 && tilePos.x < tileMapSize.x && tilePos.y >= 0 && tilePos.y < tileMapSize.x)
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
              Debug.Log($"Selected cardObject: {cardObject._Id}");

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
}
