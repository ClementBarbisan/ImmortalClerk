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
    public struct DataTree
    {
        public string text;
        public string techno;
    };

    [Serializable]
    public struct Data
    {
        public List<Technos> TechnologyTree;
    };
    [Serializable]
    public class Word
    {
        public override string ToString()
        {
            return name;
        }

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
    public class Technos
    {
        public override string ToString()
        {
            return name;
        }

        public string name;
        public List<string> dependances;
        public WordsStruct words;
        public string type;
        public float time;
        public bool useable;
        public bool learned;
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

   
}
