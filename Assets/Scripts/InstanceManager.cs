using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstanceManager : MonoBehaviour
{
    public static InstanceManager Instance { get; private set; }

    [SerializeField]
    GameObject _usingBoard;

    BoardSO _boardSO;
    UserOptionsSO _userOptionsSO;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        _boardSO = ScriptableObject.CreateInstance<BoardSO>();
        _boardSO.initializeBoard("Assets/JSONBoards/New/Board_1_Info.json", _usingBoard);

        _userOptionsSO = ScriptableObject.CreateInstance<UserOptionsSO>();
    }

    public BoardSO getBoardSOInstance() { return _boardSO; }
    public UserOptionsSO getUserOptionsSOInstance() { return _userOptionsSO; }
}
