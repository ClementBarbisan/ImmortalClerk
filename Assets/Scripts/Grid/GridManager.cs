using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private GameObject _background;
    private Square[][] _grid;

    public Square[][] Grid => _grid;
    [SerializeField]
    private Civilisation _civilisation;
    [SerializeField] 
    private Square _square;
    [SerializeField]
    private Vector2Int _size = new Vector2Int(10, 10);
    [SerializeField] private int _maxCiv = 3;
    private int _nbCiv = 0;
    // Start is called before the first frame update
    public void CreateGrid()
    {
        RectTransform rect = _background.GetComponentInParent<Canvas>().GetComponent<RectTransform>();
        _square.Size = new Vector2((rect.rect.width) / (_size.x), (rect.rect.height) / (_size.y));
        _grid = new Square[_size.x][];
        for (int i = 0; i < _size.x; i++)
        {
            _grid[i] = new Square[_size.y];
            for (int j = 0; j < _size.y; j++)
            {
                GameObject obj = Instantiate(_square.gameObject, _background.transform);
                obj.transform.position = new Vector3((_square.Size.x / 2f + (i) * _square.Size.x) * rect.localScale.x,
                    (_square.Size.y / 2f + j * _square.Size.y) * rect.localScale.y, 0);
                Square cur = obj.GetComponent<Square>();
                _grid[i][j] = cur;
                if (i > 0)
                {
                    cur.Sides[0] = _grid[i - 1][j];
                    _grid[i - 1][j].Sides[2] = cur;
                }

                if (j > 0)
                {
                    cur.Sides[1] = _grid[i][j - 1];
                    _grid[i][j - 1].Sides[3] = cur;
                }
                cur.Coords = new Vector2Int(i, j);
                cur.name = cur.Coords.ToString();
                if (_nbCiv < _maxCiv && Random.Range(0, 70) < 3)
                {
                    Civilisation civ = Instantiate(_civilisation, transform);
                    civ.name = _nbCiv.ToString();
                    civ.OpenButton.transform.position = cur.transform.position;
                    cur.Content = civ.gameObject;
                    civ.gameObject.layer = default;
                    _nbCiv++;
                }
                if (Player.Instance.playerPos == null && Random.Range(0, 75) < 2)
                {
                    Player.Instance.playerPos = cur;
                }
            }
        }
    }
}
