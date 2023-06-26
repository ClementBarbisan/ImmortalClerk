using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Square : MonoBehaviour
{
    Square[] _sides = new Square[4];
    public Square[] Sides => _sides;
    private GameObject _content;
    private Image _image;
    private RectTransform _rectTr;
    public GameObject Content
    {
        get => _content;
        set => _content = value;
    }

    private Vector2Int _coords;
    public Vector2Int Coords
    {
        get => _coords;
        set => _coords = value;
    }
    [SerializeField]
    private Vector2 _size = new Vector2(5, 5);

    public Vector2 Size
    {
        get => _size;
        set => _size = value;
    }

    // Start is called before the first frame update
    void Awake()
    {
        _rectTr = GetComponent<RectTransform>();
        _image = GetComponent<Image>();
        _rectTr.sizeDelta = _size;
        _image.color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
