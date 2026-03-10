using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class Tutoial : MonoBehaviour
{
    [Header("UI")]
    public GameObject tutorialPanel;   // 전체 패널
    public Image centerImage;          // 중앙 이미지

    [Header("Sprites")]
    public List<Sprite> tutorialSprites = new List<Sprite>();

    int currentIndex = 0;

    // HowTo 버튼 클릭 시
    public void OpenTutorial()
    {
        if (tutorialSprites.Count == 0) return;

        tutorialPanel.SetActive(true);
        currentIndex = 0;
        UpdateImage();
    }

    // X 버튼 같은 걸로 닫기
    public void CloseTutorial()
    {
        tutorialPanel.SetActive(false);
    }

    // ▶ 버튼 (오른쪽)
    public void NextTutorial()
    {
        if (tutorialSprites.Count == 0) return;

        currentIndex++;

        if (currentIndex >= tutorialSprites.Count)
            currentIndex = tutorialSprites.Count - 1;

        UpdateImage();
    }

    // ◀ 버튼 (왼쪽)
    public void PrevTutorial()
    {
        if (tutorialSprites.Count == 0) return;

        currentIndex--;

        if (currentIndex < 0)
            currentIndex = 0;

        UpdateImage();
    }

    // 중앙 이미지 갱신
    void UpdateImage()
    {
        centerImage.sprite = tutorialSprites[currentIndex];
    }
}
