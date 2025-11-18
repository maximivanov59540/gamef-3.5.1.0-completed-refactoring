using TMPro;
using UnityEngine;

public class UIResourceDisplay : MonoBehaviour
{
    // Теперь используем интерфейсы
    private IResourceManager _resourceManager;
    private IMoneyManager _moneyManager;

    [Header("UI Elements")]
    public TextMeshProUGUI woodText;
    public TextMeshProUGUI stoneText;
    public TextMeshProUGUI planksText;
    public TextMeshProUGUI populationText;
    public TextMeshProUGUI moneyText;

    void Start()
    {
        // Получаем сервисы
        _resourceManager = ServiceLocator.Get<IResourceManager>();
        _moneyManager = ServiceLocator.Get<IMoneyManager>();

        if (_resourceManager != null)
        {
            _resourceManager.OnResourceChanged += UpdateResourceText;
            if (_resourceManager.Population != null)
                _resourceManager.Population.OnAnyPopulationChanged += UpdatePopulationText;
                
            // Инициализация
            UpdateResourceText(ResourceType.Wood);
            UpdateResourceText(ResourceType.Stone);
            UpdateResourceText(ResourceType.Planks);
            UpdatePopulationText();
        }

        if (_moneyManager != null)
        {
            _moneyManager.OnMoneyChanged += UpdateMoneyText;
            UpdateMoneyText(_moneyManager.GetCurrentMoney());
        }
    }

    void OnDestroy()
    {
        if (_resourceManager != null)
        {
            _resourceManager.OnResourceChanged -= UpdateResourceText;
            if (_resourceManager.Population != null)
                _resourceManager.Population.OnAnyPopulationChanged -= UpdatePopulationText;
        }

        if (_moneyManager != null)
        {
            _moneyManager.OnMoneyChanged -= UpdateMoneyText;
        }
    }

    private void UpdateResourceText(ResourceType type)
    {
        float amount = _resourceManager.GetResourceAmount(type);
        string text = $"{type}: {Mathf.FloorToInt(amount)}";

        switch (type)
        {
            case ResourceType.Wood: if (woodText) woodText.text = text; break;
            case ResourceType.Stone: if (stoneText) stoneText.text = text; break;
            case ResourceType.Planks: if (planksText) planksText.text = text; break;
        }
    }

    private void UpdatePopulationText()
    {
        if (populationText && _resourceManager != null)
        {
            var pop = _resourceManager.Population;
            populationText.text = $"Pop: {pop.GetTotalCurrentPopulation()} / {pop.GetTotalMaxPopulation()}";
        }
    }

    private void UpdateMoneyText(float money)
    {
        if (moneyText) moneyText.text = $"Gold: {Mathf.FloorToInt(money)}";
    }
}