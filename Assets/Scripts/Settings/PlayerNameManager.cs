using TMPro;
using UnityEngine;

public class PlayerNameManager : MonoBehaviour
{
    [SerializeField] private GameObject playerNameSetMenu;
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private TextMeshProUGUI warningText;

    private void Awake()
    {
        if (PlayerPrefs.HasKey("PlayerName"))
        {
            return;
        }
        else
        {
            playerNameSetMenu.SetActive(true);
        }
    }

    public void CheckPlayerName()
    {
        warningText.text = "";

        if (!PlayerPrefs.HasKey("PlayerName"))
        {
            warningText.text = "Enter your name already";
            return;
        }

        string _name = PlayerPrefs.GetString("PlayerName");

        if(_name.Length <= 2)
        {
            warningText.text = "The name has to be at least 3 characters long";
            return;
        }

        playerNameSetMenu.SetActive(false);
        mainMenu.SetActive(true);
    }
}
