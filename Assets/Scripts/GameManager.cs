using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static bool isGameActive;
    public GameObject titleScreen;
    public GameObject gameOverScreen;
    public GameObject player;
    void Start()
    {
        isGameActive = false;
        titleScreen.gameObject.SetActive(true);
        gameOverScreen.gameObject.SetActive(false);
        player.gameObject.SetActive(false);
    }

    public void GameOver() 
    {
        isGameActive = false;
        gameOverScreen.gameObject.SetActive(true);
        player.gameObject.SetActive(false);
    }

    public void StartGame() 
    {
        titleScreen.gameObject.SetActive(false);
        isGameActive = true;
        player.gameObject.SetActive(true);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene("MainScene");
    }
}
