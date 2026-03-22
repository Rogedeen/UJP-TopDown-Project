using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static bool isGameActive;
    public GameObject titleScreen;
    public GameObject gameOverScreen;
    public GameObject winScreen;
    public GameObject ingameScreen; // HUD — oyun sırasında görünür
    public GameObject tutorialScreen; // Yeni: Kontrolleri gösteren panel
    public GameObject retryObject;
    public GameObject player;
    public Animator playerControllerAnim;

    void Update()
    {
        // Oyun herhangi bir sebepten ötürü durdurulduğunda (Ölüm, Kart seçimi vs.)
        // Tüm dünyadaki sesleri (AudioListener) otomatik olarak dondurur.
        AudioListener.pause = (Time.timeScale == 0f);
    }

    void Start()
    {
        Application.targetFrameRate = 60;
        Time.timeScale = 1;
        isGameActive = false;
        titleScreen.SetActive(true);
        gameOverScreen.SetActive(false);
        winScreen.SetActive(false);
        ingameScreen.SetActive(false);
        
        if (tutorialScreen != null)
            tutorialScreen.SetActive(false);

        player.SetActive(false);
        retryObject.SetActive(false);
    }

    public void StartGame()
    {
        titleScreen.SetActive(false);
        ingameScreen.SetActive(true);
        isGameActive = true;
        player.SetActive(true);
        Time.timeScale = 1;

        // Tutorial ekranını göster ve 5 saniye sonra gizle
        if (tutorialScreen != null)
        {
            tutorialScreen.SetActive(true);
            StartCoroutine(HideTutorialRoutine());
        }
    }

    private IEnumerator HideTutorialRoutine()
    {
        yield return new WaitForSeconds(15f);
        if (tutorialScreen != null)
        {
            tutorialScreen.SetActive(false);
        }
    }

    public void GameOver()
    {
        isGameActive = false;
        ingameScreen.SetActive(false);
        gameOverScreen.SetActive(true);
        retryObject.SetActive(true);
        Time.timeScale = 0;
    }

    public IEnumerator WinGame()
    {
        isGameActive = false;
        ingameScreen.SetActive(false);
        playerControllerAnim.SetTrigger("winGame_t");
        KillAllEnemies();
        yield return new WaitForSecondsRealtime(2);
        winScreen.SetActive(true);
        retryObject.SetActive(true);
    }

    private void KillAllEnemies()
    {
        EnemyBase[] remainingEnemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        foreach (EnemyBase enemy in remainingEnemies)
        {
            enemy.ForceKill();
        }

    }

    public void RestartGame()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}