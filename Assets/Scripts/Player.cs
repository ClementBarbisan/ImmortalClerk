using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public class Player : MonoBehaviour
{
    public static Player Instance;
    public JsonParser.Data data;
    public int turn = 0;
    private Civilisation[] _civilisations;
    private RaycastHit2D hit;
    private float turns = 0;
    private TextMeshProUGUI turnText;
    [SerializeField] private GameObject _prefabWordKeeper;
    [SerializeField] private Transform _parent;
    public bool debug = true;
    private int _turnPast;
    public bool wordKeep = true;
    public int lastTurn;
    private List<GameObject> _wordKeepers;
    private bool _open;

    public void AddTurn()
    {
        for (int i = 0; i < _civilisations.Length; i++)
            _civilisations[i].AddTurn();
        turnText.text = "Year " + turn;
    }

    private void Awake()
    {
        // hit = new RaycastHit2D();
        Instance = this;
        _wordKeepers = new List<GameObject>();
        turnText = GameObject.FindWithTag("Turn").GetComponent<TextMeshProUGUI>();
        if (debug || !File.Exists(Application.persistentDataPath + "/" + gameObject.name + ".save"))
        {
            data = JsonParser.Instance.GetData();
            for (int i = 0; i < data.TechnologyTree.Count; i++)
            {
                if (data.TechnologyTree[i].dependances.Count == 0)
                {
                    var tmpStruct = data.TechnologyTree[i];
                    tmpStruct.useable = true;
                    tmpStruct.learned = true;
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
                // else if (debug)
                // {
                //     JsonParser.Technos tmpStruct = data.TechnologyTree[i];
                //     tmpStruct.useable = Random.Range(0, 5) < 2;
                //     tmpStruct.learned = tmpStruct.useable;
                //     data.TechnologyTree[i] = tmpStruct;
                // }
            }
            _turnPast = Random.Range(0, 5);
        }
        else
        {
            if (PlayerPrefs.HasKey("year"))
                turns = PlayerPrefs.GetInt("year");
            if (PlayerPrefs.HasKey("debug"))
                debug = bool.Parse(PlayerPrefs.GetString("debug"));
            if (File.Exists(Application.persistentDataPath + "/" + gameObject.name + ".save"))
            {
                data = JsonUtility.FromJson<JsonParser.Data>(File.ReadAllText(Application.persistentDataPath + "/" + gameObject.name + ".save"));
            }
            _turnPast = Random.Range(20, 30);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        _civilisations = FindObjectsOfType<Civilisation>();
    }

    public void Done(string level)
    {
        if (level == "end")
        {
            PlayerPrefs.DeleteAll();
            File.Delete(Application.persistentDataPath + "/" + gameObject.name + ".save");
            for (int i = 0; i < _civilisations.Length; i++)
            {
                File.Delete(Application.persistentDataPath + "/" + _civilisations[i].gameObject.name + ".save");
            }
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
        else
        {
            bool fail = true;
            Notifications.Instance.gameObject.SetActive(true);
            Notifications.Instance.TimeVisible += 2.5f;
            Notifications.Instance.AddText("Civilisation fail : " + level);
            for (int i = 0; i < _civilisations.Length; i++)
            {
                if (!_civilisations[i].done)
                {
                    fail = false;
                    break;
                }
            }

            if (fail)
            {
                File.Delete(Application.persistentDataPath + "/" + gameObject.name + ".save");
                for (int i = 0; i < _civilisations.Length; i++)
                {
                    File.Delete(Application.persistentDataPath + "/" + _civilisations[i].gameObject.name + ".save");
                }
                PlayerPrefs.DeleteAll();
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 2);
            }
        }
    }

    public void DeleteWordKeeper(GameObject obj)
    {
        _wordKeepers.Remove(obj);
    }

    public void Close()
    {
        _open = false;
        for (int i = 0; i < _civilisations.Length; i++)
            _civilisations[i].gameObject.SetActive(true);
        for (int i = 0; i < _wordKeepers.Count; i++)
        {
            _wordKeepers[i].gameObject.SetActive(true);
        }
    }

    public void Open(GameObject obj)
    {
        _open = true;
        for (int i = 0; i < _civilisations.Length; i++)
        {
            if (_civilisations[i].gameObject != obj)
                _civilisations[i].gameObject.SetActive(false);
        }
        for (int i = 0; i < _wordKeepers.Count; i++)
        {
            if (_wordKeepers[i].gameObject != obj)
                _wordKeepers[i].gameObject.SetActive(false);
        }

    }

    private void OnApplicationQuit()
    {
        File.WriteAllText(Application.persistentDataPath + "/" + gameObject.name + ".save", JsonUtility.ToJson(data));
        PlayerPrefs.SetInt("year", turn);
        PlayerPrefs.Save();
    }
    
    void Update()
    {
        turns += Time.deltaTime;
        if (turn != (int)(turns))
        {
            turn = (int)(turns);
            AddTurn();
        }

        if (wordKeep && _turnPast < turn - lastTurn)
        {
            wordKeep = false;
            // lastTurn = turn;
            GameObject go = Instantiate(_prefabWordKeeper, new Vector2(Screen.width / 2f, Screen.height / 2f), Quaternion.identity, _parent);
            go.transform.localScale = Vector3.one;
            GameObject openButton = go.GetComponent<WordKeeper>().buttonOpen;
            openButton.transform.position = new Vector3(Random.Range(Screen.width / 4, Screen.width / 2 + Screen.width / 4),
                Random.Range(Screen.height / 4, Screen.height / 2 + Screen.height / 4), 0);
            _wordKeepers.Add(go);
            if (_open)
                go.SetActive(false);
            _turnPast = Random.Range(10, 20);
        }
    }
}
