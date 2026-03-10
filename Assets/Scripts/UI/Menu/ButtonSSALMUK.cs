using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonSSALMUK : MonoBehaviour
{
    public void Quit()
    {
        Debug.Log("게임 종료 요청됨");

        // 에디터에서 플레이 중이라면 정지
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // 빌드된 실행 파일에서 종료
        Application.Quit();
#endif
    }


    public void StartGame()
    {
        SceneManager.LoadScene("Game");
    }

    public void HowTo()
    {

    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Quit();
        }
    }

}
