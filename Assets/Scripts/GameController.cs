using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Mirror;

public class GameController : MonoBehaviour
{
  public static GameController s_Singleton;

  // Start is called before the first frame update
  void Start()
  {
    s_Singleton = this;

    new CardController();
    new ObjectController();

    new ObjectController.CardObject(new Vector2Int(0, 0));

    // Start network
#if UNITY_EDITOR
    NetworkManager.singleton.StartHost();
#else
    NetworkManager.singleton.StartClient();
#endif
  }

  // Update is called once per frame
  void Update()
  {

  }
}
