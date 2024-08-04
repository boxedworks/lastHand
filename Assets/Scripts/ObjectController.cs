using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Linq;
using System;
using Unity.VisualScripting;

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

    //
    _tileMapPositionsAll = new();
    for (var y = 0; y < _tileMapSize.y; y++)
      for (var x = 0; x < _tileMapSize.x; x++)
      {
        var tilePos = new Vector2Int(x, y);
        _tileMapPositionsAll.Add(tilePos);
      }
  }

  //
  public void Update()
  {

    //
    if (_takingTurn)
    {

      // Check enemy taking turn
      if (_enemyTurn)
      {

        // Play enemy cards / units

        // Enemy turn ended
        if (_turnObjects.Count == 0)
        {
          _enemyTurn = false;

          // Order by x-y
          var cardObjects = GetCardObjects(0);
          cardObjects = cardObjects.OrderBy(c => c._Position.y).ThenBy(c => c._Position.x).ToList();
          _turnObjects = new Queue<CardObject>(cardObjects);
        }

        // Tap all cards
        else
        {
          if (_gameCoroutine == null)
          {
            var nextObject = _turnObjects.Dequeue();
            if (nextObject != null && !nextObject._Destroyed)
            {
              _gameCoroutine = GameController.s_Singleton.StartCoroutine(nextObject.TrySmoothTap(() =>
              {
                _gameCoroutine = null;
              }));
            }
          }
        }
      }

      // Check player and enemy end turn
      else if (_turnObjects.Count == 0)
      {
        if (_playerTurn)
        {
          _playerTurn = false;
          _enemyTurn = true;

          // Get all tap monsters
          var cardObjects = GetCardObjects(0).Where(x => x._CardData.HasTapEffect).ToList();
          _turnObjects = new Queue<CardObject>(cardObjects);
        }
        else
          _takingTurn = false;
      }
      else
      {
        if (_gameCoroutine == null)
        {
          var nextObject = _turnObjects.Dequeue();
          if (nextObject != null && !nextObject._Destroyed)
          {
            _gameCoroutine = GameController.s_Singleton.StartCoroutine(nextObject.TakeTurn(() =>
            {
              _gameCoroutine = null;
            }));
          }
        }
      }

    }
  }

  //
  Coroutine _gameCoroutine;
  public static bool _IsActionsHappening { get { return s_Singleton._gameCoroutine != null; } }
  public static void TryTap(CardObject cardObject)
  {

    if (s_Singleton._gameCoroutine != null) return;

    Debug.Log($"Tapped cardObject: {cardObject._ObjectId} {cardObject._CardData.TextTitle}");
    s_Singleton._gameCoroutine = GameController.s_Singleton.StartCoroutine(cardObject.TrySmoothTap(() =>
    {
      s_Singleton._gameCoroutine = null;
    }));
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
    return tileMap[position]._ObjectId != cardObject._ObjectId;
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
    return s_Singleton._tileMapPositionsAll
      .Where(x => !s_Singleton._objectsTileMap.ContainsKey(x))
      .ToArray();
  }
  public static Vector2Int[] GetEmptyDeployTiles(int ownerId)
  {
    var deployYPos = ownerId == 0 ? s_TileMapSize.y - 1 : 0;
    return GetEmptyTiles()
      .Where(p => p.y == deployYPos)
      .ToArray();
  }

  public static bool IsDeployTile(Vector2Int tilePos)
  {
    return tilePos.y == 0 || tilePos.y == s_TileMapSize.y - 1;
  }

  //
  bool _takingTurn;
  bool _playerTurn;
  bool _enemyTurn;
  Queue<CardObject> _turnObjects;
  public void HandleCardObjects()
  {


    // Execute all card actions in order; allies, then system turn + actions
    var cardObjects = new List<CardObject>();
    _takingTurn = true;
    _playerTurn = true;

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

    /*/ Order system by x-y
    var systemEntities = GetCardObjects(0);
    systemEntities = systemEntities.OrderBy(c => c._Position.y).ThenBy(c => c._Position.x).ToList();
    foreach (var cardObject in systemEntities)
      cardObjects.Add(cardObject);*/

    //
    _turnObjects = new(cardObjects);
  }

  //
  public static UnityEngine.UI.Image GetTileMapImage(Vector2Int position)
  {
    return GameObject.Find("TileMapUI").transform.GetChild(0).GetChild(position.y + 1).GetChild(position.x).GetComponent<UnityEngine.UI.Image>();
  }

  //
  public static bool IsPosWithinTilemap(Vector2Int pos)
  {
    return pos.x > -1 && pos.x < s_TileMapSize.x && pos.y > -1 && pos.y < s_TileMapSize.y;
  }

  // Objects on the tilemap that can be affected by cards
  public class CardObject
  {
    public static int s_id;
    public int _ObjectId;

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
    TMPro.TextMeshPro _textStatus;

    public CardController.CardData _CardData;

    public CardObject(int ownerId, Vector2Int spawnPosition, CardController.CardData cardData)
    {
      _ObjectId = s_id++;
      _OwnerId = ownerId;
      RegisterCardObject(this);

      //
      _CardData = CardController.CardData.Clone(cardData);

      // Configure model
      _gameObject = GameObject.Instantiate(Resources.Load("CardObjects/Placeholder")) as GameObject;
      SetTokenColor();
      _gameObject.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>($"CardImages/{cardData.CardId}");
      _textStatus = _gameObject.transform.GetChild(1).GetComponent<TMPro.TextMeshPro>();
      UpdateStatus();

      // Set position on tilemap
      _Position = new Vector2Int(-100, -100);
      _positionLocalOffsets = new Vector2Int[] { Vector2Int.zero };
      SetPosition(spawnPosition);
    }

    //
    public void UpdateStatus()
    {
      _textStatus.text = $"{_CardData.CardInstanceData.Attack}/{_CardData.CardInstanceData.Health}";
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
    public bool CanSetPosition(Vector2Int tilePos)
    {

      //
      if (!IsPosWithinTilemap(tilePos))
        return false;

      // Check can move per offset
      foreach (var offset in _positionLocalOffsets)
        if (TileOccupied(tilePos + offset, this)) return false;

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
    //
    public bool CanJump()
    {
      // Check stationary
      if (_CardData.IsStationary) return false;

      // Normal move
      var moveAmount = GetMovementDirection();

      var startTilePos = _Position;
      var middleTilePos = startTilePos + moveAmount;
      var endTilePos = middleTilePos + moveAmount;

      return !CanSetPosition(middleTilePos) && CanSetPosition(endTilePos);
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
      return other != null && IsEnemy(other);
    }

    //
    public IEnumerator TakeTurn(Action onComplete)
    {

      // Check turn start effects
      if (_CardData.HasStartEffect)
        yield return ProcessTriggerEffects("start");

      // Movement
      yield return ProcessTriggerEffect("move");

      //yield return new WaitForSeconds(0.5f);

      //
      _tapped = false;
      SetTokenColor();

      //
      onComplete?.Invoke();
    }

    //


    //
    public IEnumerator TrySmoothTap(Action onComplete)
    {

      // Gather tap effect
      if (_CardData.HasTapEffect && !_tapped)
      {
        _tapped = true;
        SetTokenColor();

        yield return ProcessTriggerEffects("tap");
      }

      //
      onComplete?.Invoke();
    }

    //
    IEnumerator ProcessTriggerEffects(string tagName)
    {
      foreach (var effect in _CardData.BehaviorPattern.Split(';'))
      {

        if (effect.StartsWith($"{tagName}:"))
        {
          var effectDetails = effect[(tagName.Length + 1)..];
          yield return ProcessTriggerEffect(effectDetails);
        }

      }
    }
    IEnumerator ProcessTriggerEffect(string effect)
    {
      yield return ProcessEffect(effect);
    }

    //
    IEnumerator ProcessEffect(string effectType)
    {

      Debug.Log($"processing effect {effectType}");
      bool effectFired = false;

      IEnumerator EffectFired()
      {
        if (!effectFired)
        {
          effectFired = true;

          // UI
          if (PlayerController.TilemapController.s_Singleton._ViewedObjects[0] != this)
          {
            PlayerController.TilemapController.SetViewedObject(0, this);
            yield return new WaitForSeconds(0.5f);
          }
        }
      }

      // Check buff
      if (effectType.StartsWith("buff("))
      {
        var effectDetails = effectType[5..^1];

        // Get buff amounts
        var details = effectDetails.Split(",");
        var amountAttack = int.Parse(details[0].Trim());
        var amountHealth = int.Parse(details[1].Trim());
        var effectTargets = details[2].Trim();

        // Buff targets
        foreach (var target in getTargets(effectTargets))
        {
          yield return EffectFired();

          PlayerController.TilemapController.SetViewedObject(1, target);
          PlayerController.TilemapController.s_Singleton.SetBuffIndicator(target._Position);
          yield return new WaitForSeconds(0.75f);

          target._CardData.CardInstanceData.Attack += amountAttack;
          target._CardData.CardInstanceData.Health += amountHealth;
          target.UpdateStatus();

          PlayerController.TilemapController.UpdateViewedObjects();
          yield return new WaitForSeconds(0.5f);
        }
      }

      // Check attack
      else if (effectType.StartsWith("attack("))
      {
        var effectDetails = effectType[7..^1];

        // Damage targets
        foreach (var target in getTargets(effectDetails))
        {
          yield return EffectFired();

          PlayerController.TilemapController.SetViewedObject(1, target);
          PlayerController.TilemapController.s_Singleton.SetAttackIndicator(target._Position);
          yield return new WaitForSeconds(1f);

          // Attack animation

          // Record damage
          target._CardData.CardInstanceData.Health -= _CardData.CardInstanceData.Attack;
          PlayerController.TilemapController.UpdateViewedObjects();
          target.UpdateStatus();
          yield return new WaitForSeconds(0.5f);
          yield return SmoothCheckStatus(target);
        }
      }

      // Check deploy
      else if (effectType.StartsWith("deploy("))
      {
        var effectDetails = effectType[7..^1];

        // Spawn new cardObject
        var cardTarget = getTargets(effectDetails)[0];
        var freeTiles = GetEmptyDeployTiles(_OwnerId);
        if (freeTiles.Length > 0)
        {
          yield return EffectFired();

          var newObject = new CardObject(
            cardTarget._OwnerId,
            freeTiles[UnityEngine.Random.Range(0, freeTiles.Length)],
            CardController.GetCardData(cardTarget._CardData.CardId)
          );

          PlayerController.TilemapController.SetViewedObject(0, newObject);
          yield return new WaitForSeconds(0.5f);
        }
      }

      // Check movement
      else if (effectType.StartsWith("move"))
      {
        // Check if has movement
        if (CanMove())
        {
          yield return EffectFired();
          yield return SmoothMove(false);
        }
        // Else, check in front; check attack
        else if (CanAttack())
        {
          yield return EffectFired();
          yield return ProcessTriggerEffect("attack(front)");
        }
        // Check jump ally
        else if (CanJump())
        {
          yield return EffectFired();
          yield return SmoothMove(true);
        }
      }

      // Check effect fire
      if (effectFired)
      {

        PlayerController.TilemapController.SetViewedObject(0, this);
        PlayerController.TilemapController.HideViewedObject(1);
        PlayerController.TilemapController.s_Singleton.ClearAttackTile();
        PlayerController.TilemapController.s_Singleton.ClearBuffTile();
        yield return new WaitForSeconds(0.5f);

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

      // Check dirs
      else if (targetString == "front")
      {
        var frontObject = GetCardObject(_Position + new Vector2Int(0, _OwnerId == 0 ? -1 : 1));
        if (frontObject != null)
          targetList.Add(frontObject);
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
        Debug.Log($"{targetList.Count} .. {targetModifier}");

        switch (targetModifier)
        {
          case "ally":
            targetList = targetList.Where(x => OwnerIdIsAlly(x._OwnerId)).ToList();
            break;
          case "enemy":
            targetList = targetList.Where(x => !OwnerIdIsAlly(x._OwnerId)).ToList();
            break;

          default:
            Debug.Log($"target modifier not implemented");
            break;
        }


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
    public IEnumerator SmoothMove(bool isJump)
    {

      //
      if (PlayerController.TilemapController.s_Singleton._ViewedObjects[0] != this)
      {
        PlayerController.TilemapController.SetViewedObject(0, this);
        yield return new WaitForSeconds(0.5f);
      }

      //
      var moveAmount = GetMovementDirection();

      var startTilePos = _Position;
      var endTilePos = startTilePos + moveAmount * (isJump ? 2 : 1);

      var startGameObjectPos2 = PlayerController.TilemapController.GetTileGameObjectPosition(startTilePos);
      var endGameObjectPos2 = PlayerController.TilemapController.GetTileGameObjectPosition(endTilePos);

      var startGameObjectPos = new Vector3(startGameObjectPos2.x, 0f, startGameObjectPos2.y);
      var endGameObjectPos = new Vector3(endGameObjectPos2.x, 0f, endGameObjectPos2.y);

      var t = 1f;
      while (t > 0f)
      {
        _gameObject.transform.position = Vector3.Lerp(startGameObjectPos, endGameObjectPos, 1f - t);

        yield return new WaitForSeconds(0.005f);
        t -= 0.04f;
      }
      _gameObject.transform.position = endGameObjectPos;
      SetPosition(endTilePos);

      PlayerController.TilemapController.UpdateViewedObject(0);

      // Check battlefield cross
      if (_CardData.HasBattleCrossEffect)
      {
        var halfway = s_TileMapSize.y / 2;
        if ((startTilePos.y <= halfway - 1 && endTilePos.y >= halfway) || (startTilePos.y >= halfway && endTilePos.y <= halfway - 1))
        {
          yield return ProcessTriggerEffects("battlecross");
        }
      }
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
      PlayerController.TilemapController.SetViewedObject(1, other);
      yield return new WaitForSeconds(0.5f);

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
          PlayerController.TilemapController.UpdateViewedObjects();
          other.UpdateStatus();
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

      // Update UI
      //PlayerController.TilemapController.UpdateViewedObject(0);

      // Check dead
      if (cardObject._CardData.CardInstanceData.Health <= 0)
      {
        yield return new WaitForSeconds(0.5f);
        cardObject.Destroy();

        // Update UI
        for (var i = 0; i < 2; i++)
          if (PlayerController.TilemapController.s_Singleton._ViewedObjects[i] == cardObject)
            PlayerController.TilemapController.HideViewedObject(i);
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
      return _ObjectId == item._ObjectId;
    }
    public override int GetHashCode()
    {
      return _ObjectId.GetHashCode();
    }
  }


}
