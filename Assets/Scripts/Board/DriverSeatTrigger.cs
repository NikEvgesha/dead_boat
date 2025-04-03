using UnityEngine;

public class DriverSeatTrigger : MonoBehaviour
{
    [Header("Настройки входа в режим вождения")]
    [Tooltip("Ссылка на TrainController, отвечающий за управление поездом")]
    public BoardController trainController;

    [Tooltip("Transform сиденья водителя, куда будет телепортироваться игрок")]
    public Transform driverSeatTransform;

    [Tooltip("Игровой объект игрока (его модель)")]
    public GameObject playerCharacter;

    [Tooltip("Имя компонента, отвечающего за стандартное движение игрока (например, PlayerMovement)")]
    public string playerMovementComponentName = "PlayerInput";

    // Флаг, показывающий, находится ли игрок в режиме вождения
    private bool isDriving = false;

    // Ссылка на компонент PlayerInput для отслеживания прыжка
    private PlayerInput playerInput;
    private bool _endGame; 

    // Сохраняем исходную позицию и поворот игрока для возврата при выходе
   // private Vector3 originalPlayerPosition;
    private Quaternion originalPlayerRotation;

    private void Start()
    {
        playerCharacter = GameManager.Instance.Player.gameObject;
        // Получаем ссылку на компонент PlayerInput у игрока
        trainController.EndGame += EndGame;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Если объект, входящий в триггер, является игроком, и он ещё не в режиме вождения
        if (!isDriving && other.gameObject == playerCharacter && !_endGame)
        {
            EnterDrivingMode();
        }
    }
    private void EndGame()
    {
        _endGame = true;
    }
    void EnterDrivingMode()
    {
        // Сохраняем исходную позицию и поворот игрока (на случай возврата)
        //originalPlayerPosition = playerCharacter.transform.position;
        //originalPlayerRotation = playerCharacter.transform.rotation;

        // Отключаем компонент обычного движения, если он есть
        if (PlayerInput.Instance != null)
        {
            PlayerInput.Instance.SitTrain(true);
            //movementComponent.enabled = false;

        }

        // Телепортируем игрока к сиденью водителя и прикрепляем его к нему
        playerCharacter.transform.position = driverSeatTransform.position;
        playerCharacter.transform.rotation = driverSeatTransform.rotation;
        playerCharacter.transform.SetParent(driverSeatTransform);

        // Активируем управление поездом
        trainController.playerOnSeat = true;
        isDriving = true;
    }

    private void Update()
    {
        // Если игрок в режиме вождения, проверяем срабатывание прыжка через PlayerInput
        if (isDriving && PlayerInput.Instance != null && PlayerInput.Instance.JumpTriggered)
        {
            ExitDrivingMode();
        }
    }

    void ExitDrivingMode()
    {
        // Включаем обратно компонент обычного движения, если он был отключён
        if (PlayerInput.Instance != null)
        {
            PlayerInput.Instance.SitTrain(false);
        }
        // Отвязываем игрока от сиденья
        playerCharacter.transform.SetParent(trainController.transform);
        // Перемещаем игрока в безопасную позицию рядом с креслом (сдвиг вправо на 2 единицы, можно изменить)
        //playerCharacter.transform.position = driverSeatTransform.position + driverSeatTransform.right * 2f;
        //playerCharacter.transform.rotation = originalPlayerRotation;

        // Деактивируем управление поездом
        trainController.playerOnSeat = false;
        isDriving = false;
    }
}
