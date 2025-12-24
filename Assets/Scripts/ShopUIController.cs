using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class ShopUIController : MonoBehaviour
{
    [Header("UI References - ShiftCD")]
    public TMP_Text shiftCDTitle;
    public TMP_Text shiftCDDescription;
    public TMP_Text shiftCDPrice;
    public Button shiftCDBuyButton;
    public Text shiftCDBuyText; // Обычный Text

    [Header("UI References - Skins")]
    public Image punkPreview;
    public TMP_Text punkPrice;
    public Button punkBuyButton;
    public Text punkBuyText; // Обычный Text

    public Image cyborgPreview;
    public TMP_Text cyborgPrice;
    public Button cyborgBuyButton;
    public Text cyborgBuyText; // Обычный Text

    public Image bikerPreview;
    public TMP_Text bikerPrice;
    public Button bikerBuyButton;
    public Text bikerBuyText; // Обычный Text

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

    private PlayerSaveData saveData;
    private string currentPreviewSkin = "Punk";

    [Header("UI References - Multiplier")]
    public TMP_Text multiplierTitle;
    public TMP_Text multiplierDescription;
    public TMP_Text multiplierPrice;
    public Button multiplierBuyButton;
    public Text multiplierBuyText;

    void Start()
    {
        // Загружаем данные
        LoadSaveData();
        UpdateAllUI();

        SetupButtons();

        ShowSkinPreview("Punk");

        UpdateMultiplierUI();

        Debug.Log("Shop initialized. Coins: " + saveData.totalCoins +
                  ", Dash Level: " + saveData.dashUpgradeLevel +
                  ", Selected Skin: " + saveData.selectedSkin);
    }

    void LoadSaveData()
    {
        if (UIController.Instance != null)
        {
            saveData = UIController.Instance.GetSaveData();

            // Гарантируем что Punk разблокирован
            if (saveData.unlockedSkins == null)
                saveData.unlockedSkins = new List<string>();

            if (!saveData.unlockedSkins.Contains("Punk"))
                saveData.unlockedSkins.Add("Punk");

            if (string.IsNullOrEmpty(saveData.selectedSkin))
                saveData.selectedSkin = "Punk";
        }
        else
        {
            saveData = new PlayerSaveData();
            Debug.LogError("UIController not found! Using default save data.");
        }
    }

    void SaveGameData()
    {
        if (UIController.Instance != null)
        {
            UIController.Instance.UpdateSaveData(saveData);
        }
    }

    void SetupButtons()
    {
        // Кнопка назад
        if (backButton != null)
        {
            backButton.onClick.AddListener(() => BackToMenu());
        }

        // Кнопка игры
        if (playButton != null)
        {
            playButton.onClick.AddListener(() => PlayGame());
        }

        // Кнопки предметов
        if (shiftCDBuyButton != null)
        {
            shiftCDBuyButton.onClick.AddListener(() => BuyDashUpgrade());
        }

        // Кнопка множителя
        if (multiplierBuyButton != null)
        {
            multiplierBuyButton.onClick.AddListener(() => BuyMultiplierUpgrade());
        }

        if (punkBuyButton != null)
        {
            punkBuyButton.onClick.AddListener(() => OnSkinButtonClick("Punk"));
        }

        if (cyborgBuyButton != null)
        {
            cyborgBuyButton.onClick.AddListener(() => OnSkinButtonClick("Cyborg"));
        }

        if (bikerBuyButton != null)
        {
            bikerBuyButton.onClick.AddListener(() => OnSkinButtonClick("Biker"));
        }
    }

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

        if (shiftCDBuyButton != null && shiftCDBuyText != null)
        {
            if (saveData.dashUpgradeLevel < 5)
            {
                shiftCDBuyButton.interactable = true;
                shiftCDBuyText.text = $"КУПИТЬ\n({saveData.dashUpgradeLevel + 1}/5)";
            }
            else
            {
                shiftCDBuyButton.interactable = false;
                shiftCDBuyText.text = "МАКСИМУМ";
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

        if (multiplierPrice != null)
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

        if (multiplierBuyButton != null && multiplierBuyText != null)
        {
            if (saveData.scoreMultiplierLevel < 10)
            {
                multiplierBuyButton.interactable = true;
                multiplierBuyText.text = $"КУПИТЬ\n({saveData.scoreMultiplierLevel + 1}/10)";
            }
            else
            {
                multiplierBuyButton.interactable = false;
                multiplierBuyText.text = "МАКСИМУМ";
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

            // Эффект
            StartCoroutine(ButtonBounceEffect(multiplierBuyButton.transform));
        }
        else
        {
            Debug.Log("Недостаточно монет!");
        }
    }

    void UpdateSkinsUI()
    {
        // Punk (всегда разблокирован)
        UpdateSkinUI("Punk", punkPrice, punkBuyButton, punkBuyText);

        // Cyborg
        UpdateSkinUI("Cyborg", cyborgPrice, cyborgBuyButton, cyborgBuyText);

        // Biker
        UpdateSkinUI("Biker", bikerPrice, bikerBuyButton, bikerBuyText);
    }

    void UpdateSkinUI(string skinName, TMP_Text priceText, Button buyButton, Text buyText)
    {
        bool isUnlocked = saveData.unlockedSkins.Contains(skinName);
        bool isSelected = saveData.selectedSkin == skinName;

        if (priceText != null)
        {
            if (isUnlocked)
            {
                priceText.text = "";
            }
            else
            {
                priceText.text = $"{skinPrice}";
            }
        }

        if (buyButton != null && buyText != null)
        {
            buyButton.interactable = true;

            if (!isUnlocked)
            {
                buyText.text = "КУПИТЬ";
            }
            else if (isSelected)
            {
                buyText.text = "ИСПОЛЬЗУЕТСЯ";
            }
            else
            {
                buyText.text = "ВЫБРАТЬ";
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

            // Эффект
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
            // Скин уже разблокирован - выбираем его
            saveData.selectedSkin = skinName;
            SaveGameData();
            UpdateAllUI();

            // Показываем превью
            ShowSkinPreview(skinName);

            Debug.Log($"Выбран скин: {skinName}");
        }
        else
        {
            // Пытаемся купить скин
            if (saveData.totalCoins >= skinPrice)
            {
                saveData.totalCoins -= skinPrice;
                saveData.unlockedSkins.Add(skinName);
                saveData.selectedSkin = skinName;

                SaveGameData();
                UpdateAllUI();

                // Показываем превью
                ShowSkinPreview(skinName);

                Debug.Log($"Куплен скин: {skinName}");

                // Эффект
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

    Text GetSkinText(string skinName)
    {
        switch (skinName)
        {
            case "Punk": return punkBuyText;
            case "Cyborg": return cyborgBuyText;
            case "Biker": return bikerBuyText;
            default: return null;
        }
    }

    void ShowSkinPreview(string skinName)
    {
        // Скрываем все превью
        if (punkSkinPreview != null) punkSkinPreview.SetActive(false);
        if (cyborgSkinPreview != null) cyborgSkinPreview.SetActive(false);
        if (bikerSkinPreview != null) bikerSkinPreview.SetActive(false);

        // Показываем выбранное превью
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

            // Запускаем анимацию
            Animator animator = previewObject.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetBool("IsRunning", true);
                animator.SetBool("IsGrounded", true);
                animator.Play("Run");
            }

            // Начинаем цикл анимации
            StopAllCoroutines();
            StartCoroutine(PreviewAnimationCycle(previewObject));
        }
    }

    IEnumerator PreviewAnimationCycle(GameObject preview)
    {
        Animator animator = preview.GetComponent<Animator>();
        if (animator == null) yield break;

        while (preview.activeSelf)
        {
            // Бег - 3 секунды
            animator.SetBool("IsRunning", true);
            animator.SetBool("IsJumping", false);
            yield return new WaitForSeconds(3f);

            // Прыжок
            animator.SetBool("IsJumping", true);
            animator.SetBool("IsRunning", false);
            yield return new WaitForSeconds(1f);

            // Падение
            animator.SetBool("IsJumping", false);
            yield return new WaitForSeconds(0.5f);

            // Снова бег
            animator.SetBool("IsRunning", true);
            yield return new WaitForSeconds(1f);
        }
    }

    // Анимационные эффекты
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

    // Навигация
    public void BackToMenu()
    {
        Debug.Log("BackToMenu called from inspector");
        SceneManager.LoadScene("Menu");
    }

    public void PlayGame()
    {
        Debug.Log("PlayGame called from inspector");
        SceneManager.LoadScene("MainScene");
    }

    // Отладка
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

    [ContextMenu("Unlock Everything")]
    void UnlockEverything()
    {
        saveData.scoreMultiplierLevel = 10;
        saveData.dashUpgradeLevel = 5;
        saveData.unlockedSkins = new List<string> { "Punk", "Cyborg", "Biker" };
        saveData.selectedSkin = "Biker";
        SaveGameData();
        UpdateAllUI();
        ShowSkinPreview("Biker");
        Debug.Log("Everything unlocked!");
    }
}