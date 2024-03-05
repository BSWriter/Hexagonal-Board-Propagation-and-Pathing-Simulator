using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class Tile
{
    protected (int, int, int) _id;
    protected int _label;
    protected int _elevation;
    protected (int, int, int)[] _surroundingTiles;
    protected Vector3 _position;
    protected GameObject _fullObject;
    protected Vector3 _placementPos;

    // A* pathfinding variables
    protected int _gCost = -1; // Steps away from base tile
    protected int _hCost = -1; // Heuristic / Optimistic steps away from goal tile (Sum of Absolute Differences between x and y coordinates)
    protected int _fCost = -1; // Combined gCost and hCost
    protected Tile _linkTile = null; // Tile from which this tile was reached via pathfinding
    protected float _addedPos = 1000;
    protected Dictionary<string, float> _additionalHeuristic = new Dictionary<string, float>(); // Additional heuristics to be added to h value

    public Tile((int, int, int) idIn, int labelIn, int elevationIn, GameObject fullTile)
    {
        _id = idIn;
        _label = labelIn;
        if (_id.Item2 % 2 == 0)
        {
            _surroundingTiles = new (int, int, int)[] { (_id.Item1, _id.Item2, _id.Item3 - 1), // Center left adjacent tile
                                                        (_id.Item1, _id.Item2 + 1, _id.Item3 - 1), // Top left adjacent tile
                                                        (_id.Item1, _id.Item2 + 1, _id.Item3), // Top right adjacent tile 
                                                        (_id.Item1, _id.Item2, _id.Item3 + 1), // Center right adjacent tile
                                                        (_id.Item1, _id.Item2 - 1, _id.Item3), // Bottom right adjacent tile
                                                        (_id.Item1, _id.Item2 - 1, _id.Item3 - 1) // Bottom left adjacent tile
                                                        
            };
        }
        else
        {
            _surroundingTiles = new (int, int, int)[] { (_id.Item1, _id.Item2, _id.Item3 - 1), // Center left adjacent tile
                                                        (_id.Item1, _id.Item2 + 1, _id.Item3), // Top left adjacent tile
                                                        (_id.Item1, _id.Item2 + 1, _id.Item3 + 1), // Top right adjacent tile 
                                                        (_id.Item1, _id.Item2, _id.Item3 + 1), // Center right adjacent tile
                                                        (_id.Item1, _id.Item2 - 1, _id.Item3 + 1), // Bottom right adjacent tile
                                                        (_id.Item1, _id.Item2 - 1, _id.Item3) // Bottom left adjacent tile
            };
        }
        _elevation = elevationIn;
        _position = fullTile.transform.position;

        // GameObject initiallization
        _fullObject = fullTile;

        _placementPos = new Vector3(_fullObject.transform.position.x, _fullObject.transform.position.y + 1f, _fullObject.transform.position.z);



    }

    public void removeInvalidSurroudingTiles(HashSet<(int, int, int)> viableTiles, (int, int, int) id)
    {
        List<(int, int, int)> tmpSurrTiles = new List<(int, int, int)>(_surroundingTiles);

        for (int i = 0; i < 6; i++)
        {
            if (!viableTiles.Contains(_surroundingTiles[i]))
            {
                _surroundingTiles[i] = (-1, -1, -1);
            }
        }
    }

    public string addSurroundingTilesToString(string input)
    {
        string output = input + "\nSurrounding Tiles: ";
        foreach ((int, int, int) tile in _surroundingTiles)
        {
            output += tile + "->";
        }
        return output;
    }

    public int isIDSurrouding((int, int, int) idIn)
    {
        for (int i = 0; i < 6; i++)
        {
            if (_surroundingTiles[i] == idIn)
            {
                return i;
            }
        }
        Debug.Log($"(Tile/isIDSurrounding) ERROR! Id is not among current tile's surrounding tiles. Current Tile ID: {_id}, Surrounding Tile ID: {idIn}");
        return -1;
    }

    public bool isEqual(Tile t)
    {
        if (t.getID() == _id)
        {
            return true;
        }
        return false;
    }


    public (int, int, int) getTileIDInDir(int dir)
    {
        if (dir < 0)
        {
            dir += 6;
        }
        else if (dir >= 6)
        {
            dir -= 6;
        }

        try
        {
            (int, int, int) x = _surroundingTiles[dir];
        }
        catch (Exception e)
        {
            Debug.Log($"Error occured. Dir: {dir} \n Debug Log: {e}");
        }

        return _surroundingTiles[dir];
    }

    public void printSurroundingTiles()
    {
        string str = $"(Tile/printSurroundingTiles) Surrounding Tiles for {_id} \n";

        foreach ((int, int, int) x in _surroundingTiles)
        {
            str += $"{x} \n";
        }

        Debug.Log(str);
    }

    public int calculatefCost(Tile startTile, Tile goalTile, int gCost = -1)
    {
        (int, int, int) goalID = goalTile.getID();
        if (_gCost == -1)
        {
            _gCost = gCost;
        }

        _hCost = Math.Abs(_id.Item2 - goalID.Item2) + Math.Abs(_id.Item3 - goalID.Item3);
        int subFac = Math.Abs(_id.Item2 - goalID.Item2) / 2;
        if (_id.Item2 % 2 == 1 && Math.Abs(_id.Item2 - goalID.Item2) == 1)
        {
            subFac += 1;
        }
        if (_id.Item3 != goalID.Item3)
        {
            _hCost -= subFac;
        }

        if (_gCost == -1 || _hCost == -1)
        {
            Debug.Log($"Error for tile {_id}, gCost = {_gCost}, hCost = {_hCost}");
        }
        //_hCost = (int)Math.Abs(_position.x - goalTile.getPosition().x) + (int)Math.Abs(_position.z - goalTile.getPosition().z);
        // _hCost = (Vector3.Distance(_position, goalTile.getPosition()));
        _fCost = _gCost + _hCost;
        return _fCost;
    }

    public void resetPathVars()
    {
        _fCost = -1;
        _hCost = -1;
        _gCost = -1;
        _linkTile = null;
        _addedPos = 1000;
    }

    public void setGCost(int newGCost)
    {
        _gCost = newGCost;
    }

    public void setLinkedTile(Tile t)
    {
        _linkTile = t;
    }

    public void setAddedPos(float val)
    {
        _addedPos = val;
    }


    public (int, int, int) getID() { return _id; }
    public int getLabel() { return _label; }
    public int getElevation() { return _elevation; }
    public (int, int, int)[] getSurroundingTiles() { return _surroundingTiles; }
    public int getFCost() { return _fCost; }
    public int getGCost() { return _gCost; }
    public int getHCost() { return _hCost; }
    public Tile getLinkedTile() { return _linkTile; }
    public float getAddedPos() { return _addedPos; }
    public Vector3 getPosition() { return _position; }
    public GameObject getFullTile() { return _fullObject; }
    public Vector3 getPlacementPos() { return _placementPos; }

}