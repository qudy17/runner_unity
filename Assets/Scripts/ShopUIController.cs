using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;

public class ShopUIController : MonoBehaviour
{
    [Header("UI References - ShiftCD")]
    public TMP_Text shiftCDTitle;
    public TMP_Text shiftCDDescription;
    public TMP_Text shiftCDPrice;
    public Button shiftCDBuyButton;
    public Text shiftCDBuyText;
    public Sprite maxVaueImg;

    [Header("UI References - Skins")]
    public Sprite usedButtonImg;
    public Sprite useButtonImg;
    public Sprite buyButtonImg;
    public Image punkPreview;
    public TMP_Text punkPrice;
    public Button punkBuyButton;
    public Text punkBuyText;
    public Image PunkUseInage;

    public Image cyborgPreview;
    public TMP_Text cyborgPrice;
    public Button cyborgBuyButton;
    public Text cyborgBuyText;

    public Image bikerPreview;
    public TMP_Text bikerPrice;
    public Button bikerBuyButton;
    public Text bikerBuyText;

    [Header("Skin Previews in Scene")]
    public GameObject punkSkinPreview;
    public GameObject cyborgSkinPreview;
    public GameObject bikerSkinPreview;

    [Header("Prices")]
    public int[] dashUpgradePrices = { 50, 100, 150, 200, 250 };
    public int skinPrice = 200;
    public int multiplierPriceAmount = 100;

    [Header("UI Elements")]
    public TMP_Text totalCoinsText;
    public Button backButton;
    public Button playButton;

    [Header("UI References - Multiplier")]
    public TMP_Text multiplierTitle;
    public TMP_Text multiplierDescription;
    public TMP_Text multiplierPrice;
    public Button multiplierBuyButton;
    public Text multiplierBuyText;

    private PlayerSaveData saveData;
    private string currentPreviewSkin = "Punk";

    [Header("Coins")]
    public GameObject ShiftCoins;
    public GameObject MultiplierCoins;
    public GameObject punkCoins;
    public GameObject cyborgCoins;
    public GameObject bikerCoins;




    // === ДОБАВЛЕНО: Собственная система сохранения ===
    private string saveFolderPath;
    private string saveFilePath;

    void Start()
    {
        Debug.Log("=== SHOP INITIALIZED ===");

        // Инициализируем собственную систему сохранения
        InitializeSaveSystem();

        // === АВТОПОИСК КНОПОК ===
        AutoFindButtons();

        // Загружаем данные
        LoadSaveData();
        UpdateAllUI();
        SetupButtons();
        ShowSkinPreview("Punk");
        UpdateMultiplierUI();

        Debug.Log("Shop ready. Coins: " + saveData.totalCoins);
    }

