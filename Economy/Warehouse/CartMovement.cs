using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Компонент движения тележки.
/// Следует по переданному списку точек.
/// </summary>
public class CartMovement : MonoBehaviour
{
    [Header("Настройки")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;

    private List<Vector2Int> _currentPath;
    private int _pathIndex = 0;
    private Vector3 _targetWorldPos;
    private bool _isMoving = false;

    private GridSystem _gridSystem;
    
    // Событие прибытия
    public event System.Action OnDestinationReached;

    public void Initialize()
    {
        _gridSystem = Object.FindFirstObjectByType<GridSystem>();
    }

    public void SetPath(List<Vector2Int> path)
    {
        if (path == null || path.Count == 0)
        {
            Stop();
            return;
        }

        _currentPath = path;
        _pathIndex = 0;
        UpdateTargetNode();
        _isMoving = true;
    }

    public void Stop()
    {
        _isMoving = false;
        _currentPath = null;
    }

    public bool IsMoving => _isMoving;

    private void Update()
    {
        if (!_isMoving || _gridSystem == null) return;

        // Движение
        Vector3 currentPos = transform.position;
        float step = moveSpeed * Time.deltaTime; // Здесь можно добавить множитель скорости дороги

        transform.position = Vector3.MoveTowards(currentPos, _targetWorldPos, step);

        // Поворот
        Vector3 dir = (_targetWorldPos - currentPos).normalized;
        if (dir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        // Проверка прибытия в узел
        if (Vector3.Distance(transform.position, _targetWorldPos) < 0.05f)
        {
            _pathIndex++;
            if (_pathIndex >= _currentPath.Count)
            {
                _isMoving = false;
                OnDestinationReached?.Invoke();
            }
            else
            {
                UpdateTargetNode();
            }
        }
    }

    private void UpdateTargetNode()
    {
        if (_currentPath == null || _pathIndex >= _currentPath.Count) return;

        Vector2Int cell = _currentPath[_pathIndex];
        _targetWorldPos = _gridSystem.GetWorldPosition(cell.x, cell.y);
        
        // Центрируем
        float offset = _gridSystem.GetCellSize() / 2f;
        _targetWorldPos.x += offset;
        _targetWorldPos.z += offset;
        _targetWorldPos.y += 0.1f; // Чуть над землей
    }
    
    // Для визуализации пути (LineRenderer)
    public List<Vector3> GetRemainingPathWorld()
    {
        var points = new List<Vector3>();
        if (!_isMoving || _currentPath == null) return points;

        points.Add(transform.position);
        points.Add(_targetWorldPos);

        for (int i = _pathIndex + 1; i < _currentPath.Count; i++)
        {
            var cell = _currentPath[i];
            var pos = _gridSystem.GetWorldPosition(cell.x, cell.y);
            float offset = _gridSystem.GetCellSize() / 2f;
            pos.x += offset; pos.z += offset; pos.y += 0.1f;
            points.Add(pos);
        }
        return points;
    }
}