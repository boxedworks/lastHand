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
  List<Vector2Int> _tileMapPositionsAll;
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
    _tileMapPositionsAll = new();
    for (var y = 0; y < _tileMapSize.y; y++)
      for (var x = 0; x < _tileMapSize.x; x++)
        _tileMapPositionsAll.Add(new Vector2Int(x, y));

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
          if (nextObject != null && !nextObject._Destroyed)
          {

            _currentTurnObject = GameController.s_Singleton.StartCoroutine(nextObject.TakeTurn(() =>
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
  public static Vector2Int[] GetEmptyTiles()
  {
    return s_Singleton._objectsTileMap
      .Where(x => x.Value == null)
      .ToDictionary(p => p.Key, p => p.Value)
      .Keys.ToArray();
  }
  public static Vector2Int[] GetEmptyDeployTiles(int ownerId)
  {
    var deployYPos = ownerId == 0 ? s_TileMapSize.y - 1 : 0;
    return GetEmptyTiles()
      .Where(p => p.y == deployYPos)
      .ToArray();
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

    // Order system by x-y
    var systemEntities = GetCardObjects(0);
    systemEntities = systemEntities.OrderBy(c => c._Position.y).ThenBy(c => c._Position.x).ToList();
    foreach (var cardObject in systemEntities)
      cardObjects.Add(cardObject);

    //
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
    public bool _Destroyed { get { return _gameObject == null; } }

    //
    public int _OwnerId;

    // Tile position
    public Vector2Int _Position;

    //
    bool _tapped;

    // Holds local position offsets; allows one object to take up multiple tiles
    Vector2Int[] _positionLocalOffsets;

    //
    GameObject _gameObject;

    public CardController.CardData _CardData;

    public CardObject(int ownerId, Vector2Int spawnPosition, CardController.CardData cardData)
    {
      _Id = s_id++;
      _OwnerId = ownerId;
      RegisterCardObject(this);

      //
      _CardData = new()
      {
        CardId = cardData.CardId,

        TextTitle = cardData.TextTitle,
        TextDescription = cardData.TextDescription,

        Deck = cardData.Deck,

        BehaviorPattern = cardData.BehaviorPattern,

        CardInstanceData = new()
        {
          Health = cardData.CardInstanceData.Health,
          Attack = cardData.CardInstanceData.Attack,

          Cost = cardData.CardInstanceData.Cost
        }
      };

      // Configure model
      _gameObject = GameObject.Instantiate(Resources.Load("CardObjects/Placeholder")) as GameObject;
      SetTokenColor();
      _gameObject.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>($"CardImages/{cardData.CardId}");

      // Set position on tilemap
      _Position = new Vector2Int(-100, -100);
      _positionLocalOffsets = new Vector2Int[] { Vector2Int.zero };
      SetPosition(spawnPosition);
    }

    //
    void SetTokenColor()
    {
      _gameObject.transform.GetChild(0).GetChild(0).GetComponent<SpriteRenderer>().color = (_OwnerId switch
      {
        1 => Color.red,
        2 => Color.blue,

        _ => Color.gray
      }) * (_tapped || !_CardData.HasTapEffect ? 0.7f : 1f);
    }

    //
    public void Destroy()
    {
      UnregisterCardObject(this);
      GameObject.Destroy(_gameObject);
      _CardData = null;
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
      // Check stationary
      if (_CardData.IsStationary) return false;

      // Normal move
      var moveAmount = GetMovementDirection();

      var startTilePos = _Position;
      var endTilePos = startTilePos + moveAmount;

      return CanSetPosition(endTilePos);
    }
    public bool CanAttack()
    {
      // Check stationary
      if (_CardData.IsStationary) return false;

      // Normal 'melee' attack
      var moveAmount = GetMovementDirection();

      var startTilePos = _Position;
      var endTilePos = startTilePos + moveAmount;

      var other = GetCardObject(endTilePos);
      return IsEnemy(other);
    }

    //
    public IEnumerator TakeTurn(Action onComplete)
    {

      // Check turn start effects
      if (_CardData.HasStartEffect)
        yield return ProcessTriggerEffect("start");

      // Check if has movement
      if (CanMove())
        yield return SmoothMove(null);

      // Else, check in front; check attack
      else if (CanAttack())
        yield return SmoothMeleeAttack(null);
      //yield return new WaitForSeconds(0.5f);

      //
      _tapped = false;
      SetTokenColor();

      //
      onComplete?.Invoke();
    }

    //
    public IEnumerator TrySmoothTap(Action onComplete)
    {

      // Gather tap effect
      if (_CardData.HasTapEffect && !_tapped)
      {
        _tapped = true;
        SetTokenColor();

        yield return ProcessTriggerEffect("tap");
      }

      //
      onComplete?.Invoke();
    }

    //
    IEnumerator ProcessTriggerEffect(string tagName)
    {
      foreach (var effect in _CardData.BehaviorPattern.Split(';'))
      {

        if (effect.StartsWith($"{tagName}:"))
        {
          var effectDetails = effect[(tagName.Length + 1)..];
          yield return ProcessEffect(effectDetails);
        }

      }
    }

    //
    IEnumerator ProcessEffect(string effectType)
    {

      Debug.Log($"processing effect {effectType}");

      // Check buff
      if (effectType.StartsWith("buff("))
      {
        var effectDetails = effectType[5..^1];
        yield return EffectBuff(effectDetails);
      }

      // Check attack
      else if (effectType.StartsWith("attack("))
      {
        var effectDetails = effectType[7..^1];

        // Damage targets
        foreach (var target in getTargets(effectDetails))
        {

          yield return new WaitForSeconds(0.5f);

          target._CardData.CardInstanceData.Health -= _CardData.CardInstanceData.Attack;
          yield return SmoothCheckStatus(target);
        }
      }

      // Check deploy ; deploy(self)
      else if (effectType.StartsWith("deploy("))
      {
        var effectDetails = effectType[7..^1];

        // Spawn new cardObject
        var cardTarget = getTargets(effectDetails)[0];
        var freeTiles = GetEmptyDeployTiles(_OwnerId);
        if (freeTiles.Length > 0)
          new CardObject(
            cardTarget._OwnerId,
            freeTiles[UnityEngine.Random.Range(0, freeTiles.Length)],
            CardController.GetCardData(cardTarget._Id)
          );
        else Debug.Log("0 available deploy tiles");
      }

      // Check movement
      else if (effectType.StartsWith("move"))
      {
        // Check if has movement
        if (CanMove())
          yield return SmoothMove(null);
      }
    }

    //
    IEnumerator EffectBuff(string effectDetails)
    {

      yield return new WaitForSeconds(0.5f);

      // Get buff amounts
      var details = effectDetails.Split(",");
      var amountAttack = int.Parse(details[0].Trim());
      var amountHealth = int.Parse(details[1].Trim());
      var effectTargets = details[2].Trim();

      // Buff targets
      foreach (var target in getTargets(effectTargets))
      {
        target._CardData.CardInstanceData.Attack += amountAttack;
        target._CardData.CardInstanceData.Health += amountHealth;
      }
    }

    CardObject[] getTargets(string targetString)
    {
      Debug.Log($"Gathering targets: {targetString}");

      var targetList = new List<CardObject>();
      var targetModifier = "";

      // Check self
      if (targetString == "self")
      {
        targetList.Add(this);
      }

      // Check surrounding units
      else if (targetString.StartsWith("allSurrounding"))
      {
        // Get list of all surrounding entities
        void AddTarget(CardObject cardObject)
        {
          if (cardObject == null || cardObject._Destroyed) return;
          targetList.Add(cardObject);
        }
        AddTarget(GetCardObject(new Vector2Int(_Position.x + 1, _Position.y)));
        AddTarget(GetCardObject(new Vector2Int(_Position.x - 1, _Position.y)));
        AddTarget(GetCardObject(new Vector2Int(_Position.x, _Position.y + 1)));
        AddTarget(GetCardObject(new Vector2Int(_Position.x, _Position.y - 1)));

        if (targetString.Contains(":"))
        {
          targetModifier = targetString[15..];
        }

      }

      // Gather a random enemy
      else if (targetString == "randomEnemy")
      {
        var enemyTargets = s_Singleton._objectsAll.Where(x => !OwnerIdIsAlly(x._OwnerId)).ToList();
        if (enemyTargets.Count > 0)
          targetList.Add(enemyTargets[UnityEngine.Random.Range(0, enemyTargets.Count)]);
      }

      // Filter by type
      if (targetModifier.Length > 0)
      {
        Debug.Log($"{targetList.Count} .. {targetModifier} (not implemented)");
        targetList = targetList.Where(x => OwnerIdIsAlly(x._OwnerId)).ToList();
      }
      Debug.Log(targetList.Count);

      //
      return targetList.ToArray();
    }

    //
    bool OwnerIdIsAlly(int otherOwnerId)
    {
      return _OwnerId == 0 ? otherOwnerId == 0 : otherOwnerId != 0;
    }


    //
    public IEnumerator SmoothMove(Action onComplete)
    {

      //
      var moveAmount = GetMovementDirection();

      var startTilePos = _Position;
      var endTilePos = startTilePos + moveAmount;

      var startGameObjectPos2 = PlayerController.TilemapController.GetTileGameObjectPosition(startTilePos);
      var endGameObjectPos2 = PlayerController.TilemapController.GetTileGameObjectPosition(endTilePos);

      var startGameObjectPos = new Vector3(startGameObjectPos2.x, 0f, startGameObjectPos2.y);
      var endGameObjectPos = new Vector3(endGameObjectPos2.x, 0f, endGameObjectPos2.y);

      var t = 1f;
      var triggered = false;
      while (t > 0f)
      {
        _gameObject.transform.position = Vector3.Lerp(startGameObjectPos, endGameObjectPos, 1f - t);

        yield return new WaitForSeconds(0.005f);
        t -= 0.04f;

        //
        if (!triggered && t <= 0.5f)
        {
          triggered = true;

          // Check battlefield cross
          if (_CardData.HasBattleCrossEffect)
          {
            var halfway = s_TileMapSize.y / 2;
            if ((startTilePos.y <= halfway && endTilePos.y >= halfway + 1) || (startTilePos.y >= halfway + 1 && endTilePos.y <= halfway))
            {
              yield return ProcessTriggerEffect("battlecross");
            }
          }
        }
      }
      _gameObject.transform.position = endGameObjectPos;
      SetPosition(endTilePos);

      // Check tilemap ui
      if (PlayerController.s_TilemapController._ViewedObject == this)
      {
        PlayerController.s_TilemapController.SelectTile(endTilePos);
      }

      //
      onComplete?.Invoke();
    }

    //
    public IEnumerator SmoothStuck(Action onComplete)
    {

      //
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
    public IEnumerator SmoothMeleeAttack(Action onComplete)
    {

      //
      var moveAmount = GetMovementDirection();

      var startTilePos = _Position;
      var endTilePos = startTilePos + moveAmount;

      var startPos2 = PlayerController.TilemapController.GetTileGameObjectPosition(startTilePos);
      var endPos2 = PlayerController.TilemapController.GetTileGameObjectPosition(endTilePos);

      var startPos = new Vector3(startPos2.x, 0f, startPos2.y);
      var endPos = new Vector3(endPos2.x, 0f, endPos2.y);

      // Gather other
      var other = GetCardObject(endTilePos);

      //
      var t = 1f;
      var size = 0.35f;
      var attacked = false;
      while (t > 0f)
      {
        _gameObject.transform.position = Vector3.Lerp(startPos, endPos, -Math.Abs((1f - t) - size) + size);

        yield return new WaitForSeconds(0.005f);
        t -= 0.04f;

        if (!attacked && t <= 0.5f)
        {
          attacked = true;

          //

          // Set health values
          //_CardData.CardInstanceData.Health -= other._CardData.CardInstanceData.Attack;
          other._CardData.CardInstanceData.Health -= _CardData.CardInstanceData.Attack;
        }
      }
      //_gameObject.transform.position = endPos;
      //SetPosition(endTilePos);

      //yield return CheckStatus(this);
      yield return SmoothCheckStatus(other);

      //
      onComplete?.Invoke();
    }

    // Resolve altercation
    IEnumerator SmoothCheckStatus(CardObject cardObject)
    {

      if (cardObject._CardData.CardInstanceData.Health <= 0)
      {
        yield return new WaitForSeconds(0.5f);
        cardObject.Destroy();
      }

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
