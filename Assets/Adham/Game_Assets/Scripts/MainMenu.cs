using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject startupCanvas;
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject guidanceMenu;

    public void StartButtonClicked()
    {
       startupCanvas.SetActive(false);
    }

    public void GuideButtonClicked()
    {
        mainMenu.SetActive(false);
        guidanceMenu.SetActive(true);
    }

    public void BackButtonClicked()
    {
        guidanceMenu.SetActive(false);
        mainMenu.SetActive(true);
    }
}
