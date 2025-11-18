using System.Collections.Generic;
using UnityEngine;

public class RoadCoverageVisualizer : MonoBehaviour
{
    // Зависимости
    private CoverageCalculator _calculator;
    private IRoadManager _roadManager;
    
    // Состояние
    private List<AuraEmitter> _sources = new List<AuraEmitter>();
    // Кеш хайлайтеров (чтобы не делать GetComponent каждый кадр)
    private Dictionary<RoadTile, RoadTileHighlighter> _highlighters = new Dictionary<RoadTile, RoadTileHighlighter>();
    
    // Визуал
    private static readonly Color StrongBlue = new Color(0.35f, 0.75f, 1f, 1f);
    private static readonly Color LightBlue  = new Color(0.85f, 0.95f, 1f, 1f);
    private bool _dirty = false;

    private void Start()
    {
        _calculator = new CoverageCalculator();
        
        _roadManager = ServiceLocator.Get<IRoadManager>();
        if (_roadManager != null)
        {
            // Подписываемся именованным методом, чтобы можно было отписаться
            _roadManager.OnRoadAdded += OnRoadGraphChanged;
            _roadManager.OnRoadRemoved += OnRoadGraphChanged;
        }
    }

    private void OnDestroy()
    {
        // Обязательно отписываемся при уничтожении объекта
        if (_roadManager != null)
        {
            _roadManager.OnRoadAdded -= OnRoadGraphChanged;
            _roadManager.OnRoadRemoved -= OnRoadGraphChanged;
        }
    }

    // Обработчик событий изменений дороги
    private void OnRoadGraphChanged(Vector2Int pos)
    {
        _dirty = true;
    }

    public void ShowForEmitter(AuraEmitter emitter)
    {
        if (!_sources.Contains(emitter))
        {
            _sources.Add(emitter);
            _dirty = true;
        }
    }
    
    public void ShowPreview(Vector3 worldPos, float radius)
    {
        // Заглушка для превью (можно реализовать через временный эмиттер)
        // Если нужно превью при строительстве - добавьте логику сюда
    }

    public void HideAll()
    {
        _sources.Clear();
        ClearVisuals();
    }
    
    public void HidePreview()
    {
        // Логика скрытия превью
    }

    private void Update()
    {
        if (_dirty)
        {
            RefreshVisuals();
            _dirty = false;
        }
    }

    private void RefreshVisuals()
    {
        // 1. Сначала очищаем старую подсветку
        ClearVisuals();

        if (_sources.Count == 0) return;

        // 2. Запрашиваем расчет у Калькулятора (он делает всю тяжелую работу)
        var coverage = _calculator.CalculateCoverage(_sources);

        // 3. Применяем цвета
        foreach (var kvp in coverage)
        {
            RoadTile tile = kvp.Key;
            float eff = kvp.Value;

            // Ленивое получение компонента (кешируем результат)
            if (!_highlighters.TryGetValue(tile, out var hl))
            {
                if (tile != null)
                {
                    hl = tile.GetComponent<RoadTileHighlighter>();
                    _highlighters[tile] = hl;
                }
            }

            if (hl != null)
            {
                // Интерполяция цвета: от синего к светло-голубому на краях
                Color c = Color.Lerp(StrongBlue, LightBlue, (1f - eff) * (1f - eff));
                hl.SetHighlight(true, c);
            }
        }
    }

    private void ClearVisuals()
    {
        foreach (var kvp in _highlighters)
        {
            if (kvp.Value != null) kvp.Value.SetHighlight(false);
        }
    }
}