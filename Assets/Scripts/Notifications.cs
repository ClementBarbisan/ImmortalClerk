using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using TMPro;
using UnityEngine;

public class Notifications : MonoBehaviour
{
    public static Notifications Instance;
    public float TimeVisible = 0;
    [SerializeField] private GameObject _text;
    private List<GameObject> _texts;
    
    public void AddText(string currentText)
    {
        GameObject go = Instantiate(_text, Vector3.zero, Quaternion.identity, gameObject.transform);
        go.GetComponent<TextMeshProUGUI>().text = currentText; 
        _texts.Add(go);
    }

    private void Awake()
    {
        Instance = this;
        _texts = new List<GameObject>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (TimeVisible <= 0)
        {
            for (int i = 0; i < _texts.Count; i++)
                Destroy(_texts[i]);
            _texts.Clear();
            gameObject.SetActive(false);
        }

        if (TimeVisible > 0)
            TimeVisible -= Time.deltaTime;
    }
}
