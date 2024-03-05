using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Propagation
{
    HashSet<Tile> _propTiles;
    HashSet<(int, int, int)> _viableTiles;
    List<(Tile, Tile)> _tilesToRemove;
    BoardSO _currBoard;
    UserOptionsSO _userOptions;

    public HashSet<Tile> propagate(HashSet<(int, int, int)> viableTilesIn, Tile startTile, string propType)
    {
        _currBoard = InstanceManager.Instance.getBoardSOInstance();
        _userOptions = InstanceManager.Instance.getUserOptionsSOInstance();

        int dir = _userOptions.getPropDir();
        float spread = _userOptions.getPropSpread();
        float reach = _userOptions.getPropReach();
        
        _propTiles = new HashSet<Tile>();
        _viableTiles = viableTilesIn;
        _tilesToRemove = new List<(Tile, Tile)>();
        

        switch (propType)
        {
            case "linear":
                linearPropRec(startTile, dir, spread, reach);
                break;
            case "circular":
                circlePropRec(_currBoard.getTile(startTile.getTileIDInDir(0)), 0, spread, 1, 0);
                circlePropRec(_currBoard.getTile(startTile.getTileIDInDir(1)), 1, spread, 1, 1);
                circlePropRec(_currBoard.getTile(startTile.getTileIDInDir(2)), 2, spread, 1, 2);
                circlePropRec(_currBoard.getTile(startTile.getTileIDInDir(3)), 3, spread, 1, 3);
                circlePropRec(_currBoard.getTile(startTile.getTileIDInDir(4)), 4, spread, 1, 4);
                circlePropRec(_currBoard.getTile(startTile.getTileIDInDir(5)), 5, spread, 1, 5);
                break;
            default:
                Debug.Log($"(TileProp/propagate) Propagation method '{propType}' not recognized");
                break;
        }

        foreach ((Tile t, Tile prevTile) in _tilesToRemove)
        {
            if (_propTiles.Contains(t))
            {
                _propTiles.Remove(t);
            }

        }

        return _propTiles;
    }


    void linearPropRec(Tile currTile, int dir, float spread, float reach)
    {
        string output = $"(TileProp/linearPropRec) Current tile: {currTile.getID()} ";

        if ((int)reach > 0)
        {
            (int, int, int) nextTID = currTile.getTileIDInDir(dir);
            if (_viableTiles.Contains(nextTID))
            {
                Tile nextTile = _currBoard.getTile(nextTID);
                _propTiles.Add(nextTile);
                output += $"\n Added tile {nextTID} to propageted tiles list.";
                linearPropRec(nextTile, dir, spread - 1f, reach - 1f);

            }

        }
        while ((int)spread > 0)
        {
            (int, int, int) spreadTID1 = currTile.getTileIDInDir(dir - (int)spread);
            if (_viableTiles.Contains(spreadTID1))
            {
                Tile spreadTile1 = _currBoard.getTile(spreadTID1);
                _propTiles.Add(spreadTile1);
                output += $"\n Added spread tile {spreadTID1} to propageted tiles list.";
                linearPropRec(spreadTile1, dir, spread - 1f, reach - 1f);
            }

            (int, int, int) spreadTID2 = currTile.getTileIDInDir(dir + (int)spread);
            if (_viableTiles.Contains(spreadTID2))
            {
                Tile spreadTile2 = _currBoard.getTile(spreadTID2);
                _propTiles.Add(spreadTile2);
                output += $"\n Added spread tile {spreadTID2} to propageted tiles list.";
                linearPropRec(spreadTile2, dir, spread - 1f, reach - 1f);
            }

            spread -= 1f;
        }

        // output += $"\n Added list of propagated tiles of count {prop.Count}";
        // Debug.Log(output);
    }

    void circlePropRec(Tile currTile, int dir, float spread, int currRadi, float dirAvg)
    {

        if (currRadi <= spread && !_propTiles.Contains(currTile))
        {
            _propTiles.Add(currTile);

            if (dirAvg < 0 || dirAvg > 5)
            {
                (int, int, int) exTile1ID = currTile.getTileIDInDir(0);
                if (_viableTiles.Contains(exTile1ID))
                {
                    Tile exTile1 = _currBoard.getTile(currTile.getTileIDInDir(0));
                    // circlePropRec(exTile1, 0, spread, currRadi + 1, dirAvg + ((-1 - dirAvg) / currRadi), obstructed);
                    circlePropRec(exTile1, 0, spread, currRadi + 1, -1f);
                }


                (int, int, int) exTile2ID = currTile.getTileIDInDir(5);
                if (_viableTiles.Contains(exTile2ID))
                {
                    Tile exTile2 = _currBoard.getTile(currTile.getTileIDInDir(5));
                    circlePropRec(exTile2, 5, spread, currRadi + 1, 5.5f);
                }
            }
            else
            {
                dir = Mathf.FloorToInt(dirAvg);

                List<(Tile, int, float)> nextTiles = new List<(Tile, int, float)>();

                (int, int, int) firstTID = currTile.getTileIDInDir(dir);
                if (_viableTiles.Contains(firstTID))
                {
                    nextTiles.Add((_currBoard.getTile(firstTID), dir, dirAvg + ((dir - dirAvg) / (currRadi + 1))));
                }

                // int otherDir = (dir + 1) % 6;
                (int, int, int) otherTID = currTile.getTileIDInDir(dir + 1);
                if (_viableTiles.Contains(otherTID))
                {
                    nextTiles.Add((_currBoard.getTile(otherTID), dir, dirAvg + ((dir + 1 - dirAvg) / (currRadi + 1))));
                    /*Debug.Log($"(TileProp/CirclePropRec) Calculating new dirAvg for tile {otherTID} from {currTile.getID()}. New Dir Avg: {dirAvg + ((dir + 1 + dirAvg) / (currRadi + 1))}" +
                        $"\n PrevDirAvg = {dirAvg}, Dir = {dir}, currRadi = {currRadi}");*/
                }

                if (dirAvg % 1 == 0)
                {
                    // int lastDir = ((dir - 1) < 0) ? 5 : (dir - 1);
                    (int, int, int) lastTID = currTile.getTileIDInDir(dir - 1);
                    if (_viableTiles.Contains(lastTID))
                    {
                        nextTiles.Add((_currBoard.getTile(lastTID), dir, dirAvg + ((dir - 1 - dirAvg) / (currRadi + 1))));
                    }
                }

                foreach ((Tile tile, int newDir, float newDirAvg) in nextTiles)
                {
                    circlePropRec(tile, newDir, spread, currRadi + 1, newDirAvg);
                }
            }

        }
    }

}