    // Добавь этот новый метод после Start()
    void AutoFindButtons()
    {
        // Ищем кнопку Back если не назначена
        if (backButton == null)
        {
            // Пробуем разные варианты имён
            string[] backButtonNames = { "BackButton", "Back", "ButtonBack", "ExitButton", "Exit", "CloseButton", "Close", "MenuButton" };

            foreach (string name in backButtonNames)
            {
                GameObject obj = GameObject.Find(name);
                if (obj != null)
                {
                    backButton = obj.GetComponent<Button>();
                    if (backButton != null)
                    {
                        Debug.Log("✅ Auto-found back button: " + name);
                        break;
                    }
                }
            }

            // Если всё ещё не нашли - ищем любую кнопку с нужным текстом
            if (backButton == null)
            {
                // ИСПРАВЛЕНО: новый метод вместо устаревшего
                Button[] allButtons = FindObjectsByType<Button>(FindObjectsSortMode.None);

                foreach (Button btn in allButtons)
                {
                    Text btnText = btn.GetComponentInChildren<Text>();
                    TMPro.TMP_Text btnTMP = btn.GetComponentInChildren<TMPro.TMP_Text>();

                    string buttonText = "";
                    if (btnText != null) buttonText = btnText.text.ToLower();
                    if (btnTMP != null) buttonText = btnTMP.text.ToLower();

                    if (buttonText.Contains("назад") || buttonText.Contains("back") ||
                        buttonText.Contains("выход") || buttonText.Contains("exit") ||
                        buttonText.Contains("меню") || buttonText.Contains("menu") ||
                        buttonText.Contains("закрыть") || buttonText.Contains("close"))
                    {
                        backButton = btn;
                        Debug.Log("✅ Auto-found back button by text: " + buttonText);
                        break;
                    }
                }
            }
        }

        // Показываем все кнопки в сцене для отладки
        if (backButton == null)
        {
            Debug.LogError("❌ Back button not found! Available buttons in scene:");

            // ИСПРАВЛЕНО: новый метод
            Button[] allButtons = FindObjectsByType<Button>(FindObjectsSortMode.None);

            foreach (Button btn in allButtons)
            {
                string btnName = btn.gameObject.name;
                Text btnText = btn.GetComponentInChildren<Text>();
                TMPro.TMP_Text btnTMP = btn.GetComponentInChildren<TMPro.TMP_Text>();

                string textContent = "";
                if (btnText != null) textContent = btnText.text;
                if (btnTMP != null) textContent = btnTMP.text;

                Debug.Log("   → Button: '" + btnName + "' | text: '" + textContent + "'");
            }
        }
    }

    // === ДОБАВЛЕНО: Инициализация системы сохранения ===
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

    // === ИСПРАВЛЕНО: Загрузка данных напрямую из файла ===
    void LoadSaveData()
    {
        // Сначала пробуем получить из UIController (если он есть)
        if (UIController.Instance != null)
        {
            saveData = UIController.Instance.GetSaveData();
            Debug.Log("✅ Loaded data from UIController");
        }
        // Если UIController нет - загружаем напрямую из файла
        else
        {
            LoadFromFile();
            Debug.Log("✅ Loaded data directly from file");
        }

        // Гарантируем что Punk разблокирован
        if (saveData.unlockedSkins == null)
            saveData.unlockedSkins = new List<string>();

        if (!saveData.unlockedSkins.Contains("Punk"))
            saveData.unlockedSkins.Add("Punk");

        if (string.IsNullOrEmpty(saveData.selectedSkin))
            saveData.selectedSkin = "Punk";
    }

    // === ДОБАВЛЕНО: Загрузка из файла ===
    void LoadFromFile()
    {
        if (File.Exists(saveFilePath))
        {
            try
            {
                string json = File.ReadAllText(saveFilePath);
                saveData = JsonUtility.FromJson<PlayerSaveData>(json);
                Debug.Log("✅ Save file loaded: " + saveFilePath);
            }
            catch (Exception e)
            {
                Debug.LogError("❌ Failed to load save: " + e.Message);
                saveData = new PlayerSaveData();
            }
        }
        else
        {
            saveData = new PlayerSaveData();
            Debug.Log("ℹ️ No save file, using defaults");
        }
    }

    // === ИСПРАВЛЕНО: Сохранение данных ===
    void SaveGameData()
    {
        // Пробуем сохранить через UIController
        if (UIController.Instance != null)
        {
            UIController.Instance.UpdateSaveData(saveData);
        }

        // Всегда сохраняем напрямую в файл (на всякий случай)
        SaveToFile();
    }

    // === ДОБАВЛЕНО: Сохранение в файл ===
    void SaveToFile()
    {
        try
        {
            string json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(saveFilePath, json);
            Debug.Log("✅ Saved to file: " + saveFilePath);
        }
        catch (Exception e)
        {
            Debug.LogError("❌ Failed to save: " + e.Message);
        }
    }

