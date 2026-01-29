using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GridLayoutGroup))]
public class ResponsiveGridController : MonoBehaviour
{
    [SerializeField] private int _phoneColumns = 2;
    [SerializeField] private int _tabletColumns = 3;

    [SerializeField, Range(1.3f, 2.2f)]
    private float _tabletAspectThreshold = 1.6f;

    private GridLayoutGroup _grid;
    private RectTransform _rect;

    private void Awake()
    {
        _grid = GetComponent<GridLayoutGroup>();
        _rect = (RectTransform)transform;

        UpdateGrid();
    }

    private void OnRectTransformDimensionsChange()
    {
        UpdateGrid();
    }

    private void UpdateGrid()
    {
        if (_grid == null || _rect == null) return;

        float aspect = (float)Screen.width / Screen.height;
        aspect = Mathf.Max(aspect, 1f / aspect);

        bool isTablet = aspect < _tabletAspectThreshold;

        int columns = isTablet ? _tabletColumns : _phoneColumns;

        _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        _grid.constraintCount = columns;

        float totalSpacing = _grid.spacing.x * (columns - 1);
        float width = _rect.rect.width - _grid.padding.horizontal - totalSpacing;
        float cellSize = width / columns;

        _grid.cellSize = new Vector2(cellSize, cellSize);
    }
}
