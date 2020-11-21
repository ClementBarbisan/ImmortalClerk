﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class Civilisation : MonoBehaviour
{
    public JsonParser.Data data;
    [SerializeField ]private GameObject _resultPanel;
    [SerializeField] private Slider _religion;
    [SerializeField] private Slider _science;
    [SerializeField] private Slider _social;
    [SerializeField] private Slider _conquest;
    [SerializeField] private Slider _trustIndice;
    private Vector4 _currentValues;
    public Vector4 tmpSubject;
    public Vector4 tmpVerb;
    public Vector4 tmpObject;
    private GameObject _notifPanel;
    [FormerlySerializedAs("normalColors")] [SerializeField] private ColorBlock _normalColors;
    [FormerlySerializedAs("aboveColors")] [SerializeField] private ColorBlock _aboveColors;
    [FormerlySerializedAs("beneathColors")] [SerializeField] private ColorBlock _beneathColors;
    [FormerlySerializedAs("_exit")] [SerializeField] private GameObject _open;
    [SerializeField] private TextMeshProUGUI _autoTechText;
    [SerializeField]private GameObject _panelAbstract;
    [SerializeField] private GameObject _openButton;
    private Word[] _words;
    private bool _hitting = false;
    private int _indexTech = 0;
    [SerializeField] private GameObject _content;
    [SerializeField]private GameObject _prefab;
    private Subjects _subjects;
    private Verbs _verbs;
    private Objects _objects;

    // Start is called before the first frame update

    public void CloseCivilisation()
    {
        Player.Instance.Close();
    }

    public void OpenCivilisation()
    {
        Player.Instance.Open(this.gameObject);
        if (_trustIndice.value < 10f)
        {
            Notifications.Instance.gameObject.SetActive(true);
            Notifications.Instance.TimeVisible += 2.5f;
            Notifications.Instance.AddText(gameObject.name +  " don't want to talk with you anymore!");
        }
        else
        {
            _open.SetActive(true);
        }
    }

    void Awake()
    {
        PlayerPrefs.DeleteAll();
        // if (PlayerPrefs.HasKey("religion" + gameObject.name))
        //     _religion.value = PlayerPrefs.GetFloat("religion" + gameObject.name);
        // if (PlayerPrefs.HasKey("science" + gameObject.name))
        //     _science.value = PlayerPrefs.GetFloat("science" + gameObject.name);
        // if (PlayerPrefs.HasKey("social" + gameObject.name))
        //     _social.value = PlayerPrefs.GetFloat("social" + gameObject.name);
        // if (PlayerPrefs.HasKey("conquest" + gameObject.name))
        //     _conquest.value = PlayerPrefs.GetFloat("conquest" + gameObject.name);
        // if (File.Exists(Application.persistentDataPath + "/" + gameObject.name + ".save"))
        // {
        //     _data = JsonUtility.FromJson<JsonParser.Data>(File.ReadAllText(Application.persistentDataPath + "/" + gameObject.name + ".save"));
        // }
        // else
        // {
            _religion.value = Random.Range(10f, 50f);
            _social.value = Random.Range(10f, 50f);
            _science.value = Random.Range(10f, 50f);
            _conquest.value = Random.Range(10f, 50f);
            _currentValues = new Vector4(_religion.value, _social.value, _science.value, _conquest.value);
            _trustIndice.value = 100f - (Mathf.Max(Mathf.Max(_currentValues.x, _currentValues.y),
                                     Mathf.Max(_currentValues.z, _currentValues.w)) -
                                 Mathf.Min(Mathf.Min(_currentValues.x, _currentValues.y), Mathf.Min(_currentValues.z, _currentValues.w)));
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
                        if (tmpStructDescends.limits[0] <= _religion.value && tmpStructDescends.limits[1] <= _social.value &&
                            tmpStructDescends.limits[2] <= _science.value && tmpStructDescends.limits[3] <= _conquest.value)
                        {
                            tmpStructDescends.useable = true;
                            tmpStruct.words.objects[j] = tmpStructDescends;
                        }
                    }
                    for (int j = 0; j < tmpStruct.words.subjects.Count; j++)
                    {
                        var tmpStructDescends = tmpStruct.words.subjects[j];
                        if (tmpStructDescends.limits[0] <= _religion.value && tmpStructDescends.limits[1] <= _social.value &&
                            tmpStructDescends.limits[2] <= _science.value && tmpStructDescends.limits[3] <= _conquest.value)
                        {
                            tmpStructDescends.useable = true;
                            tmpStruct.words.subjects[j] = tmpStructDescends;
                        }
                    }
                    for (int j = 0; j < tmpStruct.words.verbs.Count; j++)
                    {
                        var tmpStructDescends = tmpStruct.words.verbs[j];
                        if (tmpStructDescends.limits[0] <= _religion.value && tmpStructDescends.limits[1] <= _social.value &&
                            tmpStructDescends.limits[2] <= _science.value && tmpStructDescends.limits[3] <= _conquest.value)
                        {
                            tmpStructDescends.useable = true;
                            tmpStruct.words.verbs[j] = tmpStructDescends;
                        }
                    }
                    data.TechnologyTree[i] = tmpStruct;
                }

                
            }
        
            // }
            _subjects = GetComponentInChildren<Subjects>();
            _objects = GetComponentInChildren<Objects>();
            _verbs = GetComponentInChildren<Verbs>();
            _notifPanel = GameObject.FindWithTag("Notifications");
            _panelAbstract.SetActive(false);
            _open.SetActive(false);
            
    }

    public void AddTurn()
    {
        for (int i = 0; i < data.TechnologyTree.Count; i++)
        {
            if (data.TechnologyTree[i].useable)
            {
                JsonParser.Technos tech = data.TechnologyTree[i];
                tech.time -= 1;
                if (data.TechnologyTree[i].time <= 0)
                {
                    tech.useable = true;
                    Notifications.Instance.gameObject.SetActive(true);
                    Notifications.Instance.TimeVisible += 2.5f;
                    Notifications.Instance.AddText("New Technology " + gameObject.name + " : " + tech.name);
                }

                data.TechnologyTree[i] = tech;
            }
        }
    }

    public void ExecuteResult()
    {
        _words = _resultPanel.GetComponentsInChildren<Word>();
        if (_words.Length != 3)
            return;
        List<string> techs = new List<string>();
        for (int i = 0; i < _words.Length; i++)
        {
            if (_words[i].type != Word.wordType.subject)
                techs.Add(_words[i].technos.name);
        }
        int count = 0;
        for (int i = 0; i < data.TechnologyTree.Count; i++)
        {
            if (data.TechnologyTree[i].dependances.Contains(techs[count]))
            {
                count++;
                while (count < techs.Count)
                {
                    if (data.TechnologyTree[i].dependances.Contains(techs[count]))
                        count++;
                    else
                    {
                        break;
                    }
                }

                if (count == techs.Count)
                {
                    // Player.Instance.AddTurn();
                    JsonParser.Technos tmpStruct = data.TechnologyTree[i];
                    if (tmpStruct.useable)
                    {
                        Notifications.Instance.gameObject.SetActive(true);
                        Notifications.Instance.TimeVisible += 2.5f;
                        Notifications.Instance.AddText(tmpStruct.name + " already known!");
                        tmpObject = Vector4.zero;
                        tmpSubject = Vector4.zero;
                        tmpVerb = Vector4.zero;
                        break;
                    }

                    JsonParser.Technos tmpStructPlayer = Player.Instance.data.TechnologyTree[i];
                    if (!tmpStruct.useable)
                    {
                        Notifications.Instance.gameObject.SetActive(true);
                        Notifications.Instance.TimeVisible += 2.5f;
                        Notifications.Instance.AddText("New Technology " + gameObject.name + " : " + tmpStruct.name);
                    }

                    if (!tmpStructPlayer.useable)
                    {
                        Notifications.Instance.gameObject.SetActive(true);
                        Notifications.Instance.TimeVisible += 2.5f;
                        Notifications.Instance.AddText("New Technology Player : " + tmpStructPlayer.name);
                    }

                    tmpStruct.useable = true;
                    if (tmpStruct.type == "religion")
                        tmpStruct.time *= 1 - (_religion.value / 100f);
                    else if (tmpStruct.type == "social")
                        tmpStruct.time *= 1 - (_social.value / 100f);
                    else if (tmpStruct.type == "science")
                        tmpStruct.time *= 1 - (_science.value / 100f);
                    else if (tmpStruct.type == "conquest")
                        tmpStruct.time *= 1 - (_conquest.value / 100f);

                    tmpStructPlayer.useable = true;

                    _currentValues += Vector4.Scale((tmpObject + tmpVerb), tmpSubject * (_trustIndice.value / 100f));
                    _religion.value = _currentValues.x;
                    _social.value = _currentValues.y;
                    _science.value = _currentValues.z;
                    _conquest.value = _currentValues.w;

                    _trustIndice.value = 100f - (Mathf.Max(Mathf.Max(_currentValues.x, _currentValues.y),
                                                     Mathf.Max(_currentValues.z, _currentValues.w)) -
                                                 Mathf.Min(Mathf.Min(_currentValues.x, _currentValues.y),
                                                     Mathf.Min(_currentValues.z, _currentValues.w)));
                    tmpObject = Vector4.zero;
                    tmpSubject = Vector4.zero;
                    tmpVerb = Vector4.zero;
                    if (_trustIndice.value > 25)
                    {
                        if (tmpStruct.words.subjects.Count > 0)
                            tmpStruct = GetNewWord(tmpStruct.words.subjects, tmpStruct, Word.wordType.subject,
                                ref tmpStructPlayer);
                        else if (tmpStruct.words.verbs.Count > 0)
                            tmpStruct = GetNewWord(tmpStruct.words.verbs, tmpStruct, Word.wordType.verb,
                                ref tmpStructPlayer);
                        else if (tmpStruct.words.objects.Count > 0)
                            tmpStruct = GetNewWord(tmpStruct.words.objects, tmpStruct, Word.wordType.obj,
                                ref tmpStructPlayer);
                    }
                    data.TechnologyTree[i] = tmpStruct;
                    Player.Instance.data.TechnologyTree[i] = tmpStructPlayer;
                    if (_religion.value >= 100f)
                        Player.Instance.Done("religion");
                    if (_social.value >= 100f)
                        Player.Instance.Done("social");
                    if (_science.value >= 100f)
                        Player.Instance.Done("science");
                    if (_conquest.value >= 100f)
                        Player.Instance.Done("conquest");
                        break;
                }
                else
                {
                    count = 0;
                }
            }
        }
        _subjects.OnDisable();
        _subjects.OnEnable();
        _objects.OnDisable();
        _objects.OnEnable();
        _verbs.OnDisable();
        _verbs.OnEnable();
        
        for (int i = 0; i < _words.Length; i++)
        {
            if (_words[i].GetComponentInChildren<Image>().sprite != null)
                Destroy(_words[i].GetComponentInChildren<Image>().sprite.texture);
            Destroy(_words[i].gameObject);
        }
    }

    private JsonParser.Technos GetNewWord(List<JsonParser.Word> words, JsonParser.Technos techs,
        Word.wordType type, ref JsonParser.Technos playerStruct)
    {
        float distance = 10000;
        int index = 0;
        for (int i = 0; i < words.Count; i++)
        {
            if (Vector4.Distance(new Vector4(words[i].limits[0], words[i].limits[1],
                words[i].limits[2],words[i].limits[3]), _currentValues) < distance &&
                (type == Word.wordType.subject && !playerStruct.words.subjects[index].useable) ||
                (type == Word.wordType.verb && !playerStruct.words.verbs[index].useable) ||
                (type == Word.wordType.obj && !playerStruct.words.objects[index].useable))
                index = i;
        }
        if (type == Word.wordType.subject)
        {
            JsonParser.Word tmpWordsPlayer = playerStruct.words.subjects[index];
            if (!tmpWordsPlayer.useable)
            {
                Notifications.Instance.gameObject.SetActive(true);
                Notifications.Instance.TimeVisible += 2.5f;
                Notifications.Instance.AddText("New Word : " + tmpWordsPlayer.name);
            }

            tmpWordsPlayer.useable = true;
            playerStruct.words.subjects[index] = tmpWordsPlayer;
            JsonParser.Word tmpWords = techs.words.subjects[index];
            tmpWords.useable = true;
            techs.words.subjects[index] = tmpWords;
        }
        else if(type == Word.wordType.verb)
        {
            JsonParser.Word tmpWordsPlayer = playerStruct.words.verbs[index];
            if (!tmpWordsPlayer.useable)
            {
                Notifications.Instance.TimeVisible += 2.5f;
                Notifications.Instance.AddText("New Word : " + tmpWordsPlayer.name);
            }
            tmpWordsPlayer.useable = true;
            playerStruct.words.verbs[index] = tmpWordsPlayer;
            JsonParser.Word tmpWords = techs.words.verbs[index];
            tmpWords.useable = true;
            techs.words.verbs[index] = tmpWords;
        }
        else if(type == Word.wordType.obj)
        {
            JsonParser.Word tmpWordsPlayer = playerStruct.words.objects[index];
            if (!tmpWordsPlayer.useable)
            {
                Notifications.Instance.TimeVisible += 2.5f;
                Notifications.Instance.AddText("New Word : " + tmpWordsPlayer.name);
            }
            tmpWordsPlayer.useable = true;
            playerStruct.words.objects[index] = tmpWordsPlayer;
            JsonParser.Word tmpWords = techs.words.objects[index];
            tmpWords.useable = true;
            techs.words.objects[index] = tmpWords;
        }
        return (techs);
    }

    private void OnApplicationQuit()
    {
        File.WriteAllText(Application.persistentDataPath + "/" + gameObject.name + ".save", JsonUtility.ToJson(data));
        PlayerPrefs.SetFloat("religion" + gameObject.name, _religion.value);
        PlayerPrefs.SetFloat("science" + gameObject.name, _science.value);
        PlayerPrefs.SetFloat("social" + gameObject.name, _social.value);
        PlayerPrefs.SetFloat("conquest" + gameObject.name, _conquest.value);
        PlayerPrefs.Save();
    }

    // Update is called once per frame
    void Update()
    {
        RaycastHit2D hit = Physics2D.Raycast(Input.mousePosition, Vector2.zero);
        // if (hit && hit.collider.CompareTag("Civilisation") && (hit.collider.gameObject == _openButton || hit.collider.gameObject == _panelAbstract))
        // {
        //     Debug.Log(hit.collider.tag);
        //     if (!_hitting)
        //     {
        //         for (int i = 0; i < data.TechnologyTree.Count; i++)
        //         {
        //             if (data.TechnologyTree[i].useable)
        //             {
        //                 for (int j = 0; j < data.TechnologyTree[i].autoTechs.Count; j++)
        //                 {
        //                     if (data.TechnologyTree[i].autoTechs[j].useable)
        //                     {
        //                         GameObject go = Instantiate<GameObject>(_prefab, Vector3.zero, Quaternion.identity, _content.transform);
        //                         go.GetComponentInChildren<TextMeshProUGUI>().text =
        //                             data.TechnologyTree[i].autoTechs[j].name;
        //                         if (Resources.Load<Sprite>(data.TechnologyTree[i].autoTechs[j].name))
        //                             go.GetComponentInChildren<Image>().sprite =
        //                                 Resources.Load<Sprite>(data.TechnologyTree[i].autoTechs[j].name);
        //                     }
        //                     _hitting = true;
        //                     _panelAbstract.SetActive(true);
        //                 }
        //             }
        //         }
        //         
        //     }
        // }
        // else
        // {
        //     foreach (Transform child in _content.transform)
        //         Destroy(child.gameObject);
        //     _panelAbstract.SetActive(false);
        //     _hitting = false;
        // }

        if (tmpObject != Vector4.zero || tmpVerb != Vector4.zero || tmpSubject != Vector4.zero)
        {
            _religion.value = _currentValues.x + (tmpObject.x + tmpVerb.x) * (tmpSubject.x * (_trustIndice.value / 100f));
            _social.value = _currentValues.y + (tmpObject.y + tmpVerb.y) * (tmpSubject.y * (_trustIndice.value / 100f));
            _science.value = _currentValues.z + (tmpObject.z + tmpVerb.z) * (tmpSubject.z * (_trustIndice.value / 100f));
            _conquest.value = _currentValues.w + (tmpObject.w + tmpVerb.w) * (tmpSubject.w * (_trustIndice.value / 100f));
            float tmpIndice = 100f - (Mathf.Max(Mathf.Max(_religion.value, _social.value),
                                          Mathf.Max(_science.value, _conquest.value)) -
                                      Mathf.Min(Mathf.Min(_religion.value, _social.value), Mathf.Min(_science.value, _conquest.value)));
            if (tmpIndice > _trustIndice.value)
                _trustIndice.colors = _aboveColors;
            else if (tmpIndice < _trustIndice.value)
                _trustIndice.colors = _beneathColors;
            if (_religion.value > _currentValues.x)
                _religion.colors = _aboveColors;
            if (_religion.value < _currentValues.x)
                _religion.colors = _beneathColors;
            if (_social.value > _currentValues.y)
                _social.colors = _aboveColors;
            if (_social.value < _currentValues.y)
                _social.colors = _beneathColors;
            if (_science.value > _currentValues.z)
                _science.colors = _aboveColors;
            if (_science.value < _currentValues.z)
                _science.colors = _beneathColors;
            if (_conquest.value > _currentValues.w)
                _conquest.colors = _aboveColors;
            if (_conquest.value < _currentValues.w)
                _conquest.colors = _beneathColors;
        }
        else
        {
            _religion.value = _currentValues.x;
            _social.value = _currentValues.y;
            _science.value = _currentValues.z;
            _conquest.value = _currentValues.w;
            _religion.colors = _normalColors;
            _social.colors = _normalColors;
            _science.colors = _normalColors;
            _conquest.colors = _normalColors;
            _trustIndice.colors = _normalColors;
        }
        _words = _resultPanel.GetComponentsInChildren<Word>();
        if (_words.Length != 3)
        {
            _autoTechText.text = "";
            return;
        }

        // List<string> techs = new List<string>();
        // for (int i = 0; i < _words.Length; i++)
        // {
        //     if (_words[i].type != Word.wordType.subject)
        //         techs.Add(_words[i].technos.name);
        // }
        // for (int i = 0; i < data.TechnologyTree.Count; i++)
        // {
        //     int count = 0;
        //     if (data.TechnologyTree[i].dependances.Contains(techs[count]))
        //     {
        //         count++;
        //         while (count < techs.Count)
        //         {
        //             if (data.TechnologyTree[i].dependances.Contains(techs[count]))
        //                 count++;
        //             else
        //             {
        //                 break;
        //             }
        //         }
        //
        //         if (count == techs.Count)
        //         {
        //             _indexTech = 0;
        //             float distance = 10000;
        //             Vector4 values = new Vector4(_religion.value, _social.value, _science.value, _conquest.value);
        //             for (int j = 0; j < data.TechnologyTree[i].autoTechs.Count; j++)
        //             {
        //                 if (!data.TechnologyTree[i].autoTechs[j].useable)
        //                 {
        //                     Vector4 tmpVector = new Vector4(data.TechnologyTree[i].autoTechs[j].limits[0],
        //                         data.TechnologyTree[i].autoTechs[j].limits[1],
        //                         data.TechnologyTree[i].autoTechs[j].limits[2],
        //                         data.TechnologyTree[i].autoTechs[j].limits[3]);
        //                     if (Vector4.Distance(tmpVector, values) < distance)
        //                     {
        //                         distance = Vector4.Distance(tmpVector, values);
        //                         _indexTech = j;
        //                     }
        //                 }
        //             }
        //
        //             if (data.TechnologyTree[i].autoTechs.Count == 0 ||
        //                 data.TechnologyTree[i].autoTechs[_indexTech].useable)
        //                 return;
        //             int turns = 0;
        //             if (data.TechnologyTree[i].autoTechs[_indexTech].type == "religion")
        //                 turns = Mathf.FloorToInt(data.TechnologyTree[i].autoTechs[_indexTech].time * (1 - _religion.value / 100f));
        //             else if (data.TechnologyTree[i].autoTechs[_indexTech].type == "social")
        //                 turns = Mathf.FloorToInt(data.TechnologyTree[i].autoTechs[_indexTech].time * (1 - _social.value / 100f));
        //             else if (data.TechnologyTree[i].autoTechs[_indexTech].type == "science")
        //                 turns = Mathf.FloorToInt(data.TechnologyTree[i].autoTechs[_indexTech].time * (1 - _science.value / 100f));
        //             else if (data.TechnologyTree[i].autoTechs[_indexTech].type == "conquest")
        //                 turns = Mathf.FloorToInt(data.TechnologyTree[i].autoTechs[_indexTech].time * (1 - _conquest.value / 100f));
        //             if (distance < 10000)
        //                 _autoTechText.text = "New Tech : " + data.TechnologyTree[i].autoTechs[_indexTech].name + Environment.NewLine + "turns : " + turns;
        //             else
        //             {
        //                 _autoTechText.text = "";
        //             }
        //         }
        //     }
        // }
    }
}
