using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Serialization;

public class JsonParser : MonoBehaviour
{
    public static JsonParser Instance;
    [Serializable]
    public struct Data
    {
        public List<Technos> TechnologyTree;
        public List<Descends> Descendants;
        public List<Auto> AutoTech;
    };

    [Serializable]
    public struct WordsStruct
    {
        public List<string> subjects;
        public List<string> verbs;
        public List<string> objects;
    };

    [Serializable]
    public struct Technos
    {
        public string name;
        public List<string> dependances;
        public WordsStruct words;
        public bool useable;
    };

    [Serializable]
    public struct Descends
    {
        public string name;
        public Vector4 limits;
        public Vector4 value;
        public bool useable;
    };

    [Serializable]
    public struct Auto
    {
        public string name;
        public string dependance;
        public Vector4 limits;
        public float time;
        public bool useable;
    };
    public Data data;
    [FormerlySerializedAs("nameFile")] [SerializeField] private string _nameFile;
    private void Start()
    {
        Instance = this;
        data = JsonUtility.FromJson<Data>(File.ReadAllText(Application.streamingAssetsPath + "/" + _nameFile));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
