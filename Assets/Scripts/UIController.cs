using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIController : MonoBehaviour
{
    Player player;
    Text distanceText;

    GameObject results;
    Text finalDistanceText;

    private void Awake()
    {
        player = GameObject.Find("Player").GetComponent<Player>();
        distanceText = GameObject.Find("ScoreText").GetComponent<Text>();
        results = GameObject.Find("Results");
        finalDistanceText = GameObject.Find("FinalDistance").GetComponent<Text>();

        results.SetActive(false);
    }

    void Start()
    {

    }

    void Update()
    {
        int distance = Mathf.FloorToInt(player.distance);
        distanceText.text = distance.ToString();

        if (player.isDead)
        {
            results.SetActive(true);
            finalDistanceText.text = distance.ToString();
        }
    }

    public void Quit()
    {
        SceneManager.LoadScene("Menu");
    }

    public void Retry()
    {
        SceneManager.LoadScene("MainScene");
    }
}
