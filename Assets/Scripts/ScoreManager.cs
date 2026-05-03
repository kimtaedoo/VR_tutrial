using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Oculus.Interaction;

public class ScoreManager : MonoBehaviour
{
    [SerializeField] private Text scoreText;
    [SerializeField] private int throwsPerPlay = 10;
    [SerializeField] private float resultDelay = 1.25f;
    [SerializeField] private float resultDistanceFromPlayer = 1.5f;
    [SerializeField] private ResettableObject throwableResettable;

    private int score;
    private int throwCount;
    private bool scoringClosed;
    private GameObject resultCanvasObject;
    private GameObject resultMenu;
    private Text resultText;
    private Coroutine resultCoroutine;

    private int RemainingThrows => Mathf.Max(throwsPerPlay - throwCount, 0);

    private void Awake()
    {
        BuildResultMenu();
    }

    private void Start()
    {
        ResetGame();
    }

    public bool RegisterThrow()
    {
        if (scoringClosed || throwCount >= throwsPerPlay)
        {
            return false;
        }

        throwCount++;
        UpdateScoreText();

        if (throwCount >= throwsPerPlay)
        {
            if (resultCoroutine != null)
            {
                StopCoroutine(resultCoroutine);
            }

            resultCoroutine = StartCoroutine(ShowResultAfterDelay());
        }

        return true;
    }

    public void AddScore(int amount)
    {
        if (scoringClosed)
        {
            return;
        }

        score += amount;
        UpdateScoreText();
    }

