using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class WordKeeper : MonoBehaviour
{
    [SerializeField] private GameObject _prefabTechno;
    [SerializeField] private GameObject _prefabConcept;
    [SerializeField] private GameObject _panelDrawing;
    [SerializeField] private GameObject _panelConcept;
    [SerializeField] private Transform _posSubject;
    [SerializeField] private Transform _posVerb;
    [SerializeField] private Transform _posObject;
    
    private JsonParser.Data _data;
    private JsonParser.Technos _result;
    public GameObject buttonOpen;
    public GameObject buttonClose;

    private JsonParser.Word _currentSubject;
    private JsonParser.Word _currentVerb;
    private JsonParser.Word _currentObject;

    private bool _wordsSets = false;
    private JsonParser.Word _currentWord;


    private void Start()
    {
        _data = Player.Instance.data;
        var technos = _data.TechnologyTree.FindAll(x => x.useable && x.dependances.Count > 0);
        var general = _data.TechnologyTree.Find(x => x.name == "general");
        _currentSubject = general.words.subjects[Random.Range(0, general.words.subjects.Count)];
        if (technos.Count <= 0)
        {
            Player.Instance.wordKeep = true;
            Player.Instance.lastTurn = Player.Instance.turn;
            Player.Instance.DeleteWordKeeper(this.gameObject);
            Destroy(gameObject);
        }

        int index = Random.Range(0, technos.Count);
        var tech1 = _data.TechnologyTree.Find(x => x.name == technos[index].dependances[0]);
        var tech2 = _data.TechnologyTree.Find(x => x.name == technos[index].dependances[1]);
        if (tech1.words.verbs.Count > 0)
        {
                _currentObject = tech2.words.objects.Find(x => x.useable);
                _currentVerb = tech1.words.verbs.Find(x => x.useable);
                _wordsSets = true;
                _result = technos[index];
        }
        if (tech2.words.verbs.Count > 0)
        {
                _currentObject = tech1.words.objects.Find(x => x.useable);
                _currentVerb = tech2.words.verbs.Find(x => x.useable);
                _wordsSets = true;
                _result = technos[index];
        }
        if (technos[index].words.verbs.Count > 0)
            _currentWord = technos[index].words.verbs.Find(x => x.useable == false);
        else
        {
            _currentWord = technos[index].words.objects.Find(x => x.useable == false);
        }

        if (!_wordsSets)
        {
            Player.Instance.wordKeep = true;
            Player.Instance.lastTurn = Player.Instance.turn;
            Player.Instance.DeleteWordKeeper(this.gameObject);
            Destroy(gameObject);
        }

    GameObject subject = Instantiate(_prefabTechno, Vector3.zero, Quaternion.identity, _posSubject);
        subject.transform.localPosition = Vector3.zero;
        subject.GetComponentInChildren<TextMeshProUGUI>().text = _currentSubject.name;
        if (Resources.Load<Sprite>(_currentSubject.name))
            subject.GetComponentInChildren<Image>().sprite = Resources.Load<Sprite>(_currentSubject.name);
        GameObject verb = Instantiate(_prefabTechno, Vector3.zero, Quaternion.identity, _posVerb);
        verb.GetComponentInChildren<TextMeshProUGUI>().text = _currentVerb.name;
        verb.transform.localPosition = Vector3.zero;
        if (Resources.Load<Sprite>(_currentVerb.name))
            verb.GetComponentInChildren<Image>().sprite = Resources.Load<Sprite>(_currentVerb.name);
        GameObject obj = Instantiate(_prefabTechno, Vector3.zero, Quaternion.identity, _posObject);
        obj.GetComponentInChildren<TextMeshProUGUI>().text = _currentObject.name;
        obj.transform.localPosition = Vector3.zero;
        if (Resources.Load<Sprite>(_currentObject.name))
            obj.GetComponentInChildren<Image>().sprite = Resources.Load<Sprite>(_currentObject.name);
        for (int i = 0; i < _data.TechnologyTree.Count; i++)
        {
            if (!_data.TechnologyTree[i].learned)
                continue;
            GameObject go = Instantiate(_prefabConcept, Vector3.zero, Quaternion.identity, _panelConcept.transform);
            if (_data.TechnologyTree[i].name == _result.name)
            {
                go.GetComponent<Button>().onClick.AddListener(delegate
                {
                    bool foundWord = false;
                    _currentWord.useable = true;
                    for (int j = 0; j < _result.words.verbs.Count; j++)
                    {
                        if (_result.words.verbs[j].name == _currentWord.name)
                        {
                            _result.words.verbs[j] = _currentWord;
                            foundWord = true;
                            break;
                        }
                    }

                    if (!foundWord)
                    {
                        for (int j = 0; j < _result.words.objects.Count; j++)
                        {
                            if (_result.words.objects[j].name == _currentWord.name)
                            {
                                _result.words.objects[j] = _currentWord;
                                break;
                            }
                        }
                    }

                    for (int j = 0; j < _data.TechnologyTree.Count; j++)
                    {
                        if (_data.TechnologyTree[j].name == _result.name)
                        {
                            _data.TechnologyTree[j] = _result;
                        }
                    }

                    Notifications.Instance.gameObject.SetActive(true);
                    Notifications.Instance.TimeVisible += 2.5f;
                    Notifications.Instance.AddText("You found a new word : " + _currentWord.name);
                    Player.Instance.data = _data;
                    Player.Instance.wordKeep = true;
                    Player.Instance.lastTurn = Player.Instance.turn;
                    Player.Instance.DeleteWordKeeper(this.gameObject);
                    Player.Instance.Close();
                    Destroy(gameObject);
                });
            }
            else
            {
                go.GetComponent<Button>().onClick.AddListener(delegate
                {
                    Notifications.Instance.gameObject.SetActive(true);
                    Notifications.Instance.TimeVisible += 2.5f;
                    Notifications.Instance.AddText("You didn't understand!");
                    Player.Instance.wordKeep = true;
                    Player.Instance.lastTurn = Player.Instance.turn;
                    Player.Instance.DeleteWordKeeper(this.gameObject);
                    Player.Instance.Close();
                    Destroy(gameObject);
                });
            }
            buttonOpen.GetComponent<Button>().onClick.AddListener(delegate
            {
                Player.Instance.Open(this.gameObject);
                
            });
            buttonClose.GetComponent<Button>().onClick.AddListener(delegate
            {
                Player.Instance.Close();
                
            });
            go.GetComponentInChildren<TextMeshProUGUI>().text = _data.TechnologyTree[i].name;
            if (Resources.Load<Sprite>(_data.TechnologyTree[i].name))
                go.GetComponentInChildren<Image>().sprite = Resources.Load<Sprite>(_data.TechnologyTree[i].name);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
