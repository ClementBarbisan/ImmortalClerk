using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class Civilisation : MonoBehaviour
{
    public JsonParser.Data data;
    private GameObject _resultPanel;
    [SerializeField] private Slider _religion;
    [SerializeField] private Slider _science;
    [SerializeField] private Slider _social;
    [SerializeField] private Slider _conquest;
    private Vector4 _currentValues;
    public Vector4 tmpSubject;
    public Vector4 tmpVerb;
    public Vector4 tmpObject;

    [SerializeField] private ColorBlock normalColors;
    [SerializeField] private ColorBlock aboveColors;
    [SerializeField] private ColorBlock beneathColors;
    // Start is called before the first frame update
    void Awake()
    {
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
            _currentValues = new Vector4(_religion.value, _science.value, _social.value, _conquest.value);
            data = JsonParser.Instance.data;
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
            _resultPanel = GameObject.FindWithTag("Result");
    }

    public void ExecuteResult()
    {
        Word[] words = _resultPanel.GetComponentsInChildren<Word>();
        if (words.Length != 3)
            return;
        List<string> techs = new List<string>();
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].type != Word.wordType.subject)
                techs.Add(words[i].technos.name);
        }
        int count = 0;
        for (int i = 0; i < data.TechnologyTree.Count; i++)
        {
            if (data.TechnologyTree[i].useable)
                continue;
            else if (data.TechnologyTree[i].dependances.Contains(techs[count]))
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
                    JsonParser.Technos tmpStruct = data.TechnologyTree[i];
                    JsonParser.Technos tmpStructPlayer = Player.Instance.data.TechnologyTree[i];
                    tmpStruct.useable = true;
                    tmpStructPlayer.useable = true;
                            
                    _currentValues += Vector4.Scale((tmpObject + tmpVerb), tmpSubject);
                    tmpObject = Vector4.zero;
                    tmpSubject = Vector4.zero;
                    tmpVerb = Vector4.zero;
                    if (tmpStruct.words.subjects.Count > 0)
                        tmpStruct = GetNewWord(tmpStruct.words.subjects, tmpStruct, Word.wordType.subject, ref tmpStructPlayer);
                    else if (tmpStruct.words.verbs.Count > 0)
                        tmpStruct = GetNewWord(tmpStruct.words.verbs, tmpStruct, Word.wordType.verb, ref tmpStructPlayer);
                    else if (tmpStruct.words.objects.Count > 0)
                        tmpStruct = GetNewWord(tmpStruct.words.objects, tmpStruct, Word.wordType.obj, ref tmpStructPlayer);
                    data.TechnologyTree[i] = tmpStruct;
                    Player.Instance.data.TechnologyTree[i] = tmpStructPlayer;
                    break;
                }
                else
                {
                    count = 0;
                }
            }
        }
    }

    private JsonParser.Technos GetNewWord(List<JsonParser.Word> wordsSubjects, JsonParser.Technos techs,
        Word.wordType type, ref JsonParser.Technos playerStruct)
    {
        float distance = 10000;
        int index = 0;
        for (int i = 0; i < wordsSubjects.Count; i++)
        {
            if (Vector4.Distance(new Vector4(wordsSubjects[i].limits[0], wordsSubjects[i].limits[1],
                wordsSubjects[i].limits[2],wordsSubjects[i].limits[3]), _currentValues) < distance)
                index = i;
        }

        if (type == Word.wordType.subject)
        {
            JsonParser.Word tmpWordsPlayer = playerStruct.words.subjects[index];
            tmpWordsPlayer.useable = true;
            playerStruct.words.subjects[index] = tmpWordsPlayer;
            JsonParser.Word tmpWords = techs.words.subjects[index];
            tmpWords.useable = true;
            techs.words.subjects[index] = tmpWords;
        }
        else if(type == Word.wordType.verb)
        {
            JsonParser.Word tmpWordsPlayer = playerStruct.words.verbs[index];
            tmpWordsPlayer.useable = true;
            playerStruct.words.verbs[index] = tmpWordsPlayer;
            JsonParser.Word tmpWords = techs.words.verbs[index];
            tmpWords.useable = true;
            techs.words.verbs[index] = tmpWords;
        }else if(type == Word.wordType.obj)
        {
            JsonParser.Word tmpWordsPlayer = playerStruct.words.objects[index];
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
        if (tmpObject != Vector4.zero || tmpVerb != Vector4.zero || tmpSubject != Vector4.zero)
        {
            _religion.value = _currentValues.x + (tmpObject.x + tmpVerb.x) * tmpSubject.x;
            _social.value = _currentValues.y + (tmpObject.y + tmpVerb.y) * tmpSubject.y;
            _science.value = _currentValues.z + (tmpObject.z + tmpVerb.z) * tmpSubject.z;
            _conquest.value = _currentValues.w + (tmpObject.w + tmpVerb.w) * tmpSubject.w;
            if (_religion.value > _currentValues.x)
                _religion.colors = aboveColors;
            if (_religion.value < _currentValues.x)
                _religion.colors = beneathColors;
            if (_social.value > _currentValues.y)
                _social.colors = aboveColors;
            if (_social.value < _currentValues.y)
                _social.colors = beneathColors;
            if (_science.value > _currentValues.z)
                _science.colors = aboveColors;
            if (_science.value < _currentValues.z)
                _science.colors = beneathColors;
            if (_conquest.value > _currentValues.w)
                _conquest.colors = aboveColors;
            if (_conquest.value < _currentValues.w)
                _conquest.colors = beneathColors;
        }
        else
        {
            _religion.colors = normalColors;
            _social.colors = normalColors;
            _science.colors = normalColors;
            _conquest.colors = normalColors;
        }
    }
}
