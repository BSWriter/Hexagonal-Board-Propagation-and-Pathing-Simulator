using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System;

using UnityEditor;
using NUnit.Framework;
using Unity.VisualScripting;



public class LevelGenerator : MonoBehaviour
{
    // Path to json file 
    static string jsonFilePath = "Assets/JSONBoards/Base/Board1.json";

    // Paths to prefab assets
    static string panelPrefab = "Assets/Prefabs/Tile.prefab";
    static string wallPrefab = "Assets/Prefabs/Wall.prefab";

    // Save path of board prefab
    static string savePrefabPath = "Assets/Prefabs/Board1.prefab";

    // Name of new board
    static string newBoardName = "Board_1";

    // Save path of new board json data
    static string filePath = "Assets/JSONBoards/New/Board_1_Info.json";


    //Creates a new menu (Examples) with a menu item (Create Prefab)
    [MenuItem("Generate/Generate Board Prefab")]
    static void CreatePrefab()
    {
        try
        {
            // Read the JSON file
            string jsonContent = File.ReadAllText(jsonFilePath);

            // Parse the JSON content
            dynamic jsonObject = JsonConvert.DeserializeObject(jsonContent);

            string jsonRows = jsonObject["rows"];
            string jsonCols = jsonObject["cols"];
            dynamic tiles = jsonObject["panels"];

            int rows = int.Parse(jsonRows);
            int cols = int.Parse(jsonCols);

            AddBoardPrefab(tiles, panelPrefab, wallPrefab);

            saveBoardToJson(rows, cols, tiles);
        }
        catch (FileNotFoundException)
        {
            Debug.Log("The JSON file does not exist.");
        }
        catch (JsonException)
        {
            Debug.Log("Invalid JSON format.");
        }
        catch (Exception ex)
        {
            Debug.Log("An error occurred: " + ex.Message);
            Debug.Log("Full error details: " + ex.ToString());
        }
    }

    static void AddBoardPrefab(dynamic tiles, string panelPath, string wallPath)
    {
        //Check if the Prefab and/or name already exists at the path
        if (AssetDatabase.LoadAssetAtPath(panelPath, typeof(GameObject)) && AssetDatabase.LoadAssetAtPath(wallPath, typeof(GameObject)))
        {
            /*Debug.Log("Panel exists!");
            Debug.Log("Wall exists!");*/
            GameObject board = new GameObject(newBoardName);
            GameObject panelTemplate = (AssetDatabase.LoadAssetAtPath(panelPath, typeof(GameObject))).GameObject();

            GameObject wallTemplate = (AssetDatabase.LoadAssetAtPath(wallPath, typeof(GameObject))).GameObject();

            // Go through Tiles for the first time, instantiating viable panels and recording maximum elevation
            int maxElevation = 1;
            foreach (JToken token in tiles)
            {
                // Create traversable panel if the label is not 0 (Void) or greater than 4 (Unrecognized)
                if ((int)token["label"] > 0 && (int)token["label"] < 4)
                {
                    int elevation = (int)token["elevation"];
                    (int, int) pos = ((int)token["grid position"][0], (int)token["grid position"][1]);
                    (int, int, int) id = (elevation, pos.Item1, pos.Item2);

                    GameObject panel = Instantiate(panelTemplate);
                    panel.name = id + "_Panel";
                    panel.transform.position = new Vector3((float)token["center"][1], 0.01f + (((int)token["elevation"] - 1) * 3), (float)token["center"][0]);
                    panel.transform.SetParent(board.transform);

                    if (elevation > maxElevation)
                    {
                        maxElevation = elevation;
                    }

                    // Instantiate the pillars underneath the panel
                    while (elevation > 1)
                    {
                        elevation--;
                        id = (elevation, pos.Item1, pos.Item2);
                        GameObject wall = Instantiate(wallTemplate);
                        wall.name = id + "_Pillar";
                        wall.transform.position = new Vector3((float)token["center"][1], 1f + ((elevation - 1) * 3), (float)token["center"][0]);
                        wall.transform.SetParent(board.transform);

                        // Deactivate pillars and panels above the 1st level
                        if (elevation > 1)
                        {
                            panel.SetActive(false);
                            wall.SetActive(false);
                        }
                    }
                }
            }

            // Add 1 to maxElevation to add another top layer of walls to the map
            maxElevation += 1;
            // Go through Tiles second time, creating all levels of elevation for walls (deactivate any walls above 1st level)
            foreach (JToken token in tiles)
            {
                // Match the wall label with the wall prefab
                if ((int)token["label"] == 4)
                {
                    (int, int) pos = ((int)token["grid position"][0], (int)token["grid position"][1]);
                    int tempElevation = maxElevation;

                    while (tempElevation > 1)
                    {
                        tempElevation--;
                        (int, int, int) id = (tempElevation, pos.Item1, pos.Item2);
                        GameObject wall = Instantiate(wallTemplate);
                        wall.name = id + "_Wall";
                        // wall.transform.position = new Vector3((float)token["center"][1], 1f + ((tempElevation - 2) * 3), (float)token["center"][0]);
                        wall.transform.position = new Vector3((float)token["center"][1], 1f + ((tempElevation - 1) * 3), (float)token["center"][0]);
                        wall.transform.SetParent(board.transform);

                        // Deactivate walls above the 1st level
                        if (tempElevation > 1)
                        {
                            wall.SetActive(false);
                        }


                    }

                }

            }

            PrefabUtility.SaveAsPrefabAsset(board, savePrefabPath);
        }
        //If the name doesn't exist, create the new Prefab
        else
        {
            Debug.Log("Panel and/or Wall prefab does not exist.");
        }

    }

    static void saveBoardToJson(int rowsIn, int colsIn, dynamic tilesIn)
    {
        BoardInfo data = new BoardInfo();
        data.rows = rowsIn;
        data.cols = colsIn;
        data.tileInfo = new List<TileInfo>();

        foreach (JToken token in tilesIn)
        {
            TileInfo entry = new TileInfo();
            entry.label = (int)token["label"];
            entry.gridPos = ((int)token["grid position"][0], (int)token["grid position"][1]);
            entry.center = ((float)token["center"][0], (float)token["center"][1]);
            entry.elevation = (int)token["elevation"];
            entry.surrounding = new List<(int, int)>();
            foreach (JToken p in token["surrounding"])
            {
                entry.surrounding.Add(((int)p[0], (int)p[1]));
            }

            data.tileInfo.Add(entry);
        }

        string json = JsonConvert.SerializeObject(data, Formatting.Indented);

        Debug.Log(json);

        

        // Write the JSON string to the file
        File.WriteAllText(filePath, json);

        Debug.Log("Data saved to: " + filePath);
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

}

