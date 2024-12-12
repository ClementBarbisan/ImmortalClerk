using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEditor;

[ExecuteInEditMode]
public class JsonParser : MonoBehaviour
{
    public static JsonParser Instance;
    public bool Load = false;
    public bool Save = false;
    public bool DeleteAll;
    [Serializable]
    public struct DataTree
    {
        public string text;
        public string techno;
    };

    [Serializable]
    public struct Data
    {
        public List<JsonParser.Technos> TechnologyTree;
    };

    [Serializable]
    public class Word
    {
        public override string ToString()
        {
            return name;
        }

        public string name;
        [VectorLabels("Religion", "Social", "Science", "Conquest")]
        public Vector4 value;
        [HideInInspector]
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
        [HideInInspector]
        public bool useable;
        [HideInInspector]
        public bool learned;
        [VectorLabels("Religion", "Social", "Science", "Conquest")]
        public Vector4 limits;
    };

    public TechTree data;
    [FormerlySerializedAs("nameFile")] [SerializeField] private string _nameFile;

    private void Update()
    {
        if (Load)
        {
            string dataText = Resources.Load<TextAsset>(_nameFile).text;
            data.Data = JsonUtility.FromJson<Data>(dataText);
            Load = false;
        }

        if (Save)
        {
            string dataJson = JsonUtility.ToJson(data);
            System.IO.File.WriteAllText(Application.dataPath + "/Resources/"  + _nameFile + ".json", dataJson);
            AssetDatabase.Refresh();
            Save = false;
        }

        if (DeleteAll)
        {
            Regex reg = new Regex(@"(\.save)$");
            Directory.EnumerateFiles(@Application.persistentDataPath)
                .Where(file => reg.Match(file).Success).ToList()
                .ForEach(File.Delete);
            PlayerPrefs.DeleteAll();
            DeleteAll = false;
        }
    }

    private void Awake()
    {
        Instance = this;
        data.Data = JsonUtility.FromJson<Data>(Resources.Load<TextAsset>(_nameFile).text);
    }

    public Data GetData()
    {
        return (JsonUtility.FromJson<Data>(Resources.Load<TextAsset>(_nameFile).text));
    }

   
}
