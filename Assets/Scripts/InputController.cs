using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputController : MonoBehaviour
{

    [SerializeField]
    GameObject _camFocus;
    [SerializeField]
    GameObject _effectTile;

    // Private variables
    BoardSO _board;
    UserOptionsSO _userOptions;
    Tile _selectedTile = null;
    int _terrainMask;

    Camera _mainCam;
    Transform _mainCamTransform;

    List<GameObject> _ongoingEffects;
    List<Tile> _path;

    Propagation _prop;

    private void Start()
    {
        _terrainMask = 1 << LayerMask.NameToLayer("Terrain");

        _mainCam = Camera.main;
        _mainCamTransform = Camera.main.transform;

        _board = InstanceManager.Instance.getBoardSOInstance();
        _userOptions = InstanceManager.Instance.getUserOptionsSOInstance();

        _ongoingEffects = new List<GameObject>();
        _path = new List<Tile>();

        _prop = new Propagation();
    }

    private void Update()
    {
        // If the user is holding the left shift button, allow the user to move the camera focus across the xz plane
        if (Input.GetKey(KeyCode.LeftShift))
        {
            // Get horizontal input axis
            float horizontalInput = Input.GetAxis("Horizontal");
            // Get vertical input axis
            float verticalInput = Input.GetAxis("Vertical");

            Vector3 movement = new Vector3(horizontalInput, 0f, verticalInput) * 5 * Time.deltaTime;

            // Get the forward direction of the main camera
            Vector3 cameraForward = _mainCamTransform.forward;
            // Ignore the camera's y component to flatten the direction
            cameraForward.y = 0f;

            // Create a rotation that faces the camera's forward direction
            Quaternion targetRotation = Quaternion.LookRotation(cameraForward);

            // Rotate the movement vector using the target rotation
            movement = targetRotation * movement;

            _camFocus.transform.Translate(movement);
        }

        // If the user clicks the left mouse button
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Input.mousePosition;

            Ray ray = _mainCam.ScreenPointToRay(mousePos);
            RaycastHit hit;

            // If the mouse ray hits a panel, commit action (whatever that means right now)
            if (Physics.Raycast(ray, out hit, 100, _terrainMask))
            {
                (int, int, int) tID = raycastHitToTileID(hit);
                int currFeature = _userOptions.getCurrFeature();
                if (currFeature <= 1)
                {
                    handlePathing(_board.getTile(tID), currFeature);
                }
                else if (currFeature >= 2)
                {
                    handlePropagation(_board.getTile(tID), currFeature);
                }
            }
        }
    }

    (int, int, int) raycastHitToTileID(RaycastHit hit)
    {
        GameObject tile = hit.collider.gameObject;
        string tileName = tile.name;
        (int, int, int) id = _board.getIDFromTileName(tileName);
        return id;
    }

    void handlePathing(Tile t, int feature)
    {
        if (_selectedTile == null)
        {
            primePathing(t);
        }
        else
        {
            completePathing(t, feature);
        }
    }

    // Spawns effect_tile above given tile and save selected tile
    void primePathing(Tile t)
    {
        // Befre priming a new pathing attempt, delete any ongoing effects
        while(_ongoingEffects.Count > 0)
        {
            Destroy(_ongoingEffects[0]);
            _ongoingEffects.RemoveAt(0);
        }

        // Store selected tile
        _selectedTile = t;

        GameObject effect = Instantiate(_effectTile);
        effect.transform.position = _selectedTile.getPosition();
        effect.transform.localScale = new Vector3(90, 90, 105);
        // Store new effect in ongoing effect list
        _ongoingEffects.Add(effect);
    }

    void completePathing(Tile t, int feature)
    {
        if(feature == 0)
        {
            _path = _board.FindPathBFS(_selectedTile.getID(), t.getID());
        }
        else if(feature == 1)
        {
            _path = _board.FindPathAStar(_selectedTile.getID(), t.getID());
        }
        else
        {
            Debug.Log($"Impossible feature selected for pathfinding, index: {feature}");
        }
        

        foreach (Tile tile in _path)
        {
            GameObject effect = Instantiate(_effectTile);
            effect.transform.position = tile.getPosition();
            effect.transform.localScale = new Vector3(90, 90, 105);
            _ongoingEffects.Add(effect);
        }

        _selectedTile = null;
    }

    void handlePropagation(Tile t, int feature)
    {
        // Befre priming a new effect propagation, delete any ongoing effects
        while (_ongoingEffects.Count > 0)
        {
            Destroy(_ongoingEffects[0]);
            _ongoingEffects.RemoveAt(0);
        }

        HashSet<Tile> effectProp = new HashSet<Tile>();

        if (feature == 2)
        {
            effectProp = _prop.propagate(_board.getAllPanels(), t, "linear");
        }
        else if(feature == 3)
        {
            effectProp = _prop.propagate(_board.getAllPanels(), t, "circular");
        }
        else
        {
            Debug.Log($"Impossible feature selected for propagation, index: {feature}");
        }
        

        foreach (Tile tile in effectProp)
        {
            GameObject effect = Instantiate(_effectTile);
            effect.transform.position = tile.getPosition();
            effect.transform.localScale = new Vector3(90, 90, 105);
            _ongoingEffects.Add(effect);
        }

        _selectedTile = null;
    }
}
