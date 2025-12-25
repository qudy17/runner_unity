using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.IO;
using System;

public class UIController : MonoBehaviour
{
    Player player;

    [Header("Distance Display")]
    public Text distanceText;
    public TMP_Text distanceTextTMP;

    [Header("Coin Display")]
    public Text coinText;
    public TMP_Text coinTextTMP;

    [Header("Record Display")]
    public Text recordText;
    public TMP_Text recordTextTMP;

    [Header("Results Screen")]
    public GameObject results;

    public Text finalDistanceText;
    public TMP_Text finalDistanceTextTMP;

    public Text finalCoinsText;
    public TMP_Text finalCoinsTextTMP;

    public Text newRecordText;
    public TMP_Text newRecordTextTMP;

    [Header("Display Settings")]
    public bool showTotalCoins = false;
    public string recordPrefix = "Record: ";

    // Система сохранения
    private string saveFolderPath;
    private string saveFilePath;
    private PlayerSaveData saveData;

    // Статический доступ
    private static UIController instance;
    public static UIController Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<UIController>();
            }
            return instance;
        }
    }

    // Публичные свойства для доступа к данным
    public PlayerSaveData SaveData
    {
        get { return saveData; }
    }

    private bool resultsShown = false;
    private bool isInitialized = false;

    private void Awake()
    {
        // Singleton pattern
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        // НЕ используем DontDestroyOnLoad - это важно!
        // Иначе UI будет сохраняться между сценами

        FindUIElements();
        InitializeSaveSystem();
        LoadGameData();

        // Сбрасываем флаги
        resultsShown = false;
        isInitialized = true;

        if (results != null)
        {
            results.SetActive(false);
        }
    }

    void FindUIElements()
    {
        FindUIText("ScoreText", ref distanceText, ref distanceTextTMP);
        FindUIText("CoinCounter", ref coinText, ref coinTextTMP);
        FindUIText("ScoreRecord", ref recordText, ref recordTextTMP);

        if (results == null)
            results = GameObject.Find("Results");

        if (results != null)
        {
            FindUITextInResults("FinalDistance", ref finalDistanceText, ref finalDistanceTextTMP);
            FindUITextInResults("FinalCoins", ref finalCoinsText, ref finalCoinsTextTMP);
            FindUITextInResults("NewRecordText", ref newRecordText, ref newRecordTextTMP);
        }
    }

    void FindUIText(string objectName, ref Text legacyText, ref TMP_Text tmpText)
    {
        GameObject obj = GameObject.Find(objectName);
        if (obj != null)
        {
            TMP_Text tmp = obj.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                tmpText = tmp;
            }

            Text text = obj.GetComponent<Text>();
            if (text != null)
            {
                legacyText = text;
            }

            if (tmp == null && text == null)
            {
                Debug.LogWarning("⚠️ GameObject '" + objectName + "' has no Text or TMP_Text component!");
            }
        }
    }

    void FindUITextInResults(string childName, ref Text legacyText, ref TMP_Text tmpText)
    {
        if (results == null) return;

        Transform child = results.transform.Find(childName);
        if (child != null)
        {
            GameObject obj = child.gameObject;

            TMP_Text tmp = obj.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                tmpText = tmp;
            }

            Text text = obj.GetComponent<Text>();
            if (text != null)
            {
                legacyText = text;
            }
        }
    }

    void Start()
    {
        UpdateRecordDisplay();
    }

    void Update()
    {
        if (player == null)
        {
            // Пытаемся найти игрока если его нет
            player = GameObject.Find("Player")?.GetComponent<Player>();
            return;
        }

        UpdateDistanceDisplay();

        UpdateCoinDisplay();

        if (player.isDead && results != null && !results.activeSelf)
        {
            ShowResults();
        }
    }

    void UpdateDistanceDisplay()
    {
        // Используем GetMultipliedDistance() вместо GetDistance()
        int displayDistance = player.GetMultipliedDistance();

        if (distanceText != null)
            distanceText.text = displayDistance.ToString();

        if (distanceTextTMP != null)
            distanceTextTMP.text = displayDistance.ToString();
    }

    void UpdateCoinDisplay()
    {
        int coins = showTotalCoins ? player.GetTotalCoins() : player.GetSessionCoins();
        string coinString = coins.ToString();

        if (coinText != null)
            coinText.text = coinString;

        if (coinTextTMP != null)
            coinTextTMP.text = coinString;
    }

    void UpdateRecordDisplay()
    {
        string recordString = recordPrefix + saveData.highScore.ToString();

        if (recordText != null)
            recordText.text = recordString;

        if (recordTextTMP != null)
            recordTextTMP.text = recordString;
    }

    void ShowResults()
    {
        if (results == null || resultsShown) return;

        // Получаем актуальную ссылку на игрока
        if (player == null || player.isDead)
        {
            player = GameObject.Find("Player")?.GetComponent<Player>();
            if (player == null)
            {
                Debug.LogError("Player not found for results!");
                return;
            }
        }

        int finalDistance = player.GetMultipliedDistance();
        int sessionCoins = player.GetSessionCoins();
        bool newHighScore = CheckAndUpdateHighScore(finalDistance);

        // Активируем экран результатов
        results.SetActive(true);
        resultsShown = true;

        // Обновляем тексты
        UpdateTextElement(finalDistanceText, finalDistanceTextTMP, "Distance: " + finalDistance.ToString());
        UpdateTextElement(finalCoinsText, finalCoinsTextTMP, sessionCoins.ToString());

        if (newHighScore)
        {
            UpdateTextElement(newRecordText, newRecordTextTMP, "NEW RECORD!");
        }
        else
        {
            if (newRecordText != null) newRecordText.gameObject.SetActive(false);
            if (newRecordTextTMP != null) newRecordTextTMP.gameObject.SetActive(false);
        }

        UpdateRecordDisplay();
        UpdateGameStats(finalDistance, sessionCoins);
        SaveGameData();
    }

    void UpdateTextElement(Text legacyText, TMP_Text tmpText, string text)
    {
        if (legacyText != null)
        {
            legacyText.text = text;
            legacyText.gameObject.SetActive(true);
        }

        if (tmpText != null)
        {
            tmpText.text = text;
            tmpText.gameObject.SetActive(true);
        }
    }

    void InitializeSaveSystem()
    {
        string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        saveFolderPath = Path.Combine(documentsPath, "Neon Runner");
        saveFilePath = Path.Combine(saveFolderPath, "savegame.json");

        if (!Directory.Exists(saveFolderPath))
        {
            Directory.CreateDirectory(saveFolderPath);
            Debug.Log("✅ Created save folder: " + saveFolderPath);
        }
    }

    public void LoadGameData()
    {
        if (File.Exists(saveFilePath))
        {
            try
            {
                string json = File.ReadAllText(saveFilePath);
                saveData = JsonUtility.FromJson<PlayerSaveData>(json);

                // Проверка целостности данных
                if (saveData.unlockedSkins == null)
                    saveData.unlockedSkins = new List<string>();

                if (!saveData.unlockedSkins.Contains("Punk"))
                    saveData.unlockedSkins.Add("Punk");

                if (string.IsNullOrEmpty(saveData.selectedSkin))
                    saveData.selectedSkin = "Punk";

                Debug.Log("✅ Game data loaded. High Score: " + saveData.highScore +
                         ", Total Coins: " + saveData.totalCoins +
                         ", Dash Level: " + saveData.dashUpgradeLevel +
                         ", Skin: " + saveData.selectedSkin);
            }
            catch (Exception e)
            {
                Debug.LogError("❌ Failed to load game data: " + e.Message);
                saveData = new PlayerSaveData();
            }
        }
        else
        {
            saveData = new PlayerSaveData();
            Debug.Log("ℹ️ No save file found. Created new save data.");
        }
    }

    public void SaveGameData()
    {
        try
        {
            saveData.lastPlayed = DateTime.Now;

            // Синхронизируем монеты с игроком
            if (player != null)
            {
                saveData.totalCoins = player.GetTotalCoins();
            }

            string json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(saveFilePath, json);

            Debug.Log("✅ Game data saved: " + saveFilePath +
                     "\nCoins: " + saveData.totalCoins +
                     ", Dash Level: " + saveData.dashUpgradeLevel +
                     ", Skin: " + saveData.selectedSkin);
        }
        catch (Exception e)
        {
            Debug.LogError("❌ Failed to save game data: " + e.Message);
        }
    }

    bool CheckAndUpdateHighScore(int currentDistance)
    {
        if (currentDistance > saveData.highScore)
        {
            saveData.highScore = currentDistance;
            return true;
        }
        return false;
    }

    void UpdateGameStats(int distance, int coinsCollected)
    {
        saveData.totalRuns++;
        saveData.totalDistance += distance;
    }

    // === ПУБЛИЧНЫЕ МЕТОДЫ ДЛЯ ДОСТУПА К ДАННЫМ ===

    public int GetSavedCoins()
    {
        return saveData.totalCoins;
    }

    public void UpdateCoinSaveData(int coins)
    {
        saveData.totalCoins = coins;
        SaveGameData();
    }

    public void AddCoinsToSave(int amount)
    {
        saveData.totalCoins += amount;
        SaveGameData();
    }

    public bool SpendCoinsFromSave(int amount)
    {
        if (saveData.totalCoins >= amount)
        {
            saveData.totalCoins -= amount;
            SaveGameData();
            return true;
        }
        return false;
    }

    public PlayerSaveData GetSaveData()
    {
        return saveData;
    }

    public void UpdateSaveData(PlayerSaveData newData)
    {
        saveData = newData;
        SaveGameData();
    }

    // === КНОПКИ ===

    public void Quit()
    {
        SaveGameData();
        SceneManager.LoadScene("Menu");
    }

    public void Retry()
    {
        Debug.Log("Retry button pressed!");

        // Сбрасываем сессионные монеты игрока
        if (player != null)
        {
            player.ResetSessionCoins();
        }
        else
        {
            // Ищем игрока в сцене
            player = GameObject.Find("Player")?.GetComponent<Player>();
            if (player != null)
            {
                player.ResetSessionCoins();
            }
        }

        // Скрываем экран результатов
        if (results != null)
        {
            results.SetActive(false);
            resultsShown = false;
        }

        // Сбрасываем состояние UI
        ResetUIState();

        // Загружаем сцену заново
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void ResetUIState()
    {
        resultsShown = false;
        isInitialized = false;

        // Сбрасываем тексты
        if (distanceText != null) distanceText.text = "0";
        if (distanceTextTMP != null) distanceTextTMP.text = "0";

        if (coinText != null) coinText.text = "0";
        if (coinTextTMP != null) coinTextTMP.text = "0";

        // Скрываем результаты
        if (results != null)
        {
            results.SetActive(false);
        }

        Debug.Log("UI state reset for retry");
    }

    // === ОТЛАДКА ===

    [ContextMenu("Reset Save Data")]
    public void ResetSaveData()
    {
        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
        }
        saveData = new PlayerSaveData();
        Debug.Log("🗑️ Save data reset!");

        UpdateRecordDisplay();
    }

    [ContextMenu("Show Save Path")]
    public void ShowSavePath()
    {
        Debug.Log("Save folder: " + saveFolderPath);
        Debug.Log("Save file: " + saveFilePath);
        Debug.Log("File exists: " + File.Exists(saveFilePath));
    }

    [ContextMenu("Add 100 Test Coins")]
    public void AddTestCoins()
    {
        if (player != null)
        {
            player.AddCoins(100);
            UpdateCoinDisplay();
        }
        else
        {
            saveData.totalCoins += 100;
            SaveGameData();
            Debug.Log("Added 100 coins directly to save");
        }
    }

    [ContextMenu("Check UI Setup")]
    public void CheckUISetup()
    {
        string debugMessage = "UI Elements found:\n";
        debugMessage += "✓ Player: " + (player != null) + "\n";
        debugMessage += "✓ Distance: Legacy=" + (distanceText != null) + " TMP=" + (distanceTextTMP != null) + "\n";
        debugMessage += "✓ Coins: Legacy=" + (coinText != null) + " TMP=" + (coinTextTMP != null) + "\n";
        debugMessage += "✓ Record: Legacy=" + (recordText != null) + " TMP=" + (recordTextTMP != null) + "\n";
        debugMessage += "✓ Results GameObject: " + (results != null) + "\n";
        Debug.Log(debugMessage);
    }
}