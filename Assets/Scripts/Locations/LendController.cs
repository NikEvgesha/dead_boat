using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEditor.Search;

public class LendController : MonoBehaviour
{
    [System.Serializable]
    public class Location
    {
        [Tooltip("Куски земли для этой локации")]
        public Transform[] groundSegments;

        [Tooltip("Флаг: является ли эта локация последней (после неё ничего не будет)")]
        public bool isFinalLocation;

        [HideInInspector] public LinkedList<Transform> segmentsQueue;
        [Tooltip("Расстояние до смены локаци (если предпоследняя то будет до конца)")]
        public float totalLength;
    }

    [Header("Настройки локаций")]
    public Location[] locations;

    [Tooltip("Длина каждого куска земли (по оси Z)")]
    public float segmentLength = 100f;

    [Tooltip("Ссылка на трансформ игрока или лодки")]
    public PlayerMovement _player;

    private int currentLocationIndex = 0;
    private List<float> switchThresholds = new List<float>();
    private float lastEndZ = 0f;
    private bool isStart; 

    private void Awake()
    {
        if (_player == null)
            _player = FindObjectOfType<PlayerMovement>();
        if (_player == null)
            Debug.LogError("Player transform not found");
    }

    private void Start()
    {
        float cumulative = 0f;

        for (int i = 0; i < locations.Length; i++)
        {
            var loc = locations[i];
            //loc.totalLength = loc.groundSegments.Length * segmentLength;
            if (i == locations.Length - 2)
            {
                loc.totalLength = GameManager.Instance.PlayDistance - (loc.groundSegments.Count()-1) * (segmentLength);
            }
            cumulative += loc.totalLength;
            switchThresholds.Add(cumulative);

            // Инициализируем очередь и активируем только первую локацию
            loc.segmentsQueue = new LinkedList<Transform>(loc.groundSegments);
            bool active = (i == 0);
            foreach (var seg in loc.groundSegments)
                seg.gameObject.SetActive(active);

            // Позиционируем сегменты подряд, начиная с 0 или предыдущего порога
            float startZ = (i == 0) ? 0f : switchThresholds[i - 1];
            for (int j = 0; j < loc.groundSegments.Length; j++)
            {
                var seg = loc.groundSegments[j];
                var pos = seg.position;
                pos.z = startZ + j * segmentLength;
                seg.position = pos;
            }

            // После первой локации устанавливаем lastEndZ
            if (i == 0)
                lastEndZ = switchThresholds[0] - segmentLength;

            StartCoroutine(StartSettings());
            //isStart = true;
        }
    }
    private IEnumerator StartSettings()
    {
        if (FixCoordinate.Instance.PlayerAddPos > 0)
        {
            float fantomPos = _player.zPositionFix - segmentLength * locations[0].groundSegments.Count();
            while (fantomPos < _player.zPositionFix)
            {
                Movelocation(fantomPos);
                fantomPos += Time.deltaTime * ((segmentLength * locations[0].groundSegments.Count()) / 1.2f);
                //Debug.Log(fantomPos);
                yield return null;
            }
        }
        isStart = true;
    }

    private void Update()
    {
        if (!isStart)
            return;
        Movelocation();
    }
    private void Movelocation(float playerZ = 0)
    {
        if (playerZ == 0)
            playerZ = _player.zPositionFix;

        // Переход вперёд
        if (currentLocationIndex < locations.Length - 1 && playerZ >= switchThresholds[currentLocationIndex])
        {
            SwitchLocation(currentLocationIndex + 1);
        }
        // Переход назад
        else if (currentLocationIndex > 0 && playerZ < switchThresholds[currentLocationIndex - 1])
        {
            SwitchLocation(currentLocationIndex - 1);
        }

        var activeLoc = locations[currentLocationIndex];
        var queue = activeLoc.segmentsQueue;

        if (!activeLoc.isFinalLocation)
        {
            // Вперёд
            while (playerZ - FixCoordinate.Instance.PlayerAddPos > queue.First.Value.position.z + segmentLength + 100)
            {
                var seg = queue.First.Value;
                queue.RemoveFirst();
                float newZ = queue.Last.Value.position.z + segmentLength;
                var p = seg.position; p.z = newZ; seg.position = p;
                queue.AddLast(seg);
                lastEndZ = newZ;
            }
            // Назад
            while (playerZ - FixCoordinate.Instance.PlayerAddPos < queue.First.Value.position.z - 1 + 100)
            {
                var seg = queue.Last.Value;
                queue.RemoveLast();
                float newZ = queue.First.Value.position.z - segmentLength;
                var p = seg.position; p.z = newZ; seg.position = p;
                queue.AddFirst(seg);
                // при движении назад lastEndZ не меняем
            }
        }
        else
        {
            // Финальная локация
            if (playerZ > lastEndZ)
            {
                // OnLevelComplete();
            }
        }
    }

    private void SwitchLocation(int newIndex)
    {
        bool forward = newIndex > currentLocationIndex;

        if (forward)
        {
            // активируем новую локацию без деактивации старой
            var newLoc = locations[newIndex];
            foreach (var seg in newLoc.groundSegments)
                seg.gameObject.SetActive(true);

            // позиционируем сегменты сразу после lastEndZ
            for (int j = 0; j < newLoc.groundSegments.Length; j++)
            {
                var seg = newLoc.groundSegments[j];
                var pos = seg.position;
                pos.z = locations[currentLocationIndex].segmentsQueue.Last.Value.position.z + segmentLength * (j + 1);
                seg.position = pos;
            }

            // инициализируем очередь по возрастающему Z
            newLoc.segmentsQueue = new LinkedList<Transform>(
                newLoc.groundSegments.OrderBy(s => s.position.z));

            // обновляем lastEndZ
            lastEndZ += newLoc.totalLength;
            currentLocationIndex = newIndex;
        }
        else // backward
        {
            // деактивируем все локации впереди текущей
            for (int i = newIndex + 1; i < locations.Length; i++)
                foreach (var seg in locations[i].groundSegments)
                    seg.gameObject.SetActive(false);

            // переключаемся
            currentLocationIndex = newIndex;
            var newLoc = locations[currentLocationIndex];
            foreach (var seg in newLoc.groundSegments)
                seg.gameObject.SetActive(true);

            // создаём очередь по текущему положению
            var sorted = newLoc.groundSegments.OrderBy(s => s.position.z);
            newLoc.segmentsQueue = new LinkedList<Transform>(sorted);

            // пересчитываем lastEndZ среди всех активных сегментов
            float maxZ = float.MinValue;
            for (int i = 0; i <= currentLocationIndex; i++)
                foreach (var seg in locations[i].groundSegments)
                    if (seg.gameObject.activeSelf)
                        maxZ = Mathf.Max(maxZ, seg.position.z);
            lastEndZ = maxZ;
        }
    }
}