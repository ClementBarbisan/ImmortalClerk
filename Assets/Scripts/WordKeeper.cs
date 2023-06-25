using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    [SerializeField]
    private Vector4 _values;

    private void Start()
    {
        _data = Player.Instance.data;
        _values = new Vector4(Random.Range(5, 90), Random.Range(5, 90), Random.Range(5, 90), Random.Range(5, 90));
        List<JsonParser.Technos> technos = _data.TechnologyTree.FindAll(x => x.learned && x.dependances.Count > 0);
        JsonParser.Technos general = _data.TechnologyTree.Find(x => x.name == "general");
        _currentSubject = general.words.subjects[Random.Range(0, general.words.subjects.Count)];
        if (technos.Count <= 0)
        {
            Player.Instance.wordKeep = true;
            Player.Instance.lastTurn = Player.Instance.turn;
            Player.Instance.DeleteWordKeeper(this.gameObject);
            Destroy(gameObject);
            return;
        }

        int indexFirst = Random.Range(0, technos.Count);
        int index = indexFirst;
        do
        {
            JsonParser.Technos tech1 = _data.TechnologyTree.Find(x => x.name == technos[index].dependances[0]);
            JsonParser.Technos tech2 = _data.TechnologyTree.Find(x => x.name == technos[index].dependances[1]);
            if (tech1 != null && tech2 != null)
            {
                if (tech1.words.verbs.Count > 0 && tech2.words.objects.Any(x => x.useable) &&
                    tech1.words.verbs.Any(x => x.useable))
                {
                    _currentObject = tech2.words.objects.FindAll(x => x.useable).OrderBy((x)
                        => Vector4.Distance(x.value, _values)).First();
                    _currentVerb = tech1.words.verbs.FindAll(x => x.useable).OrderBy((x)
                        => Vector4.Distance(x.value, _values)).First();
                }
                else if (tech2.words.verbs.Count > 0 && tech1.words.objects.Any(x => x.useable) &&
                         tech2.words.verbs.Any(x => x.useable))
                {
                    _currentObject = tech1.words.objects.FindAll(x => x.useable).OrderBy((x)
                        => Vector4.Distance(x.value, _values)).First();
                    _currentVerb = tech2.words.verbs.FindAll(x => x.useable).OrderBy((x)
                        => Vector4.Distance(x.value, _values)).First();
                }
                if (technos[index].words.verbs.Count > 0 && technos[index].words.verbs.Any(x => x.useable == false))
                {
                    _currentWord = technos[index].words.verbs.FindAll(x => x.useable == false).OrderBy((x)
                        => Vector4.Distance(x.value, _values)).First();;
                }
                else if (technos[index].words.objects.Count > 0 && technos[index].words.objects.Any(x => x.useable == false))
                {
                    _currentWord = technos[index].words.objects.FindAll(x => x.useable == false).OrderBy((x)
                        => Vector4.Distance(x.value, _values)).First();;
                }
                _result = technos[index];
                if (_currentObject != null && _currentVerb != null && _currentWord != null)
                {
                    _wordsSets = true;
                    break;
                }
                _currentObject = null;
                _currentVerb = null;
                _currentWord = null;
            }
            index = (index + 1) % technos.Count;
        } while (index != indexFirst);
        if (!_wordsSets)
        {
            Player.Instance.wordKeep = true;
            Player.Instance.lastTurn = Player.Instance.turn;
            Player.Instance.DeleteWordKeeper(this.gameObject);
            Destroy(gameObject);
            return;
        }
        GameObject subject = Instantiate(_prefabTechno, Vector3.zero, Quaternion.identity, _posSubject);
        subject.transform.localPosition = Vector3.zero;
        subject.transform.localScale = Vector3.one;

        subject.GetComponentInChildren<TextMeshProUGUI>().text = _currentSubject.name;
        if (Resources.Load<Sprite>(_currentSubject.name))
            subject.GetComponentInChildren<Image>().sprite = Resources.Load<Sprite>(_currentSubject.name);
        GameObject verb = Instantiate(_prefabTechno, Vector3.zero, Quaternion.identity, _posVerb);
        verb.GetComponentInChildren<TextMeshProUGUI>().text = _currentVerb.name;
        verb.transform.localPosition = Vector3.zero;
        verb.transform.localScale = Vector3.one;

        if (Resources.Load<Sprite>(_currentVerb.name))
            verb.GetComponentInChildren<Image>().sprite = Resources.Load<Sprite>(_currentVerb.name);
        GameObject obj = Instantiate(_prefabTechno, Vector3.zero, Quaternion.identity, _posObject);
        obj.GetComponentInChildren<TextMeshProUGUI>().text = _currentObject.name;
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localScale = Vector3.one;

        if (Resources.Load<Sprite>(_currentObject.name))
            obj.GetComponentInChildren<Image>().sprite = Resources.Load<Sprite>(_currentObject.name);
        for (int i = 0; i < _data.TechnologyTree.Count; i++)
        {
            if (!_data.TechnologyTree[i].learned)
                continue;
            GameObject go = Instantiate(_prefabConcept, Vector3.zero, Quaternion.identity, _panelConcept.transform);
            go.transform.localScale = Vector3.one;

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
