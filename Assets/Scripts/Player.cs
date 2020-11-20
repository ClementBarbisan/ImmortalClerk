using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    public static Player Instance;
    public JsonParser.Data data;
    public int turn = 0;
    private Civilisation[] _civilisations;
    private RaycastHit2D hit;
    private float turns = 0;
    [SerializeField] private TextMeshProUGUI turnText;
    public void AddTurn()
    {
        for (int i = 0; i < _civilisations.Length; i++)
            _civilisations[i].AddTurn();
        turnText.text = "Turn " + turn;
    }

    private void Awake()
    {
        hit = new RaycastHit2D();
        Instance = this;
        turnText = GameObject.FindWithTag("Turn").GetComponent<TextMeshProUGUI>();
        // if (File.Exists(Application.persistentDataPath + "/" + gameObject.name + ".save"))
        // {
        //     data = JsonUtility.FromJson<JsonParser.Data>(File.ReadAllText(Application.persistentDataPath + "/" + gameObject.name + ".save"));
        // }
        // else
        // {
            data = JsonParser.Instance.GetData();
            for (int i = 0; i < data.TechnologyTree.Count; i++)
            {
                if (data.TechnologyTree[i].dependances.Count == 0)
                {
                    var tmpStruct = data.TechnologyTree[i];
                    tmpStruct.useable = true;
                    for (int j = 0; j < tmpStruct.words.objects.Count; j++)
                    {
                        var tmpStructDescends = tmpStruct.words.objects[j];
                        tmpStructDescends.useable = true;
                        tmpStruct.words.objects[j] = tmpStructDescends;
                    }
                    for (int j = 0; j < tmpStruct.words.subjects.Count; j++)
                    {
                        var tmpStructDescends = tmpStruct.words.subjects[j];
                        tmpStructDescends.useable = true;
                        tmpStruct.words.subjects[j] = tmpStructDescends;
                    }
                    for (int j = 0; j < tmpStruct.words.verbs.Count; j++)
                    {
                        var tmpStructDescends = tmpStruct.words.verbs[j];
                        tmpStructDescends.useable = true;
                        tmpStruct.words.verbs[j] = tmpStructDescends;
                    }
                    data.TechnologyTree[i] = tmpStruct;
                }
            }
        // }
    }

    // Start is called before the first frame update
    void Start()
    {
        _civilisations = FindObjectsOfType<Civilisation>();
    }

    public void Done(string level)
    {
        return;
    }

    public void Close()
    {
        for (int i = 0; i < _civilisations.Length; i++)
            _civilisations[i].gameObject.SetActive(true);
    }

    public void Open(GameObject obj)
    {
        for (int i = 0; i < _civilisations.Length; i++)
        {
            if (_civilisations[i].gameObject != obj)
                _civilisations[i].gameObject.SetActive(false);
        }

    }

    private void OnApplicationQuit()
    {
        File.WriteAllText(Application.persistentDataPath + "/" + gameObject.name + ".save", JsonUtility.ToJson(data));
    }
    
    // Update is called once per frame
    void Update()
    {
        turns += Time.deltaTime;
        if (turn != (int)(turns))
        {
            turn = (int)(turns);
            AddTurn();
        }
    }
}
