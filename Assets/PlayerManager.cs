using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    private static PlayerManager _instance;
    public static PlayerManager Instance { get { return _instance; } }


    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning("PlayerManager уже существует! Удаляем дубликат.");
            Destroy(gameObject);
        }
    }
}
