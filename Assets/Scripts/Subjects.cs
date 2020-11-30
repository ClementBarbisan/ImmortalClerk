using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Subjects : MonoBehaviour
{
    [SerializeField]
    private Civilisation _civ;
    [SerializeField]
    private GameObject _prefab;
    [SerializeField]
    private GameObject _content;

    [SerializeField] private GameObject _panel;
    [SerializeField] private Transform _positionSubject;
    [SerializeField]
    private GameObject _prefabSubjects;

    public void OnDisable()
    {
        foreach(Transform child in _content.transform)
        {
            Destroy(child.gameObject);
        }

    }
    // Start is called before the first frame update
    public void OnEnable()
    {
        OnDisable();
        for (int i = 0; i < _civ.data.TechnologyTree.Count; i++)
        {
            if (_civ.data.TechnologyTree[i].useable && _civ.data.TechnologyTree[i].learned)
            {
                if (Player.Instance.data.TechnologyTree[i].words.subjects.FindAll(x=>x.useable).Count > 0)
                {
                    GameObject go = Instantiate<GameObject>(_prefab, Vector3.zero, Quaternion.identity);
                    go.transform.SetParent(_content.transform);
                    int tmpInt = i;
                    go.GetComponent<Button>().onClick.AddListener(delegate
                    {
                        ClickOnSubjects(tmpInt); });
                    if (Player.Instance.debug)
                        go.GetComponentInChildren<TextMeshProUGUI>().text = _civ.data.TechnologyTree[i].name;
                    else
                        go.GetComponentInChildren<TextMeshProUGUI>().text = "";
                    if (Resources.Load<Sprite>(_civ.data.TechnologyTree[i].name))
                        go.GetComponentInChildren<Image>().sprite =
                            Resources.Load<Sprite>(_civ.data.TechnologyTree[i].name);
                }
            }
        }
    }

    void ClickOnSubjects(int tech)
    {
        Word[] words = _panel.GetComponentsInChildren<Word>();
        for (int i = 0; i < words.Length; i++)
        {
            Destroy(words[i].gameObject);
        }

        _panel.SetActive(true);
        for (int i = 0; i < _civ.data.TechnologyTree[tech].words.subjects.Count; i++)
        {
            if (!Player.Instance.data.TechnologyTree[tech].words.subjects[i].useable)
                continue;
            GameObject go = Instantiate(_prefabSubjects, Vector3.zero, Quaternion.identity);
            if (Player.Instance.debug)
                go.GetComponentInChildren<TextMeshProUGUI>().text = _civ.data.TechnologyTree[tech].words.subjects[i].name;
            else
                go.GetComponentInChildren<TextMeshProUGUI>().text = "";
            int tmpInt = i;
            go.GetComponentInChildren<Button>().onClick.AddListener(delegate
            {
                ChooseSubject(tmpInt, tech); });
            if (Resources.Load<Sprite>(_civ.data.TechnologyTree[tech].words.subjects[i].name))
                go.GetComponentInChildren<Image>().sprite = Resources.Load<Sprite>(_civ.data.TechnologyTree[tech].words.subjects[i].name);
            go.transform.SetParent(_panel.transform);
        }
    }

    void ChooseSubject(int index, int tech)
    {
        if (_positionSubject.GetComponentInChildren<Word>() != null)
        {
            GameObject word = _positionSubject.GetComponentInChildren<Word>().gameObject;
            Destroy(word);
        }    
        GameObject go = Instantiate(_prefabSubjects, Vector3.zero, Quaternion.identity, _positionSubject);
        go.GetComponentInChildren<Button>().interactable = false;
        go.GetComponentInChildren<TextMeshProUGUI>().text = _civ.data.TechnologyTree[tech].words.subjects[index].name;
        Word currentWord = go.GetComponent<Word>();
        currentWord.word = _civ.data.TechnologyTree[tech].words.subjects[index];
        currentWord.technos = _civ.data.TechnologyTree[tech];
        currentWord.type = Word.wordType.subject;
        _civ.tmpSubject = new Vector4(currentWord.word.value[0], currentWord.word.value[1],
                                                                currentWord.word.value[2],currentWord.word.value[3]);
        if (Resources.Load<Sprite>(_civ.data.TechnologyTree[tech].words.subjects[index].name))
            go.GetComponentInChildren<Image>().sprite = Resources.Load<Sprite>(_civ.data.TechnologyTree[tech].words.subjects[index].name);
        go.transform.localPosition = Vector3.zero;
        _panel.SetActive(false);
    }


}
