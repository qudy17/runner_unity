using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIControllerTitle : MonoBehaviour
{
    void Start()
    {

    }

    void Update()
    {

    }

    public void play()
    {
        SceneManager.LoadScene("MainScene");
    }

    public void shop()
    {
        SceneManager.LoadScene("Shop");
    }

    public void menu()
    {
        SceneManager.LoadScene("Menu");
    }
}
