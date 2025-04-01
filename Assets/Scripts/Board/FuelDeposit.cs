using UnityEngine;

public class FuelDeposit : MonoBehaviour
{
    [Tooltip("Ссылка на TrainController, куда будет добавляться топливо")]
    public BoardController trainController;

    private void OnTriggerEnter(Collider other)
    {
        FuelItem fuelItem = other.GetComponent<FuelItem>();
        if (fuelItem != null)
        {
            trainController.AddFuel(fuelItem.fuelValue);
            Debug.Log("Добавлено топлива: " + fuelItem.fuelValue);
            // Удаляем объект после его использования
            Destroy(other.gameObject);
        }
    }
}
