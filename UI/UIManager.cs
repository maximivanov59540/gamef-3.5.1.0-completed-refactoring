using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.UI; 

public class UIManager : MonoBehaviour
{
    [Header("Информационные панели")]
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private TextMeshProUGUI buildingNameText;
    
    [Header("Панель Подтверждения")]
    [SerializeField] private GameObject _confirmationPanel; 
    [SerializeField] private TextMeshProUGUI _confirmationText; 
    
    [Header("Контекстные Кнопки (Модули)")]
    [Tooltip("КОНТЕЙНЕР (пустой GameObject), куда будут 'спавниться' кнопки модулей")]
    [SerializeField] private GameObject moduleButtonContainer;
    [Tooltip("ПРЕФАБ кнопки 'UI_ModuleButton'")]
    [SerializeField] private GameObject moduleButtonPrefab;
    
    [Header("Панель Производства")]
    [Tooltip("Весь 'контейнер' со слайдером (панель)")]
    [SerializeField] private GameObject productionPanel;
    [Tooltip("Сам Слайдер")]
    [SerializeField] private Slider productivitySlider;
    [Tooltip("Текст для отображения % (напр. '100%')")]
    [SerializeField] private TextMeshProUGUI productivityText;

    [Header("Панель Склада")]
    [SerializeField] private GameObject warehousePanel;
    [SerializeField] private TextMeshProUGUI warehouseQueueText;

    [Header("Панель Баланса")]
    [SerializeField] private GameObject balancePanel;

    // --- Приватные ссылки ---
    private ResourceProducer _selectedProducer;
    private ModularBuilding _selectedFarm;
    private ZonedArea _selectedZone;
    
    private System.Action _onConfirmAction;
    private System.Action _onCancelAction;
    
    // Зависимость через интерфейс (Service Locator)
    private INotificationManager _notificationManager;
    
    private bool _sliderListenerActive = false;

    void Start()
    {
        // 1. Получаем сервисы через Service Locator
        _notificationManager = ServiceLocator.Get<INotificationManager>();
        
        // Если сервис не найден (например, запускаем сцену без Bootstrapper), 
        // можно попробовать найти старый вариант как fallback, но лучше полагаться на Locator.
        if (_notificationManager == null)
        {
            Debug.LogWarning("[UIManager] INotificationManager не найден в ServiceLocator. Уведомления не будут работать.");
        }

        // 2. Инициализация UI
        HideInfo();
        if (_confirmationPanel) _confirmationPanel.SetActive(false);
        if (moduleButtonContainer) moduleButtonContainer.SetActive(false);
        if (productionPanel) productionPanel.SetActive(false);
        if (warehousePanel) warehousePanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (PlayerInputController.Instance?.Selection != null)
            PlayerInputController.Instance.Selection.SelectionChanged += OnSelectionChanged;
    }

    private void OnDisable()
    {
        if (PlayerInputController.Instance?.Selection != null)
            PlayerInputController.Instance.Selection.SelectionChanged -= OnSelectionChanged;
    }
    
    private void OnSelectionChanged(IReadOnlyCollection<BuildingIdentity> selection)
    {
        HideInfo(); // Сначала всегда "прячем"
        
        if (selection != null && selection.Count == 1)
        {
            BuildingIdentity selectedBuilding = selection.First();
            if (selectedBuilding == null) return;
            
            ShowInfo(selectedBuilding.buildingData); // 1. Общая инфо

            // 2. "Ферма"?
            ModularBuilding farm = selectedBuilding.GetComponent<ModularBuilding>();
            if (farm != null)
            {
                _selectedFarm = farm;
                ShowModuleButtons(farm); 
            }
            // 3. "ИЛИ" "Монастырь"?
            else
            {
                ZonedArea zone = selectedBuilding.GetComponent<ZonedArea>();
                if (zone != null)
                {
                    _selectedZone = zone;
                    zone.ShowSlotHighlights();
                }
            }

            // 4. "ЭТО" "Склад"?
            Warehouse warehouse = selectedBuilding.GetComponent<Warehouse>();
            if (warehouse != null)
            {
                ShowWarehouseInfo(warehouse);
            }

            // 5. "ЭТО" "Производитель"?
            ResourceProducer producer = selectedBuilding.GetComponent<ResourceProducer>();
            if (producer != null)
            {
                _selectedProducer = producer;
                ShowProductionControls(producer); // "Включаем" слайдер
            }
        }
    }
    
    public void ShowInfo(BuildingData data)
    {
        if(data == null) return;
        if (infoPanel) infoPanel.SetActive(true);
        if (buildingNameText) buildingNameText.text = data.buildingName;
    }

