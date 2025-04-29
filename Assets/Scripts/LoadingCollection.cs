using UnityEngine;

public class LoadingCollection : MonoBehaviour
{
    private static LoadingCollection _instance;
    public static LoadingCollection Instance => _instance;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
