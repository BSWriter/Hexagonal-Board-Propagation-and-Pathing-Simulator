using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using System.Linq;
using static UnityEngine.EventSystems.EventTrigger;

public class BoardSO : ScriptableObject
{

    Dictionary<(int, int, int), Tile> _tiles = new Dictionary<(int, int, int), Tile>();
    HashSet<(int, int, int)> _hashTiles;
    HashSet<(int, int, int)> _hashPanels;

    Dictionary<Vector3, (int, int, int)> _posToID;

    int[] _panelLabels = { 1, 2, 3 };
    int _voidLabel = 0;
    int _wallLabel = 4;

    public void initializeBoard(string pathIn, GameObject boardIn)
    {
        BoardInfo boardInfo = JsonConvert.DeserializeObject<BoardInfo>(loadBoard(pathIn));
        Dictionary<(int, int, int), GameObject> matchingTileGOs = extractAllTiles(boardIn);
        _posToID = new Dictionary<Vector3, (int, int, int)>();
        _hashPanels = new HashSet<(int, int, int)>();
        _hashTiles = new HashSet<(int, int, int)>();

        foreach (TileInfo t in boardInfo.tileInfo)
        {
            int elevation = t.elevation;
            (int, int, int) tID = (elevation, t.gridPos.Item1, t.gridPos.Item2);
            GameObject tileGO = matchingTileGOs[tID];

            // Create tile instance and add to tile dict by ID
            // Add tile to panel list if it has viable label
            Tile tile = new Tile(tID, t.label, elevation, tileGO); ;
            if (_panelLabels.Contains(t.label))
            {
                _hashPanels.Add(tID);
            }
            _tiles[tID] = tile;

            // Create link between Tile GameObject position and Tile Id
            _posToID.Add(tileGO.transform.position, tID);

            // If the tile is a panel, decrease elevation and add every pillar id beneath panel as a viable panel
            while (elevation > 1)
            {
                elevation--;
                (int, int, int) pillarID = (elevation, t.gridPos.Item1, t.gridPos.Item2);
                if (_tiles.ContainsKey(pillarID))
                {
                    Debug.Log($"(Board) Tried adding pillar to tile dictionary, but tile already contains key of id: {pillarID}");
                }
                else
                {
                    _tiles[pillarID] = new Tile(pillarID, _wallLabel, elevation, matchingTileGOs[pillarID]);
                }
            }
        }

        // Clean up panel data, removing any surrounding tiles that are not of the same elevation
        foreach ((int, int, int) tID in _tiles.Keys)
        {
            _tiles[tID].removeInvalidSurroudingTiles(new HashSet<(int, int, int)>(_tiles.Keys), tID);
        }

        // Store ids of panels and tiles for quick access
        _hashTiles = _tiles.Keys.ToHashSet();
    }

    string loadBoard(string boardFilePath)
    {
        // string filePath = "Assets/JSONBoards/New/Board_1_Info.json";

        if (File.Exists(boardFilePath))
        {
            string json = File.ReadAllText(boardFilePath);
            return json;
        }
        else
        {
            Debug.LogWarning($"{boardFilePath} file not found");
            return null;
        }
    }

    // Return all tiles of the board gameobject, using unique ids as keys
    Dictionary<(int, int, int), GameObject> extractAllTiles(GameObject board)
    {
        Dictionary<(int, int, int), GameObject> boardDict = new Dictionary<(int, int, int), GameObject>();

        int childCount = board.transform.childCount;

        for (int i = 0; i < childCount; i++)
        {
            GameObject childTile = board.transform.GetChild(i).gameObject;

            string fullTileName = childTile.name;

            // Get the tile's id from the name and add to dictionary
            string idString = fullTileName.Substring(0, fullTileName.IndexOf("_"));
            int[] idValues = idString.Trim('(', ')').Split(',').Select(int.Parse).ToArray();
            (int, int, int) tileID = (idValues[0], idValues[1], idValues[2]);

            boardDict[tileID] = childTile;
        }
        return boardDict;
    }

    public List<Tile> FindPathBFS((int, int, int) start, (int, int, int) goal)
    {
        Queue<(int, int, int)> queue = new Queue<(int, int, int)>();
        HashSet<(int, int, int)> visited = new HashSet<(int, int, int)>();
        Dictionary<(int, int, int), (int, int, int)> parent = new Dictionary<(int, int, int), (int, int, int)>();

        // If the goal is not among the viable tiles, return null and give debug message
        if (!_hashPanels.Contains(goal))
        {
            Debug.Log($"(BoardManager/FindPath) Goal {goal} is not among the tiles which the entity can reach. Cancelling pathfinding.");
            return new List<Tile>();
        }

        queue.Enqueue(start);
        visited.Add(start);
        parent[start] = start;
        (int, int, int) current = (-1, -1, -1);

        while (queue.Count > 0)
        {
            current = queue.Dequeue();
            // If the current tile is the goal tile, reconstruct the path using the stored parent path
            if (current == goal)
            {
                return ReconstructPath(parent, start, goal);
            }
            // For each neighbor of the tile, enqueue the tile if it is viable and not already visited
            foreach (var neighbor in _tiles[current].getSurroundingTiles())
            {
                if (!visited.Contains(neighbor) && (_hashPanels.Contains(neighbor) || neighbor == goal))
                {
                    queue.Enqueue(neighbor);
                    visited.Add(neighbor);
                    parent[neighbor] = current;
                }
            }
        }

        string str = $"(BoardManager/FindPath) No viable path found. Last current tile: {current}, Start tile: {start}, Goal tile: {goal}, Size of Reconstructed path: {ReconstructPath(parent, start, current).Count}";
        str = _tiles[current].addSurroundingTilesToString(str);
        Debug.Log(str);
        return null; // No path found
    }

