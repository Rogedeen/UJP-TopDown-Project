using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static bool isGameActive;
    public GameObject titleScreen;
    public GameObject gameOverScreen;
    public GameObject winScreen; // Yeni: Kazanma ekranı
    public GameObject retryObject;
    public GameObject player;

    void Start()
    {
        Application.targetFrameRate = 60;
        Time.timeScale = 1;
        isGameActive = false;
        titleScreen.SetActive(true);
        gameOverScreen.SetActive(false);
        winScreen.SetActive(false);
        player.SetActive(false);
        retryObject.SetActive(false);
    }

    public void StartGame()
    {
        titleScreen.SetActive(false);
        isGameActive = true;
        player.SetActive(true);
        Time.timeScale = 1;
    }

    public void GameOver()
    {
        isGameActive = false;
        gameOverScreen.SetActive(true);
        retryObject.SetActive(true);
        Time.timeScale = 0;
    }

    public void WinGame()
    {
        isGameActive = false;
        winScreen.SetActive(true);
        retryObject.SetActive(true);
        Time.timeScale = 0; 
    }

    public void RestartGame()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}