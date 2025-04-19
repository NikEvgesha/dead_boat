using UnityEngine;

public class FuelItem : MonoBehaviour
{
    [Tooltip("Количество топлива, которое даёт этот объект")]
    public float fuelValue = 10f;
    public bool InFire = false;
}
