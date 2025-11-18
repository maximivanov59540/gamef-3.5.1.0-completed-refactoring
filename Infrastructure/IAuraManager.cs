using UnityEngine;

public interface IAuraManager : IGameService
{
    void RegisterEmitter(AuraEmitter emitter);
    void UnregisterEmitter(AuraEmitter emitter);
    
    bool IsPositionInAura(Vector3 worldPos, AuraType type);
    
    // Визуализация
    void ShowRoadAura(AuraEmitter emitter);
    void ShowRoadAuraPreview(Vector3 worldPos, Vector2Int gridPos, Vector2Int baseSize, float rotation, float radius);
    void HideRoadAuraOverlay();
    void HideRoadAuraPreview();
}