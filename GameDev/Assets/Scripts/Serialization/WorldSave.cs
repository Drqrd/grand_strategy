using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Runtime.Serialization;

using Serialization.Surrogates;

using static WorldData;
public class WorldSave : MonoBehaviour
{
    [SerializeField] private string worldName;
    
    public World world { get; private set; }
    public string WorldName { get { return worldName; } }

    public void Start()
    {
        World w = null;
        TryGetComponent(out w);
        world = w;
    }

    public void SaveWorld(World world, string fileName)
    {
        string filePath = Application.persistentDataPath + "/" + fileName + ".dat";

        Save saveData = world.worldData.save;

        BinaryFormatter bf = GenerateBinaryFormatter();

        FileStream file = File.Open(filePath, FileMode.OpenOrCreate);
        bf.Serialize(file,saveData);
        file.Close();

        UnityEngine.Debug.Log("World Saved!");
    }

    public void LoadWorld(string fileName)
    {
        string filePath = Application.persistentDataPath + "/" + fileName + ".dat";
        if (File.Exists(filePath))
        {
            BinaryFormatter bf = GenerateBinaryFormatter();

            FileStream file = File.Open(filePath, FileMode.Open);
            Save saveData = (Save)bf.Deserialize(file);
            file.Close();

            world = gameObject.AddComponent<World>();

            world.LoadWorld(saveData);

            UnityEngine.Debug.Log("World Loaded!");
        }
        else { UnityEngine.Debug.LogError("ERROR LOADING WORLD \"" + fileName + "\""); }
    }

    // Adds a Vector3, Color surrogate as Unity Vector3 and Color is not serializable
    private BinaryFormatter GenerateBinaryFormatter()
    {
        BinaryFormatter bf = new BinaryFormatter();

        SurrogateSelector surrogateSelector = new SurrogateSelector();
        Vector3SerializationSurrogate vector3SS = new Vector3SerializationSurrogate();
        ColorSerializationSurrogate colorSS = new ColorSerializationSurrogate();

        surrogateSelector.AddSurrogate(typeof(Vector3), new StreamingContext(StreamingContextStates.All), vector3SS);
        surrogateSelector.AddSurrogate(typeof(Color), new StreamingContext(StreamingContextStates.All), colorSS);

        bf.SurrogateSelector = surrogateSelector;

        return bf;
    }
}