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

    private JsonParser.Word _currentSubject;
    private JsonParser.Word _currentVerb;
    private JsonParser.Word _currentObject;

    private bool _wordsSets = false;
    private JsonParser.Word _currentWord;

    // Start is called before the first frame update
    public void OnOpen()
    {
        
    }

    public void OnClose()
    {
    }

    private void Start()
    {
        _data = Player.Instance.data;
        var technos = _data.TechnologyTree.FindAll(x => x.useable && x.dependances.Count > 0);
        var general = _data.TechnologyTree.Find(x => x.name == "general");
        _currentSubject = general.words.subjects[Random.Range(0, general.words.subjects.Count)];
        for (int i = 0; i < technos.Count; i++)
        {
            var tech1 = _data.TechnologyTree.Find(x => x.name == technos[i].dependances[0]);
            var tech2 = _data.TechnologyTree.Find(x => x.name == technos[i].dependances[1]);
            Debug.Log(tech1.name);
            Debug.Log(tech2.name);
            if (tech1.words.verbs.Count > 0)
            {
                var verbsNotUse = tech1.words.verbs.FindAll(x => x.useable == false);
                var objectsNotUse = tech2.words.objects.FindAll(x => x.useable == false);
                if (verbsNotUse.Count > 0)
                {
                    _currentObject = tech2.words.objects.Find(x=> x.useable);
                    _currentVerb = verbsNotUse[Random.Range(0, verbsNotUse.Count)];
                    _currentWord = _currentVerb;
                    _wordsSets = true;
                    _result = technos[i];
                    break;
                }
                else if (objectsNotUse.Count > 0)
                {
                    _currentVerb = tech1.words.verbs.Find(x=> x.useable);
                    _currentObject = objectsNotUse[Random.Range(0, objectsNotUse.Count)];
                    _currentWord = _currentObject;
                    _wordsSets = true;
                    _result = technos[i];
                    break;
                }
            }
            else if (tech2.words.verbs.Count > 0)
            {
                var verbsNotUse = tech2.words.verbs.FindAll(x => x.useable == false);
                var objectsNotUse = tech1.words.objects.FindAll(x => x.useable == false);
                if (verbsNotUse.Count > 0)
                {
                    _currentObject = tech1.words.objects.Find(x=> x.useable);
                    _currentVerb = verbsNotUse[Random.Range(0, verbsNotUse.Count)];
                    _currentWord = _currentVerb;
                    _wordsSets = true;
                    _result = technos[i];
                    break;
                }
                else if (objectsNotUse.Count > 0)
                {
                    _currentVerb = tech2.words.verbs.Find(x=> x.useable);
                    _currentObject = objectsNotUse[Random.Range(0, objectsNotUse.Count)];
                    _currentWord = _currentObject;
                    _wordsSets = true;
                    _result = technos[i];
                    break;
                }
            }
        }
        if (!_wordsSets)
            Destroy(gameObject);
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
        for (int i = 0; i < technos.Count; i++)
        {
            GameObject go = Instantiate(_prefabConcept, Vector3.zero, Quaternion.identity, _panelConcept.transform);
            if (technos[i].name == _result.name)
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
                    Notifications.Instance.AddText("You found a new word!");
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
                    Destroy(gameObject);
                });
            }

            go.GetComponentInChildren<TextMeshProUGUI>().text = technos[i].name;
            if (Resources.Load<Sprite>(technos[i].name))
                go.GetComponentInChildren<Image>().sprite = Resources.Load<Sprite>(technos[i].name);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
