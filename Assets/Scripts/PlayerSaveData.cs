using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerSaveData
{
    // Базовые данные
    public int highScore = 0;
    public int totalCoins = 0;
    public int totalRuns = 0;
    public int totalDistance = 0;
    public DateTime lastPlayed;

    // Множитель очков
    public int scoreMultiplierLevel = 0; // 0-10 уровней

    // Данные магазина
    public string selectedSkin = "Punk"; // Punk доступен по умолчанию
    public int dashUpgradeLevel = 0; // 0-5 уровней
    public List<string> unlockedSkins = new List<string>(); // Разблокированные скины

    // Поля для совместимости со старым сохранением
    public bool doubleJumpUnlocked = false;
    public bool speedBoostUnlocked = false;

    public PlayerSaveData()
    {
        // По умолчанию Punk разблокирован
        unlockedSkins = new List<string> { "Punk" };
        selectedSkin = "Punk";
        lastPlayed = DateTime.Now;
        scoreMultiplierLevel = 0;
    }
}