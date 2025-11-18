using System.Collections;
using UnityEngine;

/// <summary>
/// Центральный менеджер экономики.
/// Refactored: Реализует IMoneyManager.
/// </summary>
public class MoneyManager : MonoBehaviour, IMoneyManager
{
    public static MoneyManager Instance { get; private set; }

    [Header("State")]
    [SerializeField] private float _currentMoney = 500f;
    [SerializeField] private float _taxIncomePerSecond = 0f;

    [Header("Settings")]
    public float upkeepCheckInterval = 60f;

    // Реализация свойств интерфейса
    public bool IsInDebt { get; private set; } = false;
    public float CurrentIncome => _taxIncomePerSecond;

    // События
    public event System.Action<float> OnMoneyChanged;
    public event System.Action<bool> OnDebtStatusChanged;

    private Coroutine _economyTickCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        _economyTickCoroutine = StartCoroutine(EconomyTick());
    }

    void Update()
    {
        if (_taxIncomePerSecond > 0)
        {
            AddMoney(_taxIncomePerSecond * Time.deltaTime);
        }
    }

    private void OnDestroy()
    {
        if (_economyTickCoroutine != null) StopCoroutine(_economyTickCoroutine);
    }

    // --- Реализация IMoneyManager ---

    public void AddMoney(float amount)
    {
        _currentMoney += amount;
        CheckDebt();
        OnMoneyChanged?.Invoke(_currentMoney);
    }

    public bool SpendMoney(float amount)
    {
        if (_currentMoney >= amount)
        {
            _currentMoney -= amount;
            CheckDebt();
            OnMoneyChanged?.Invoke(_currentMoney);
            return true;
        }
        return false;
    }

    public bool CanAffordMoney(float amount) => _currentMoney >= amount;
    public float GetCurrentMoney() => _currentMoney;

    private void CheckDebt()
    {
        bool newDebtState = _currentMoney < 0;
        if (IsInDebt != newDebtState)
        {
            IsInDebt = newDebtState;
            OnDebtStatusChanged?.Invoke(IsInDebt);
        }
    }

    // --- Внутренняя логика (Upkeep/Tax) ---

    private IEnumerator EconomyTick()
    {
        while (true)
        {
            yield return new WaitForSeconds(upkeepCheckInterval);
            CalculateTaxes();
            ProcessUpkeep();
        }
    }

    private void CalculateTaxes()
    {
        float totalIncomePerMinute = 0f;
        // Используем Registry для доступа к домам (это тоже можно перевести на ServiceLocator)
        var residences = BuildingRegistry.Instance?.GetAllResidences();
        if (residences != null)
        {
            foreach (var res in residences)
            {
                if (res != null && res.enabled)
                    totalIncomePerMinute += res.GetCurrentTax();
            }
        }

        _taxIncomePerSecond = totalIncomePerMinute / 60f;
    }

    private void ProcessUpkeep()
    {
        float totalUpkeep = 0f;
        var buildings = BuildingRegistry.Instance?.GetAllBuildings();
        if (buildings != null)
        {
            foreach (var b in buildings)
            {
                if (b != null && !b.isBlueprint && b.buildingData != null)
                {
                    totalUpkeep += b.buildingData.upkeepCostPerMinute;
                }
            }
        }

        if (totalUpkeep > 0)
        {
            SpendMoney(totalUpkeep); // SpendMoney сама проверит долг
            Debug.Log($"[Economy] Upkeep paid: {totalUpkeep}");
        }
    }
}