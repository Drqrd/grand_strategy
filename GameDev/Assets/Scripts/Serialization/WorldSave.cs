using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

using WorldGeneration.TectonicPlate.Objects;

public class WorldSave : MonoBehaviour
{
    WorldData worldData;
    public void SaveWorld(World world, string fileName)
    {
        // Get data into Serializable
        WorldData saveData = new WorldData();
        saveData.points = new Point[world.Plates.Length][];
        saveData.triangles = new int[world.Plates.Length][];
        for (int a = 0; a < world.Plates.Length; a++)
        {
            saveData.points[a] = world.Plates[a].Points;
            saveData.triangles[a] = world.Plates[a].Triangles;
        }

        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Open(Application.persistentDataPath + "/" + fileName + ".dat", FileMode.Open);
        bf.Serialize(file,saveData);
        file.Close();
    }

    public WorldData LoadWorld(string fileName)
    {

        if (File.Exists(Application.persistentDataPath + "/" + fileName + ".dat"))
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(Application.persistentDataPath + "/" + fileName + ".dat", FileMode.Open);
            WorldData saveData = (WorldData)bf.Deserialize(file);
            file.Close();

            return saveData;
        }

        return null;
    }
}

[Serializable]
public class WorldData
{
    public Point[][] points;
    public int[][] triangles;
}