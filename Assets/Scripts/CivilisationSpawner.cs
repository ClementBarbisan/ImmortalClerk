using UnityEngine;

/// <summary>
/// Spawns the civilisations at random positions on the background.
/// </summary>
public class CivilisationSpawner : MonoBehaviour
{
    [SerializeField] private GameObject _background;
    [SerializeField] private Civilisation _civilisation;
    [SerializeField] private int _maxCiv = 3;
    [SerializeField, Range(0f, 0.5f)] private float _margin = 0.05f;

    public void SpawnCivilisations()
    {
        RectTransform rect = _background.GetComponentInParent<Canvas>().GetComponent<RectTransform>();
        for (int i = 0; i < _maxCiv; i++)
        {
            Civilisation civ = Instantiate(_civilisation, transform);
            civ.name = i.ToString();
            civ.OpenButton.transform.position = new Vector3(
                Random.Range(_margin, 1f - _margin) * rect.rect.width * rect.localScale.x,
                Random.Range(_margin, 1f - _margin) * rect.rect.height * rect.localScale.y, 0);
            civ.gameObject.layer = default;
        }
    }
}