    public void ResetGame()
    {
        score = 0;
        throwCount = 0;
        scoringClosed = false;

        if (resultCoroutine != null)
        {
            StopCoroutine(resultCoroutine);
            resultCoroutine = null;
        }

        if (resultCanvasObject != null)
        {
            resultCanvasObject.SetActive(false);
        }
        else if (resultMenu != null)
        {
            resultMenu.SetActive(false);
        }

        SetScoreDisplayVisible(true);

        if (throwableResettable == null)
        {
            throwableResettable = Object.FindAnyObjectByType<ResettableObject>();
        }

        if (throwableResettable != null)
        {
            throwableResettable.ResetToStart();
        }

        UpdateScoreText();
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}\nDarts: {RemainingThrows}/{throwsPerPlay}";
        }
    }

    private System.Collections.IEnumerator ShowResultAfterDelay()
    {
        yield return new WaitForSeconds(resultDelay);

        scoringClosed = true;
        if (resultText != null)
        {
            resultText.text = $"Final Score\n{score}";
        }

        PlaceResultCanvasInFrontOfPlayer();
        SetScoreDisplayVisible(false);
        HideThrowableUntilContinue();

        if (resultCanvasObject != null)
        {
            resultCanvasObject.SetActive(true);
        }
        else if (resultMenu != null)
        {
            resultMenu.SetActive(true);
        }

        resultCoroutine = null;
    }

    private void BuildResultMenu()
    {
        Canvas scoreCanvas = scoreText != null
            ? scoreText.GetComponentInParent<Canvas>()
            : Object.FindAnyObjectByType<Canvas>();

        if (scoreCanvas != null)
        {
            EnsureCanvasInteraction(scoreCanvas);
        }

        resultCanvasObject = GameObject.Find("ResultCanvas");
        Canvas resultCanvas = resultCanvasObject != null
            ? resultCanvasObject.GetComponent<Canvas>()
            : CreateResultCanvas();

        if (resultCanvas == null)
        {
            return;
        }

        EnsureCanvasInteraction(resultCanvas);

        Transform existingMenu = resultCanvas.transform.Find("ResultMenu");
        resultMenu = existingMenu != null
            ? existingMenu.gameObject
            : CreateResultMenu(resultCanvas.transform);

        resultText = resultMenu.transform.Find("ResultText")?.GetComponent<Text>();
        Button continueButton = resultMenu.transform.Find("ContinueButton")?.GetComponent<Button>();
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(ResetGame);
            continueButton.onClick.AddListener(ResetGame);
        }

        ControllerUiRayPointer rayPointer = resultCanvas.GetComponent<ControllerUiRayPointer>();
        if (rayPointer == null)
        {
            rayPointer = resultCanvas.gameObject.AddComponent<ControllerUiRayPointer>();
        }

        rayPointer.Configure(resultCanvas, continueButton);
        resultCanvasObject.SetActive(false);
    }

    private Canvas CreateResultCanvas()
    {
        resultCanvasObject = new GameObject("ResultCanvas", typeof(RectTransform), typeof(Canvas));
        Canvas canvas = resultCanvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        RectTransform canvasRect = resultCanvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(520f, 360f);
        canvasRect.localScale = Vector3.one * 0.0025f;

        return canvas;
    }

    private GameObject CreateResultMenu(Transform parent)
    {
        GameObject menuObject = new GameObject("ResultMenu", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        menuObject.transform.SetParent(parent, false);

        RectTransform menuRect = menuObject.GetComponent<RectTransform>();
        menuRect.anchorMin = new Vector2(0.5f, 0.5f);
        menuRect.anchorMax = new Vector2(0.5f, 0.5f);
        menuRect.anchoredPosition = Vector2.zero;
        menuRect.sizeDelta = new Vector2(500f, 330f);

        Image background = menuObject.GetComponent<Image>();
        background.color = new Color(0.04f, 0.04f, 0.04f, 0.88f);

        Text finalScoreText = CreateText(menuObject.transform, "ResultText", new Vector2(0f, 72f), new Vector2(460f, 150f), 56);
        finalScoreText.text = "Final Score\n0";
        finalScoreText.transform.SetAsLastSibling();

        GameObject buttonObject = new GameObject("ContinueButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(menuObject.transform, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(0f, -105f);
        buttonRect.sizeDelta = new Vector2(260f, 72f);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.9f, 0.9f, 0.9f, 1f);

        Button continueButton = buttonObject.GetComponent<Button>();
        continueButton.targetGraphic = buttonImage;
        continueButton.onClick.AddListener(ResetGame);

        Text buttonText = CreateText(buttonObject.transform, "Text", Vector2.zero, new Vector2(260f, 72f), 34);
        buttonText.text = "Continue";
        buttonText.color = Color.black;

        return menuObject;
    }

    private void PlaceResultCanvasInFrontOfPlayer()
    {
        if (resultCanvasObject == null)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            resultCanvasObject.transform.SetPositionAndRotation(
                new Vector3(0f, 1.4f, resultDistanceFromPlayer),
                Quaternion.identity);
            return;
        }

        Canvas resultCanvas = resultCanvasObject.GetComponent<Canvas>();
        if (resultCanvas != null)
        {
            resultCanvas.worldCamera = mainCamera;
        }

        Vector3 forward = Vector3.ProjectOnPlane(mainCamera.transform.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = mainCamera.transform.forward;
        }

        forward.Normalize();
        Vector3 position = mainCamera.transform.position + forward * resultDistanceFromPlayer;
        position.y = mainCamera.transform.position.y;

        resultCanvasObject.transform.position = position;
        resultCanvasObject.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }

    private void SetScoreDisplayVisible(bool visible)
    {
        if (scoreText == null)
        {
            return;
        }

        scoreText.gameObject.SetActive(visible);
    }

    private void HideThrowableUntilContinue()
    {
        if (throwableResettable == null)
        {
            throwableResettable = Object.FindAnyObjectByType<ResettableObject>();
        }

        if (throwableResettable != null)
        {
            throwableResettable.HideUntilReset();
        }
    }

    private Text CreateText(Transform parent, string objectName, Vector2 anchoredPosition, Vector2 size, int fontSize)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = anchoredPosition;
        textRect.sizeDelta = size;

        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null)
        {
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        text.fontSize = fontSize;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = Mathf.Max(12, fontSize / 2);
        text.resizeTextMaxSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        return text;
    }

    private void EnsureCanvasInteraction(Canvas scoreCanvas)
    {
        if (!scoreCanvas.TryGetComponent(out GraphicRaycaster _))
        {
            scoreCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        PointableCanvas pointableCanvas = scoreCanvas.GetComponent<PointableCanvas>();
        if (pointableCanvas == null)
        {
            pointableCanvas = scoreCanvas.gameObject.AddComponent<PointableCanvas>();
        }

        pointableCanvas.InjectCanvas(scoreCanvas);

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            eventSystem = Object.FindAnyObjectByType<EventSystem>();
        }

        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            eventSystem = eventSystemObject.GetComponent<EventSystem>();
        }

        if (eventSystem.GetComponent<PointableCanvasModule>() == null)
        {
            eventSystem.gameObject.AddComponent<PointableCanvasModule>();
        }

        if (eventSystem.GetComponent<StandaloneInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<StandaloneInputModule>();
        }
    }
}
