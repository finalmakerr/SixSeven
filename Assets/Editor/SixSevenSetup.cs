using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public static class SixSevenSetup
{
    private const string ScenesFolder = "Assets/scenes";
    private const string PrefabsFolder = "Assets/prefabs";
    private const string TilePrefabPath = "Assets/prefabs/TileUI.prefab";

    [MenuItem("Tools/SixSeven/Setup")]
    public static void Setup()
    {
        EnsureFolders();
        CreateTilePrefab();
        CreateMainMenuScene();
        CreateGameScene();
        SetBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("SixSeven setup complete.");
    }

    [MenuItem("Tools/SixSeven/Build Android APK")]
    public static void BuildAndroidApk()
    {
        EnsureFolders();
        SetBuildSettings();

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string buildDir = Path.Combine(projectRoot, "Builds", "Android");
        Directory.CreateDirectory(buildDir);
        string buildPath = Path.Combine(buildDir, "SixSeven.apk");

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        }

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { $"{ScenesFolder}/MainMenu.unity", $"{ScenesFolder}/Game.unity" },
            locationPathName = buildPath,
            target = BuildTarget.Android,
            options = BuildOptions.Development | BuildOptions.AllowDebugging
        };

        BuildPipeline.BuildPlayer(options);
        Debug.Log($"Android APK build complete: {buildPath}");
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Editor"))
            AssetDatabase.CreateFolder("Assets", "Editor");
        if (!AssetDatabase.IsValidFolder(ScenesFolder))
            AssetDatabase.CreateFolder("Assets", "scenes");
        if (!AssetDatabase.IsValidFolder(PrefabsFolder))
            AssetDatabase.CreateFolder("Assets", "prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/scripts"))
            AssetDatabase.CreateFolder("Assets", "scripts");
    }

    private static void CreateTilePrefab()
    {
        GameObject tile = new GameObject("TileUI", typeof(RectTransform), typeof(Image), typeof(Button), typeof(TileView));
        RectTransform rt = tile.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(90, 90);

        Image image = tile.GetComponent<Image>();
        image.color = Color.white;
        image.preserveAspect = true;

        Button button = tile.GetComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;

        GameObject selection = new GameObject("Selection", typeof(RectTransform), typeof(Image));
        selection.transform.SetParent(tile.transform, false);
        RectTransform selRt = selection.GetComponent<RectTransform>();
        selRt.anchorMin = Vector2.zero;
        selRt.anchorMax = Vector2.one;
        selRt.offsetMin = Vector2.zero;
        selRt.offsetMax = Vector2.zero;
        Image selImage = selection.GetComponent<Image>();
        selImage.color = new Color(1f, 0.92f, 0.2f, 0.35f);
        selImage.raycastTarget = false;
        selection.SetActive(false);

        TileView tileView = tile.GetComponent<TileView>();
        SerializedObject so = new SerializedObject(tileView);
        so.FindProperty("image").objectReferenceValue = image;
        so.FindProperty("selection").objectReferenceValue = selection;
        so.FindProperty("button").objectReferenceValue = button;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(tile, TilePrefabPath);
        Object.DestroyImmediate(tile);
    }

    private static void CreateMainMenuScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject camera = new GameObject("Main Camera", typeof(Camera));
        camera.tag = "MainCamera";

        Canvas canvas = CreateCanvas();
        CreateEventSystem();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject title = CreateText(canvas.transform, "Title", "SixSeven Match-3", 64, TextAnchor.MiddleCenter, font);
        RectTransform titleRt = title.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 1f);
        titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -140f);
        titleRt.sizeDelta = new Vector2(900f, 120f);

        GameObject playButton = CreateButton(canvas.transform, "PlayButton", "Play", 40, font);
        RectTransform playRt = playButton.GetComponent<RectTransform>();
        playRt.anchorMin = new Vector2(0.5f, 0.5f);
        playRt.anchorMax = new Vector2(0.5f, 0.5f);
        playRt.anchoredPosition = new Vector2(0f, -40f);
        playRt.sizeDelta = new Vector2(320f, 90f);

        GameObject menuUi = new GameObject("MainMenuUI", typeof(MainMenuUI));
        Button playBtn = playButton.GetComponent<Button>();
        MainMenuUI menu = menuUi.GetComponent<MainMenuUI>();
        playBtn.onClick.AddListener(menu.Play);

        EditorSceneManager.SaveScene(scene, $"{ScenesFolder}/MainMenu.unity");
    }

    private static void CreateGameScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject camera = new GameObject("Main Camera", typeof(Camera));
        camera.tag = "MainCamera";

        Canvas canvas = CreateCanvas();
        CreateEventSystem();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject topBar = new GameObject("TopBar", typeof(RectTransform));
        topBar.transform.SetParent(canvas.transform, false);
        RectTransform topRt = topBar.GetComponent<RectTransform>();
        topRt.anchorMin = new Vector2(0f, 1f);
        topRt.anchorMax = new Vector2(1f, 1f);
        topRt.pivot = new Vector2(0.5f, 1f);
        topRt.sizeDelta = new Vector2(0f, 140f);
        topRt.anchoredPosition = Vector2.zero;

        GameObject scoreText = CreateText(topBar.transform, "ScoreText", "Score: 0", 36, TextAnchor.MiddleLeft, font);
        RectTransform scoreRt = scoreText.GetComponent<RectTransform>();
        scoreRt.anchorMin = new Vector2(0f, 0f);
        scoreRt.anchorMax = new Vector2(0.5f, 1f);
        scoreRt.offsetMin = new Vector2(40f, 0f);
        scoreRt.offsetMax = new Vector2(-20f, 0f);

        GameObject movesText = CreateText(topBar.transform, "MovesText", "Moves: 30", 36, TextAnchor.MiddleRight, font);
        RectTransform movesRt = movesText.GetComponent<RectTransform>();
        movesRt.anchorMin = new Vector2(0.5f, 0f);
        movesRt.anchorMax = new Vector2(1f, 1f);
        movesRt.offsetMin = new Vector2(20f, 0f);
        movesRt.offsetMax = new Vector2(-40f, 0f);

        GameObject boardRoot = new GameObject("BoardRoot", typeof(RectTransform));
        boardRoot.transform.SetParent(canvas.transform, false);
        RectTransform boardRt = boardRoot.GetComponent<RectTransform>();
        boardRt.anchorMin = new Vector2(0.5f, 0.5f);
        boardRt.anchorMax = new Vector2(0.5f, 0.5f);
        boardRt.sizeDelta = new Vector2(760f, 760f);
        boardRt.anchoredPosition = new Vector2(0f, -40f);

        GameObject endPanel = new GameObject("EndPanel", typeof(RectTransform), typeof(Image));
        endPanel.transform.SetParent(canvas.transform, false);
        RectTransform endRt = endPanel.GetComponent<RectTransform>();
        endRt.anchorMin = Vector2.zero;
        endRt.anchorMax = Vector2.one;
        endRt.offsetMin = Vector2.zero;
        endRt.offsetMax = Vector2.zero;
        Image endBg = endPanel.GetComponent<Image>();
        endBg.color = new Color(0f, 0f, 0f, 0.75f);

        GameObject endCard = new GameObject("EndCard", typeof(RectTransform), typeof(Image));
        endCard.transform.SetParent(endPanel.transform, false);
        RectTransform cardRt = endCard.GetComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0.5f, 0.5f);
        cardRt.anchorMax = new Vector2(0.5f, 0.5f);
        cardRt.sizeDelta = new Vector2(600f, 460f);
        Image cardImg = endCard.GetComponent<Image>();
        cardImg.color = new Color(1f, 1f, 1f, 0.95f);

        GameObject gameOverText = CreateText(endCard.transform, "GameOverText", "Game Over", 48, TextAnchor.MiddleCenter, font);
        RectTransform goRt = gameOverText.GetComponent<RectTransform>();
        goRt.anchorMin = new Vector2(0.5f, 1f);
        goRt.anchorMax = new Vector2(0.5f, 1f);
        goRt.anchoredPosition = new Vector2(0f, -60f);
        goRt.sizeDelta = new Vector2(520f, 80f);

        GameObject finalScoreText = CreateText(endCard.transform, "FinalScoreText", "Final Score: 0", 32, TextAnchor.MiddleCenter, font);
        RectTransform fsRt = finalScoreText.GetComponent<RectTransform>();
        fsRt.anchorMin = new Vector2(0.5f, 0.5f);
        fsRt.anchorMax = new Vector2(0.5f, 0.5f);
        fsRt.anchoredPosition = new Vector2(0f, 40f);
        fsRt.sizeDelta = new Vector2(520f, 60f);

        GameObject restartButton = CreateButton(endCard.transform, "RestartButton", "Restart", 28, font);
        RectTransform restartRt = restartButton.GetComponent<RectTransform>();
        restartRt.anchorMin = new Vector2(0.5f, 0f);
        restartRt.anchorMax = new Vector2(0.5f, 0f);
        restartRt.anchoredPosition = new Vector2(0f, 120f);
        restartRt.sizeDelta = new Vector2(280f, 70f);

        GameObject menuButton = CreateButton(endCard.transform, "MainMenuButton", "Main Menu", 28, font);
        RectTransform menuRt = menuButton.GetComponent<RectTransform>();
        menuRt.anchorMin = new Vector2(0.5f, 0f);
        menuRt.anchorMax = new Vector2(0.5f, 0f);
        menuRt.anchoredPosition = new Vector2(0f, 40f);
        menuRt.sizeDelta = new Vector2(280f, 70f);

        endPanel.SetActive(false);

        GameObject managerObj = new GameObject("GameManager", typeof(GameManager));
        GameManager manager = managerObj.GetComponent<GameManager>();
        TileView prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TilePrefabPath).GetComponent<TileView>();

        SerializedObject so = new SerializedObject(manager);
        so.FindProperty("boardRoot").objectReferenceValue = boardRt;
        so.FindProperty("tilePrefab").objectReferenceValue = prefab;
        so.FindProperty("scoreText").objectReferenceValue = scoreText.GetComponent<Text>();
        so.FindProperty("movesText").objectReferenceValue = movesText.GetComponent<Text>();
        so.FindProperty("endPanel").objectReferenceValue = endPanel;
        so.FindProperty("endScoreText").objectReferenceValue = finalScoreText.GetComponent<Text>();
        so.FindProperty("restartButton").objectReferenceValue = restartButton.GetComponent<Button>();
        so.FindProperty("mainMenuButton").objectReferenceValue = menuButton.GetComponent<Button>();
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, $"{ScenesFolder}/Game.unity");
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObj = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static void CreateEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null)
            return;
#if ENABLE_INPUT_SYSTEM
        GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
#else
        // If this falls back to StandaloneInputModule, set Project Settings > Player > Active Input Handling
        // to "Input System Package (New)" (or "Both") so InputSystemUIInputModule is available.
        GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
#endif
    }

    private static GameObject CreateText(Transform parent, string name, string text, int fontSize, TextAnchor alignment, Font font)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Text txt = go.GetComponent<Text>();
        txt.text = text;
        txt.font = font;
        txt.fontSize = fontSize;
        txt.alignment = alignment;
        txt.color = new Color(0.1f, 0.1f, 0.1f, 1f);
        return go;
    }

    private static GameObject CreateButton(Transform parent, string name, string label, int fontSize, Font font)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.color = new Color(0.2f, 0.6f, 0.95f, 1f);
        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = img;

        GameObject textObj = CreateText(go.transform, "Text", label, fontSize, TextAnchor.MiddleCenter, font);
        RectTransform tr = textObj.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;
        textObj.GetComponent<Text>().color = Color.white;

        return go;
    }

    private static void SetBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene($"{ScenesFolder}/MainMenu.unity", true),
            new EditorBuildSettingsScene($"{ScenesFolder}/Game.unity", true)
        };
    }
}
