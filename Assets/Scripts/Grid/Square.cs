using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class Square : MonoBehaviour, IPointerEnterHandler
{
    private float _weight = int.MaxValue;
    public float Weight
    {
        get => _weight;
        set => _weight = value;
    }

    private int _pathWeight = int.MaxValue;
    public int PathWeight
    {
        get => _pathWeight;
        set => _pathWeight = value;
    }

    private Square _lastSquare = null;
    public Square LastSquare
    {
        get => _lastSquare;
        set => _lastSquare = value;
    }

    [SerializeField]
    Square[] _sides = new Square[4];
    public Square[] Sides
    {
        get => _sides;
        set => _sides = value;
    }

    private GameObject _content;
    [FormerlySerializedAs("_image")] public Image image;
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

    public Color initColor;
    private bool _check;
    public bool Check
    {
        get => _check;
        set => _check = value;
    }

    public Vector2 Size
    {
        get => _size;
        set => _size = value;
    }

    public int Distance(Square other)
    {
        // return (Mathf.CeilToInt(Vector3.Distance(other.transform.position, transform.position)));
        return (Mathf.Abs(Coords.x - other.Coords.x) + Mathf.Abs(Coords.y - other.Coords.y));
    }

    // Start is called before the first frame update
    void Awake()
    {
        _rectTr = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        _rectTr.sizeDelta = _size;
        image.color = new Color(Random.Range(0f, 0.7f), Random.Range(0f, 0.7f), Random.Range(0f, 0.7f));
        initColor = image.color;
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Player.Instance.currentPath != null && Player.Instance.currentPath.Count > 0)
        {
            foreach (Square square in Player.Instance.currentPath)
            {
                square.image.color = square.initColor;
                square.Reset();
            }
            Player.Instance.currentPath.Clear();
            Player.Instance.currentPath = null;
        }
        Player.Instance.currentPath = AStar.Search(Player.Instance.playerPos, this);
    }

    public int CompareTo(Square other)
    {
        if (Weight < other.Weight) return -1;
        else if (Weight > other.Weight) return 1;
        else return 0;
    }

    private void Reset()
    {
        _weight = Int32.MaxValue;
        _pathWeight = Int32.MaxValue;
        _check = false;
        _lastSquare = null;
    }
}