    private List<Tile> ReconstructPath(Dictionary<(int, int, int), (int, int, int)> parent, (int, int, int) start, (int, int, int) goal)
    {
        List<Tile> path = new List<Tile>();
        Tile current = _tiles[goal];

        while (current.getID() != start)
        {
            path.Add(current);
            current = _tiles[parent[current.getID()]];
        }
        path.Reverse();
        return path;
    }

    public List<Tile> FindPathAStar((int, int, int) start, (int, int, int) goal)
    {
        // ----------- Initial set up for pathfinding ----------- 

        // If the goal is not among the viable tiles, return null and give debug message
        if (!_hashPanels.Contains(goal))
        {
            Debug.Log($"(BoardManager/FindPath) Goal {goal} is not among the tiles which the entity can reach. Cancelling pathfinding.");
            return new List<Tile>();
        }

        // ----------- Start of pathfinding ----------- 
        Tile startTile = _tiles[start];
        startTile.setGCost(0);
        startTile.calculatefCost(_tiles[start], _tiles[goal]);
        SortedDictionary<(int, (int, int, int)), Tile> searching = new SortedDictionary<(int, (int, int, int)), Tile>();
        HashSet<Tile> travelled = new HashSet<Tile>();
        searching.Add((startTile.getFCost(), start), startTile);

        while (searching.Any())
        {
            // Get the Tile with the lowest fCost
            Tile currTile = searching.First().Value;
/*            string s = $"Expanding on tile {currTile.getID()} (f:{currTile.getFCost()},g:{currTile.getGCost()},h{currTile.getHCost()}). Other options were: \n";
            foreach (Tile t in searching.Values)
            {
                s += $"Tile {t.getID()} (f:{t.getFCost()},g:{t.getGCost()},h{t.getHCost()})   &&   ";
            }
            Debug.Log(s);*/
            searching.Remove(searching.First().Key);
            travelled.Add(currTile);

            // If the tile is the goal, retrace and return the path
            if (currTile.getID() == goal)
            {
                List<Tile> path = new List<Tile>();
                while (currTile.getID() != start)
                {
                    path.Add(currTile);
                    currTile = currTile.getLinkedTile();
                }

                path.Reverse();
/*                string res = $"Start Tile {start}, End Tile {goal}, Returning path: \n";
                foreach (Tile tile in path)
                {
                    res += $"{tile.getID()} (f:{tile.getFCost()}, g:{tile.getGCost()}, h:{tile.getHCost()}): ";
                    foreach ((int, int, int) surrID in tile.getSurroundingTiles())
                    {
                        res += $"Surrounding tile {surrID} stats: (f:{_tiles[surrID].getFCost()}, g:{_tiles[surrID].getGCost()}, h:{_tiles[surrID].getHCost()})  |||  ";
                    }
                    res += $" \n";
                }
                Debug.Log(res);*/
                return path;
            }

            foreach ((int, int, int) tID in currTile.getSurroundingTiles().Where(tID => (_hashPanels.Contains(tID) || tID == goal) && !travelled.Contains(_tiles[tID])))
            {
                Tile possibleTile = _tiles[tID];
                int newGCost = currTile.getGCost() + 1;

                // If the tile is not queued for searching, queue it up
                if (!searching.ContainsKey((possibleTile.getFCost(), tID)))
                {
                    possibleTile.resetPathVars();
                    possibleTile.setLinkedTile(currTile);
                    possibleTile.calculatefCost(_tiles[start], _tiles[goal], newGCost);
                    // Debug.Log($"Possible Tile {tID} added to search queue. Stats: f={possibleTile.getFCost()}, g={possibleTile.getGCost()}, h={possibleTile.getHCost()}");
                    searching.Add((possibleTile.getFCost(), tID), possibleTile);
                }

                // If the tile is already queued to be search but this path is more optimal, change it's gCost and recalculate fCost
                if (searching.ContainsKey((possibleTile.getFCost(), tID)) && newGCost < possibleTile.getGCost())
                {
                    possibleTile.setGCost(newGCost);
                    possibleTile.calculatefCost(_tiles[start], _tiles[goal]);
                    possibleTile.setLinkedTile(currTile);
                }

            }
        }
        Debug.Log($"Goal tile never reached.");
        return null;
    }

    public (int, int, int) getIDFromTileName(string s)
    {
        string idString = s.Substring(0, s.IndexOf("_"));
        int[] idValues = idString.Trim('(', ')').Split(',').Select(int.Parse).ToArray();
        return (idValues[0], idValues[1], idValues[2]);
    }

    public Tile getTile((int, int, int) id)
    {
        return _tiles[id];
    }

    public HashSet<(int, int, int)> getAllTiles() { return _hashTiles; }
    public HashSet<(int, int, int)> getAllPanels() { return _hashPanels; }
}

class BoardInfo
{
    public int rows;
    public int cols;
    public List<TileInfo> tileInfo;
}

class TileInfo
{
    public int label;
    public (int, int) gridPos;
    public List<(int, int)> surrounding;
    public (float, float) center;
    public int elevation;
}


