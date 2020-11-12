using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Verbs : MonoBehaviour
{
    private Civilisation _civ;
    [SerializeField]
    private GameObject _prefab;
    [SerializeField]
    private GameObject _content;
    
    [SerializeField] private GameObject _panel;
    [SerializeField] private Transform _positionVerbs;
    [SerializeField]
    private GameObject _prefabVerbs;


    // Start is called before the first frame update
    void OnEnable()
    {
        _civ = GetComponentInParent<Civilisation>();
        for (int i = 0; i < _civ.data.TechnologyTree.Count; i++)
        {
            if (_civ.data.TechnologyTree[i].useable)
            {
                if (Player.Instance.data.TechnologyTree[i].words.verbs.FindAll(x=>x.useable).Count > 0)
                {
                    GameObject go = Instantiate<GameObject>(_prefab, Vector3.zero, Quaternion.identity);
                    go.transform.SetParent(_content.transform);
                    int tmpInt = i;
                    go.GetComponent<Button>().onClick.AddListener(delegate
                    {
                        ClickOnVerbs(tmpInt); });
                    go.GetComponentInChildren<TextMeshProUGUI>().text = _civ.data.TechnologyTree[i].name;
                    if (Resources.Load<Sprite>(_civ.data.TechnologyTree[i].name))
                        go.GetComponentInChildren<Image>().sprite =
                            Resources.Load<Sprite>(_civ.data.TechnologyTree[i].name);
                }
            }
        }
    }

    void ClickOnVerbs(int tech)
    {
        if (_panel.GetComponentsInChildren<Word>().Length > 0)
        {
            Word[] words = _panel.GetComponentsInChildren<Word>();
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].GetComponentInChildren<Image>().sprite != null)
                    Destroy(words[i].GetComponentInChildren<Image>().sprite.texture);
                Destroy(words[i].gameObject);
            }

        }

        _panel.SetActive(true);
        for (int i = 0; i < _civ.data.TechnologyTree[tech].words.verbs.Count; i++)
        {
            GameObject go = Instantiate(_prefabVerbs, Vector3.zero, Quaternion.identity);
            go.GetComponentInChildren<TextMeshProUGUI>().text = _civ.data.TechnologyTree[tech].words.verbs[i].name;
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
            if (word.GetComponentInChildren<Image>().sprite != null)
                Destroy(word.GetComponentInChildren<Image>().sprite.texture);
            Destroy(word);
        }    
        GameObject go = Instantiate(_prefabVerbs, Vector3.zero, Quaternion.identity, _positionVerbs);
        go.GetComponentInChildren<Button>().interactable = false;
        go.GetComponentInChildren<TextMeshProUGUI>().text = _civ.data.TechnologyTree[tech].words.verbs[index].name;
        Debug.Log(_civ.data.TechnologyTree[tech].words.verbs[index].value);
        Word currentWord = go.GetComponent<Word>();
        currentWord.word = _civ.data.TechnologyTree[tech].words.verbs[index];
        currentWord.technos = _civ.data.TechnologyTree[tech];
        currentWord.type = Word.wordType.verb;
        Debug.Log(currentWord.word.value);
        _civ.tmpVerb = new Vector4(currentWord.word.value[0], currentWord.word.value[1],
            currentWord.word.value[2],currentWord.word.value[3]);
        if (Resources.Load<Sprite>(_civ.data.TechnologyTree[tech].words.verbs[index].name))
            go.GetComponentInChildren<Image>().sprite = Resources.Load<Sprite>(_civ.data.TechnologyTree[tech].words.verbs[index].name);
        go.transform.localPosition = Vector3.zero;
        _panel.SetActive(false);
    }

    private void Start()
    {
    }
    
    // Update is called once per frame
    void Update()
    {
        
    }
}
