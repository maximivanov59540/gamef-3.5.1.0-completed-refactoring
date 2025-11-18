using UnityEngine;
using TMPro;

public class NotificationManager : MonoBehaviour, INotificationManager
{
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private float displayDuration = 3f;

    private float timer;
    private bool isNotificationActive = false;

    void Start()
    {
        HideNotification();
    }

    void Update()
    {
        if (isNotificationActive)
        {
            timer -= Time.deltaTime;
            if (timer <= 0) HideNotification();
        }
    }

    // Реализация интерфейса
    public void ShowNotification(string message)
    {
        notificationText.text = message;
        notificationPanel.SetActive(true);
        isNotificationActive = true;
        timer = displayDuration;
    }

    private void HideNotification()
    {
        notificationPanel.SetActive(false);
        isNotificationActive = false;
    }
}