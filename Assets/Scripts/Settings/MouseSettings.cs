using UnityEngine;
using UnityEngine.UI;

public class MouseSettings : MonoBehaviour
{
    public static MouseSettings Instance { get; private set; }

    [SerializeField] private Slider mouseSpeedSlider;
    [SerializeField] private float defaultMouseSpeed = 100f;

    public float mouseSpeed { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        LoadMouseSensitivity();
    }

    public void SetMouseSpeed(float value)
    {
        mouseSpeed = value;
        PlayerPrefs.SetFloat("MouseSensitivity", value);
        PlayerPrefs.Save();
    }

    private void LoadMouseSensitivity()
    {
        if (PlayerPrefs.HasKey("MouseSensitivity"))
        {
            mouseSpeed = PlayerPrefs.GetFloat("MouseSensitivity");
        }
        else
        {
            mouseSpeed = defaultMouseSpeed;
        }
        mouseSpeedSlider.value = mouseSpeed;
    }
}
