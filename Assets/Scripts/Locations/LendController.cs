using UnityEngine;

public class LendController : MonoBehaviour
{
    [Header("Настройки земли")]
    [Tooltip("3 куска земли, назначенные в инспекторе (не важно, в каком порядке)")]
    public Transform[] groundSegments;

    [Tooltip("Длина каждого куска земли (по оси Z)")]
    public float segmentLength = 100f;

    [Tooltip("Ссылка на трансформ игрока или лодки")]
    public Transform player;

    // Индекс куска земли, который следующим будет перемещён
    private int currentSegmentIndex = 0;

    void Update()
    {
        // Если игрок пересек конец текущего сегмента земли,
        // то есть его позиция по Z больше чем начало сегмента + длина,
        // перемещаем этот сегмент в конец.
        if (player.position.z > groundSegments[currentSegmentIndex].position.z + segmentLength)
        {
            // Находим самый передний кусок земли (с максимальной координатой Z)
            float maxZ = groundSegments[0].position.z;
            for (int i = 1; i < groundSegments.Length; i++)
            {
                if (groundSegments[i].position.z > maxZ)
                    maxZ = groundSegments[i].position.z;
            }

            // Перемещаем выбранный сегмент за самый передний кусок
            Vector3 newPos = groundSegments[currentSegmentIndex].position;
            newPos.z = maxZ + segmentLength;
            groundSegments[currentSegmentIndex].position = newPos;

            // Обновляем индекс для следующего перемещения (циклически)
            currentSegmentIndex = (currentSegmentIndex + 1) % groundSegments.Length;
        }
    }
}
