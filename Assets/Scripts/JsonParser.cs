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
    };
    [Serializable]
    public struct Word
    {
        public string name;
        public float[] value;
        public bool useable;
    }

    [Serializable]
    public struct WordsStruct
    {
        public List<Word> subjects;
        public List<Word> verbs;
        public List<Word> objects;
    };

    [Serializable]
    public struct Technos
    {
        public string name;
        public List<string> dependances;
        public WordsStruct words;
        public string type;
        public float time;
        public bool useable;
        public float[] limits;
    };

    public Data data;
    [FormerlySerializedAs("nameFile")] [SerializeField] private string _nameFile;
    private void Awake()
    {
        Instance = this;
        data = JsonUtility.FromJson<Data>(Resources.Load<TextAsset>(_nameFile).text);
    }

    public Data GetData()
    {
        return (JsonUtility.FromJson<Data>(Resources.Load<TextAsset>(_nameFile).text));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