    // === ИСПРАВЛЕНО: Настройка кнопок с отладкой ===
    void SetupButtons()
    {
        // Кнопка назад
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners(); // Очищаем старые
            backButton.onClick.AddListener(BackToMenu);
            Debug.Log("✅ Back button connected");
        }
        else
        {
            Debug.LogError("❌ backButton is NULL! Assign it in Inspector!");
        }

        // Кнопка игры
        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(PlayGame);
            Debug.Log("✅ Play button connected");
        }


        // Кнопки предметов
        if (shiftCDBuyButton != null)
        {
            shiftCDBuyButton.onClick.RemoveAllListeners();
            shiftCDBuyButton.onClick.AddListener(BuyDashUpgrade);
        }

        if (multiplierBuyButton != null)
        {
            multiplierBuyButton.onClick.RemoveAllListeners();
            multiplierBuyButton.onClick.AddListener(BuyMultiplierUpgrade);
        }

        if (punkBuyButton != null)
        {
            punkBuyButton.onClick.RemoveAllListeners();
            punkBuyButton.onClick.AddListener(() => OnSkinButtonClick("Punk"));
        }

        if (cyborgBuyButton != null)
        {
            cyborgBuyButton.onClick.RemoveAllListeners();
            cyborgBuyButton.onClick.AddListener(() => OnSkinButtonClick("Cyborg"));
        }

        if (bikerBuyButton != null)
        {
            bikerBuyButton.onClick.RemoveAllListeners();
            bikerBuyButton.onClick.AddListener(() => OnSkinButtonClick("Biker"));
        }
    }

    // === ИСПРАВЛЕНО: Навигация с отладкой ===
    public void BackToMenu()
    {
        Debug.Log(">>> BackToMenu() CALLED <<<");

        // Сохраняем перед выходом
        SaveGameData();

        // Небольшая задержка для гарантии сохранения
        StartCoroutine(LoadMenuWithDelay());
    }

    IEnumerator LoadMenuWithDelay()
    {
        yield return new WaitForSeconds(0.1f);
        Debug.Log(">>> Loading Menu scene <<<");
        SceneManager.LoadScene("Menu");
    }

    public void PlayGame()
    {
        Debug.Log(">>> PlayGame() CALLED <<<");
        SaveGameData();
        StartCoroutine(LoadGameWithDelay());
    }

    IEnumerator LoadGameWithDelay()
    {
        yield return new WaitForSeconds(0.1f);
        SceneManager.LoadScene("MainScene");
    }

    // ========== ОСТАЛЬНЫЕ МЕТОДЫ (без изменений) ==========

    void UpdateAllUI()
    {
        UpdateCoinDisplay();
        UpdateDashUpgradeUI();
        UpdateSkinsUI();
        UpdateMultiplierUI();
    }

    void UpdateCoinDisplay()
    {
        if (totalCoinsText != null)
        {
            totalCoinsText.text = "" + saveData.totalCoins.ToString();
        }
    }

    void UpdateDashUpgradeUI()
    {
        if (shiftCDTitle != null)
        {
            shiftCDTitle.text = "Рывок";
        }

        if (shiftCDDescription != null)
        {
            float currentCooldown = 0.8f - (saveData.dashUpgradeLevel * 0.2f);
            shiftCDDescription.text = $"Позволяет использовать рывок чаще на 0.2с\nТекущая задержка: {currentCooldown:F1}с";
        }

        if (shiftCDPrice != null)
        {
            if (saveData.dashUpgradeLevel < 5)
            {
                shiftCDPrice.text = $"{dashUpgradePrices[saveData.dashUpgradeLevel]}";
            }
            else
            {
                shiftCDPrice.text = "";
            }
        }

        if (shiftCDBuyButton != null && maxVaueImg != null && buyButtonImg != null)
        {
            if (saveData.dashUpgradeLevel < 5)
            {
                shiftCDBuyButton.interactable = true;
                shiftCDBuyButton.image.sprite = buyButtonImg;

            }
            else
            {
                shiftCDBuyButton.interactable = false;
                shiftCDBuyButton.image.sprite = maxVaueImg;
                ShiftCoins.SetActive(false);
            }
        }
    }

    void UpdateMultiplierUI()
    {
        if (multiplierTitle != null)
        {
            multiplierTitle.text = "Множитель";
        }

        if (multiplierDescription != null)
        {
            float currentMultiplier = 1.0f + (saveData.scoreMultiplierLevel * 0.1f);
            float bonusPercentage = saveData.scoreMultiplierLevel * 10;
            multiplierDescription.text = $"Умножение текущего счета на +10% за уровень\nТекущий множитель: x{currentMultiplier:F1} (+{bonusPercentage}%)";
        }

        if (multiplierPrice != null && maxVaueImg != null && buyButtonImg != null)
        {
            if (saveData.scoreMultiplierLevel < 10)
            {
                multiplierPrice.text = $"{multiplierPriceAmount}";
            }
            else
            {
                multiplierPrice.text = "";
            }
        }

        if (multiplierBuyButton != null)
        {
            if (saveData.scoreMultiplierLevel < 10)
            {
                multiplierBuyButton.interactable = true;
                multiplierBuyButton.image.sprite = buyButtonImg;
                Debug.Log("Покупка");
            }
            else
            {
                multiplierBuyButton.interactable = false;
                multiplierBuyButton.image.sprite = maxVaueImg;
                MultiplierCoins.SetActive(false);
                Debug.Log("Достигнуто максимальное значение");
            }
        }
    }

    void BuyMultiplierUpgrade()
    {
        if (saveData.scoreMultiplierLevel >= 10)
        {
            Debug.Log("Максимум");
            return;
        }

        if (saveData.totalCoins >= multiplierPriceAmount)
        {
            saveData.totalCoins -= multiplierPriceAmount;
            saveData.scoreMultiplierLevel++;

            SaveGameData();
            UpdateAllUI();

            Debug.Log($"Куплен множитель! Уровень: {saveData.scoreMultiplierLevel}");
            StartCoroutine(ButtonBounceEffect(multiplierBuyButton.transform));
        }
        else
        {
            Debug.Log("Недостаточно монет!");
        }
    }

    void UpdateSkinsUI()
    {
        UpdateSkinUI("Punk", punkPrice, punkBuyButton, punkCoins);
        UpdateSkinUI("Cyborg", cyborgPrice, cyborgBuyButton, cyborgCoins);
        UpdateSkinUI("Biker", bikerPrice, bikerBuyButton, bikerCoins);
    }

    void UpdateSkinUI(string skinName, TMP_Text priceText, Button buyButton, GameObject coins)
    {
        bool isUnlocked = saveData.unlockedSkins.Contains(skinName);
        bool isSelected = saveData.selectedSkin == skinName;

        if (priceText != null)
        {
            priceText.text = isUnlocked ? "" : $"{skinPrice}";
        }

        if (buyButton != null && buyButtonImg != null)
        {
            buyButton.interactable = true;

            if (!isUnlocked)
            {
                buyButton.image.sprite = buyButtonImg;
                coins.SetActive(true);
            }           
            else if (isSelected)
            { 
                buyButton.image.sprite = usedButtonImg;
                coins.SetActive(false);
            }
            else
            {
                buyButton.image.sprite = useButtonImg;
                coins.SetActive(false);
            }
        }
    }

    void BuyDashUpgrade()
    {
        if (saveData.dashUpgradeLevel >= 5)
        {
            Debug.Log("Максимум");
            return;
        }

        int price = dashUpgradePrices[saveData.dashUpgradeLevel];

        if (saveData.totalCoins >= price)
        {
            saveData.totalCoins -= price;
            saveData.dashUpgradeLevel++;

            SaveGameData();
            UpdateAllUI();

            Debug.Log($"Куплено улучшение рывка! Уровень: {saveData.dashUpgradeLevel}");
            StartCoroutine(ButtonBounceEffect(shiftCDBuyButton.transform));
        }
        else
        {
            Debug.Log("Недостаточно монет!");
        }
    }

    void OnSkinButtonClick(string skinName)
    {
        bool isUnlocked = saveData.unlockedSkins.Contains(skinName);

        if (isUnlocked)
        {
            saveData.selectedSkin = skinName;
            SaveGameData();
            UpdateAllUI();
            ShowSkinPreview(skinName);
            Debug.Log($"Выбран скин: {skinName}");
        }
        else
        {
            if (saveData.totalCoins >= skinPrice)
            {
                saveData.totalCoins -= skinPrice;
                saveData.unlockedSkins.Add(skinName);
                saveData.selectedSkin = skinName;

                SaveGameData();
                UpdateAllUI();
                ShowSkinPreview(skinName);

                Debug.Log($"Куплен скин: {skinName}");

                Button skinButton = GetSkinButton(skinName);
                if (skinButton != null)
                    StartCoroutine(ButtonBounceEffect(skinButton.transform));
            }
            else
            {
                Debug.Log("Недостаточно монет для покупки скина!");
            }
        }
    }

    Button GetSkinButton(string skinName)
    {
        switch (skinName)
        {
            case "Punk": return punkBuyButton;
            case "Cyborg": return cyborgBuyButton;
            case "Biker": return bikerBuyButton;
            default: return null;
        }
    }

    void ShowSkinPreview(string skinName)
    {
        if (punkSkinPreview != null) punkSkinPreview.SetActive(false);
        if (cyborgSkinPreview != null) cyborgSkinPreview.SetActive(false);
        if (bikerSkinPreview != null) bikerSkinPreview.SetActive(false);

        GameObject previewObject = null;
        switch (skinName)
        {
            case "Punk": previewObject = punkSkinPreview; break;
            case "Cyborg": previewObject = cyborgSkinPreview; break;
            case "Biker": previewObject = bikerSkinPreview; break;
        }

        if (previewObject != null)
        {
            previewObject.SetActive(true);
            currentPreviewSkin = skinName;

            Animator animator = previewObject.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetBool("IsRunning", true);
                animator.SetBool("IsGrounded", true);
                animator.Play("Run");
            }
        }
    }

    IEnumerator ButtonBounceEffect(Transform buttonTransform)
    {
        Vector3 originalScale = buttonTransform.localScale;
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float scale = 1f + 0.3f * Mathf.Sin(t * Mathf.PI);
            buttonTransform.localScale = originalScale * scale;
            elapsed += Time.deltaTime;
            yield return null;
        }

        buttonTransform.localScale = originalScale;
    }

    // === ОТЛАДКА ===
    [ContextMenu("Add 1000 Coins")]
    void AddTestCoins()
    {
        saveData.totalCoins += 1000;
        SaveGameData();
        UpdateAllUI();
        Debug.Log("Added 1000 coins!");
    }

    [ContextMenu("Reset Shop Data")]
    void ResetShopData()
    {
        saveData.scoreMultiplierLevel = 0;
        saveData.dashUpgradeLevel = 0;
        saveData.selectedSkin = "Punk";
        saveData.unlockedSkins = new List<string> { "Punk" };
        SaveGameData();
        UpdateAllUI();
        ShowSkinPreview("Punk");
        Debug.Log("Shop data reset!");
    }

    [ContextMenu("Test Back Button")]
    void TestBackButton()
    {
        Debug.Log("=== TESTING BACK BUTTON ===");
        Debug.Log("backButton: " + (backButton != null ? backButton.name : "NULL"));
        if (backButton != null)
        {
            Debug.Log("  - interactable: " + backButton.interactable);
            Debug.Log("  - gameObject active: " + backButton.gameObject.activeSelf);
            Debug.Log("  - listeners count: " + backButton.onClick.GetPersistentEventCount());
        }
        BackToMenu();
    }
}