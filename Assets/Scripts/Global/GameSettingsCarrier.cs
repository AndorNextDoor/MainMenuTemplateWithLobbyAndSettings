using UnityEngine;

public class GameSettingsCarrier : MonoBehaviour
{
    public static GameSettingsCarrier Instance { get; private set; }

    public int ExpectedPlayers { get; set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
