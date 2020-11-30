using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Verbs : MonoBehaviour
{
    [SerializeField]
    private Civilisation _civ;
    [SerializeField]
    private GameObject _prefab;
    [SerializeField]
    private GameObject _content;
    
    [SerializeField] private GameObject _panel;
    [SerializeField] private Transform _positionVerbs;
    [SerializeField]
    private GameObject _prefabVerbs;

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
                if (Player.Instance.data.TechnologyTree[i].words.verbs.FindAll(x=>x.useable).Count > 0)
                {
                    GameObject go = Instantiate<GameObject>(_prefab, Vector3.zero, Quaternion.identity, _content.transform);
                    go.transform.localScale = Vector3.one;

                    int tmpInt = i;
                    go.GetComponent<Button>().onClick.AddListener(delegate
                    {
                        ClickOnVerbs(tmpInt); });
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

    void ClickOnVerbs(int tech)
    {
        Word[] words = _panel.GetComponentsInChildren<Word>();
        for (int i = 0; i < words.Length; i++)
        {
            Destroy(words[i].gameObject);
        }


        _panel.SetActive(true);
        for (int i = 0; i < _civ.data.TechnologyTree[tech].words.verbs.Count; i++)
        {
            if (!Player.Instance.data.TechnologyTree[tech].words.verbs[i].useable)
                continue;
            GameObject go = Instantiate(_prefabVerbs, Vector3.zero, Quaternion.identity, _panel.transform);
            go.transform.localScale = Vector3.one;

            if (Player.Instance.debug)
                go.GetComponentInChildren<TextMeshProUGUI>().text = _civ.data.TechnologyTree[tech].words.verbs[i].name;
            else
                go.GetComponentInChildren<TextMeshProUGUI>().text = "";
            int tmpInt = i;
            go.GetComponentInChildren<Button>().onClick.AddListener(delegate
            {
                ChooseVerb(tmpInt, tech); });
            if (Resources.Load<Sprite>(_civ.data.TechnologyTree[tech].words.verbs[i].name))
                go.GetComponentInChildren<Image>().sprite = Resources.Load<Sprite>(_civ.data.TechnologyTree[tech].words.verbs[i].name);
            go.transform.SetParent(_panel.transform);
        }
    }

    void ChooseVerb(int index, int tech)
    {
        if (_positionVerbs.GetComponentInChildren<Word>() != null)
        {
            GameObject word = _positionVerbs.GetComponentInChildren<Word>().gameObject;
            Destroy(word);
        }    
        GameObject go = Instantiate(_prefabVerbs, Vector3.zero, Quaternion.identity, _positionVerbs);
        go.transform.localScale = Vector3.one;

        go.GetComponentInChildren<Button>().interactable = false;
        go.GetComponentInChildren<TextMeshProUGUI>().text = _civ.data.TechnologyTree[tech].words.verbs[index].name;
        Word currentWord = go.GetComponent<Word>();
        currentWord.word = _civ.data.TechnologyTree[tech].words.verbs[index];
        currentWord.technos = _civ.data.TechnologyTree[tech];
        currentWord.type = Word.wordType.verb;
        _civ.tmpVerb = new Vector4(currentWord.word.value[0], currentWord.word.value[1],
            currentWord.word.value[2],currentWord.word.value[3]);
        if (Resources.Load<Sprite>(_civ.data.TechnologyTree[tech].words.verbs[index].name))
            go.GetComponentInChildren<Image>().sprite = Resources.Load<Sprite>(_civ.data.TechnologyTree[tech].words.verbs[index].name);
        go.transform.localPosition = Vector3.zero;
        _panel.SetActive(false);
    }

}
