public interface IMoneyManager : IGameService
{
    void AddMoney(float amount);
    bool SpendMoney(float amount);
    bool CanAffordMoney(float amount);
    float GetCurrentMoney();
    
    bool IsInDebt { get; }
    float CurrentIncome { get; }
    
    event System.Action<float> OnMoneyChanged;
    event System.Action<bool> OnDebtStatusChanged;
}