    public void HideInfo()
    {
        if (infoPanel) infoPanel.SetActive(false);
        if (buildingNameText) buildingNameText.text = "";

        ClearModuleButtons();

        if (productionPanel)
            productionPanel.SetActive(false);

        if (productivitySlider && _sliderListenerActive)
        {
            productivitySlider.onValueChanged.RemoveListener(OnEfficiencySliderChanged);
            _sliderListenerActive = false;
        }

        if (warehousePanel)
            warehousePanel.SetActive(false);

        _selectedFarm = null;
        _selectedProducer = null;

        if (_selectedZone != null)
        {
            _selectedZone.HideSlotHighlights();
            _selectedZone = null;
        }
    }

    public void ToggleBalancePanel()
    {
        if (balancePanel != null)
        {
            balancePanel.SetActive(!balancePanel.activeSelf);
        }
    }
    
    // --- Подтверждение ---
    public void ShowConfirmation(string message, System.Action onConfirm, System.Action onCancel)
    {
        _onConfirmAction = onConfirm;
        _onCancelAction = onCancel;
        if (_confirmationText) _confirmationText.text = message;
        if (_confirmationPanel) _confirmationPanel.SetActive(true);
    }
    public void OnConfirmButton()
    {
        if (_confirmationPanel) _confirmationPanel.SetActive(false);
        _onConfirmAction?.Invoke();
        _onConfirmAction = null; _onCancelAction = null;
    }
    public void OnCancelButton()
    {
        if (_confirmationPanel) _confirmationPanel.SetActive(false);
        _onCancelAction?.Invoke();
        _onConfirmAction = null; _onCancelAction = null;
    }
    
    // --- Модули ---
    private void ClearModuleButtons()
    {
        if (moduleButtonContainer == null) return;
        
        var childrenToDestroy = new List<GameObject>();
        foreach (Transform child in moduleButtonContainer.transform)
        {
            childrenToDestroy.Add(child.gameObject);
        }

        foreach (var child in childrenToDestroy)
        {
            Destroy(child);
        }

        moduleButtonContainer.SetActive(false);
    }
    
    private void ShowModuleButtons(ModularBuilding farm)
    {
        if (moduleButtonContainer == null || moduleButtonPrefab == null) return;

        ClearModuleButtons();
        moduleButtonContainer.SetActive(true); 

        if (farm.allowedModules != null)
        {
            foreach (BuildingData moduleBP in farm.allowedModules)
            {
                GameObject btnGO = Instantiate(moduleButtonPrefab, moduleButtonContainer.transform);

                TextMeshProUGUI txt = btnGO.GetComponentInChildren<TextMeshProUGUI>();
                if (txt)
                {
                    txt.text = $"{moduleBP.buildingName} ({moduleBP.size.x}x{moduleBP.size.y})";
                }

                Button btn = btnGO.GetComponent<Button>();
                if (btn)
                {
                    bool canBuild = farm.CanAddModule();
                    btn.interactable = canBuild;
                    btn.onClick.AddListener(() => OnClick_BuildModule(moduleBP));
                }
            }
        }
    }

    public void OnClick_BuildModule(BuildingData moduleToBuild)
    {
        if (_selectedFarm == null)
        {
            Debug.LogError("Кнопка 'Build Module' нажата, но _selectedFarm == null!");
            return;
        }

        if (!_selectedFarm.CanAddModule())
        {
            // ИСПОЛЬЗУЕМ ИНТЕРФЕЙС СЕРВИСА
            _notificationManager?.ShowNotification("Достигнут лимит модулей!");
            return;
        }

        if (PlayerInputController.Instance != null)
        {
            PlayerInputController.Instance.EnterPlacingModuleMode(_selectedFarm, moduleToBuild);
        }
    }

    // --- Склад ---
    private void ShowWarehouseInfo(Warehouse warehouse)
    {
        if (warehousePanel == null || warehouseQueueText == null) return;
        
        warehousePanel.SetActive(true);
        warehouseQueueText.text = $"Очередь: {warehouse.GetQueueCount()} / {warehouse.maxCartQueue}";
    }

    // --- Производство ---
    private void ShowProductionControls(ResourceProducer producer)
    {
        if (productionPanel == null || productivitySlider == null) return;

        productionPanel.SetActive(true);

        if (productivitySlider != null)
        {
            float currentEfficiency = producer.GetEfficiency();
            productivitySlider.value = currentEfficiency;

            if (!_sliderListenerActive)
            {
                productivitySlider.onValueChanged.AddListener(OnEfficiencySliderChanged);
                _sliderListenerActive = true;
            }
        }

        UpdateEfficiencyText(producer.GetFinalEfficiency());
    }

    private void OnEfficiencySliderChanged(float value)
    {
        if (_selectedProducer != null)
        {
            _selectedProducer.SetEfficiency(value);
            UpdateEfficiencyText(_selectedProducer.GetFinalEfficiency());
        }
    }

    private void UpdateEfficiencyText(float value)
    {
        if (productivityText)
        {
            productivityText.text = $"{value * 100:F0}%";
        }
    }
}