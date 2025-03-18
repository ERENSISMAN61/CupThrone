using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class LogScreen : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI logText;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform contentRectTransform;
    [SerializeField] private float minFontSize = 8f;
    [SerializeField] private float maxFontSize = 24f;
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private int maxStoredLogs = 1000; // Maksimum saklanacak log sayısı

    private Queue<string> logLines = new Queue<string>();
    private bool isInitialized = false;
    private bool shouldAutoScroll = true;
    private float currentFontSize = 12f;
    private ContentSizeFitter contentSizeFitter;
    private bool userScrolling = false;
    private float scrollStopTimer = 0f;
    private const float SCROLL_STOP_THRESHOLD = 0.1f;
    private bool isDragging = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (logText != null)
        {
            currentFontSize = 12f;
            logText.fontSize = currentFontSize;
            logText.alignment = TextAlignmentOptions.TopLeft;
            logText.enableWordWrapping = true;
            logText.raycastTarget = true;
        }

        // Content Size Fitter'ı ayarla
        contentSizeFitter = contentRectTransform.gameObject.AddComponent<ContentSizeFitter>();
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Content RectTransform ayarları
        contentRectTransform.anchorMin = new Vector2(0, 1);
        contentRectTransform.anchorMax = new Vector2(1, 1);
        contentRectTransform.pivot = new Vector2(0.5f, 1f);

        // Scroll ayarları
        if (scrollRect != null)
        {
            scrollRect.onValueChanged.AddListener(OnScroll);
            scrollRect.vertical = true;
            scrollRect.horizontal = false;
            scrollRect.verticalScrollbar.numberOfSteps = 0;
            scrollRect.scrollSensitivity = 35f;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.1f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;

            // Sürükleme olaylarını dinle
            EventTrigger trigger = scrollRect.gameObject.AddComponent<EventTrigger>();

            EventTrigger.Entry beginDragEntry = new EventTrigger.Entry();
            beginDragEntry.eventID = EventTriggerType.BeginDrag;
            beginDragEntry.callback.AddListener((data) => { OnBeginDrag(); });
            trigger.triggers.Add(beginDragEntry);

            EventTrigger.Entry endDragEntry = new EventTrigger.Entry();
            endDragEntry.eventID = EventTriggerType.EndDrag;
            endDragEntry.callback.AddListener((data) => { OnEndDrag(); });
            trigger.triggers.Add(endDragEntry);
        }
    }

    private void OnBeginDrag()
    {
        isDragging = true;
        shouldAutoScroll = false;
        userScrolling = true;
    }

    private void OnEndDrag()
    {
        isDragging = false;
        scrollStopTimer = 0f;
    }

    void OnScroll(Vector2 value)
    {
        if (!isDragging)
        {
            userScrolling = true;
            scrollStopTimer = 0f;
            shouldAutoScroll = false;
        }
    }

    void LateUpdate()
    {
        // Kullanıcı scroll yapıyorsa veya sürüklüyorsa
        if (userScrolling && !isDragging)
        {
            scrollStopTimer += Time.deltaTime;
            if (scrollStopTimer >= SCROLL_STOP_THRESHOLD)
            {
                userScrolling = false;
                // Eğer en alttaysak auto-scroll'u tekrar aktif et
                if (scrollRect.verticalNormalizedPosition <= 0.01f)
                {
                    shouldAutoScroll = true;
                }
            }
        }

        // CTRL + Mouse Wheel ile zoom kontrolü
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            float scrollDelta = Input.mouseScrollDelta.y;
            if (scrollDelta != 0 && logText != null)
            {
                currentFontSize = Mathf.Clamp(currentFontSize + scrollDelta * zoomSpeed, minFontSize, maxFontSize);
                logText.fontSize = currentFontSize;
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRectTransform);
            }
        }
    }

    void OnEnable()
    {
        if (!isInitialized)
        {
            Application.logMessageReceived += HandleLog;
            isInitialized = true;
        }
    }

    void OnDisable()
    {
        if (isInitialized)
        {
            Application.logMessageReceived -= HandleLog;
            isInitialized = false;
        }
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        string color = "white";
        switch (type)
        {
            case LogType.Error:
            case LogType.Exception:
                color = "red";
                logString = $"{logString}\n{stackTrace}";
                break;
            case LogType.Warning:
                color = "yellow";
                break;
            case LogType.Log:
                color = "white";
                break;
        }

        string timeStamp = DateTime.Now.ToString("HH:mm:ss");
        string formattedLog = $"[{timeStamp}] <color={color}>{logString}</color>";

        string[] lines = formattedLog.Split('\n');
        foreach (string line in lines)
        {
            if (!string.IsNullOrEmpty(line))
            {
                logLines.Enqueue(line);

                // Sadece çok fazla log birikirse en eskileri sil
                if (logLines.Count > maxStoredLogs)
                {
                    logLines.Dequeue();
                }
            }
        }

        UpdateLogDisplay();
    }

    private void UpdateLogDisplay()
    {
        if (logText != null)
        {
            logText.text = string.Join("\n", logLines.ToArray());
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRectTransform);

            if (shouldAutoScroll && !isDragging && !userScrolling)
            {
                StartCoroutine(ScrollToBottom());
            }
        }
    }

    private IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        if (scrollRect != null && shouldAutoScroll && !userScrolling)
        {
            scrollRect.verticalNormalizedPosition = 0f;
            Canvas.ForceUpdateCanvases();
        }
    }
}
