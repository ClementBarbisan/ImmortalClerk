using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Objects : MonoBehaviour
{
    private Civilisation _civ;
    [SerializeField]
    private GameObject _prefab;
    [SerializeField]
    private GameObject _content;
    // Start is called before the first frame update
    [SerializeField] private GameObject _panel;
    [SerializeField] private Transform _positionObjects;
    [SerializeField]
    private GameObject _prefabObjects;

    private int currentObject;
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
        _civ = GetComponentInParent<Civilisation>();
        for (int i = 0; i < _civ.data.TechnologyTree.Count; i++)
        {
            if (_civ.data.TechnologyTree[i].useable)
            {
                if (Player.Instance.data.TechnologyTree[i].words.objects.FindAll(x=>x.useable).Count > 0)
                {
                    GameObject go = Instantiate<GameObject>(_prefab, Vector3.zero, Quaternion.identity);
                    go.transform.SetParent(_content.transform);
                    int tmpInt = i;
                    go.GetComponent<Button>().onClick.AddListener(delegate
                    {
                        ClickOnObject(tmpInt); });
                    go.GetComponentInChildren<TextMeshProUGUI>().text = _civ.data.TechnologyTree[i].name;
                    if (Resources.Load<Sprite>(_civ.data.TechnologyTree[i].name))
                        go.GetComponentInChildren<Image>().sprite =
                            Resources.Load<Sprite>(_civ.data.TechnologyTree[i].name);
                }
            }
        }
    }

    void ClickOnObject(int tech)
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
        for (int i = 0; i < _civ.data.TechnologyTree[tech].words.objects.Count; i++)
        {
            if (!_civ.data.TechnologyTree[tech].words.objects[i].useable)
                continue;
            GameObject go = Instantiate(_prefabObjects, Vector3.zero, Quaternion.identity);
            go.GetComponentInChildren<TextMeshProUGUI>().text = _civ.data.TechnologyTree[tech].words.objects[i].name;
            int tmpInt = i;
            go.GetComponentInChildren<Button>().onClick.AddListener(delegate
            {
                ChooseObject(tmpInt, tech); });
            if (Resources.Load<Sprite>(_civ.data.TechnologyTree[tech].words.objects[i].name))
                go.GetComponentInChildren<Image>().sprite = Resources.Load<Sprite>(_civ.data.TechnologyTree[tech].words.objects[i].name);
            go.transform.SetParent(_panel.transform);
        }
    }

    void ChooseObject(int index, int tech)
    {
        if (_positionObjects.GetComponentInChildren<Word>() != null)
        {
            GameObject word = _positionObjects.GetComponentInChildren<Word>().gameObject;
            if (word.GetComponentInChildren<Image>().sprite != null)
                Destroy(word.GetComponentInChildren<Image>().sprite.texture);
            Destroy(word);
        }    
        GameObject go = Instantiate(_prefabObjects, Vector3.zero, Quaternion.identity, _positionObjects);
        go.GetComponentInChildren<Button>().interactable = false;
        go.GetComponentInChildren<TextMeshProUGUI>().text = _civ.data.TechnologyTree[tech].words.objects[index].name;
        Word currentWord = go.GetComponent<Word>();
        currentWord.word = _civ.data.TechnologyTree[tech].words.objects[index];
        currentWord.technos = _civ.data.TechnologyTree[tech];
        currentWord.type = Word.wordType.obj;
        _civ.tmpObject = new Vector4(currentWord.word.value[0], currentWord.word.value[1],
            currentWord.word.value[2],currentWord.word.value[3]);
        if (Resources.Load<Sprite>(_civ.data.TechnologyTree[tech].words.objects[index].name))
            go.GetComponentInChildren<Image>().sprite = Resources.Load<Sprite>(_civ.data.TechnologyTree[tech].words.objects[index].name);
        go.transform.localPosition = Vector3.zero;
        _panel.SetActive(false);
    }
    
    // Update is called once per frame
    void Update()
    {
        
    }
}
