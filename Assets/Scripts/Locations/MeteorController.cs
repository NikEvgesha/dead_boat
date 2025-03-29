using UnityEngine;

public class MeteorController : MonoBehaviour
{
    public enum MeteorType { FallingMeteor, StaticItem }

    [Header("Настройки метеора")]
    [SerializeField] private MeteorType _meteorType = MeteorType.FallingMeteor;
    [SerializeField] private float _fallSpeed = 50f;
    [SerializeField] private float _rotationSpeed = 50f;
    [SerializeField] private float _destroyAfterImpactTime = 2f;

    [Header("Эффекты")]
    [SerializeField] private GameObject _impactEffectPrefab;

    private bool _hasImpacted = false;

    void Update()
    {
        if (_hasImpacted) return;

        if (_meteorType == MeteorType.FallingMeteor)
        {
            // Перемещаем метеор вниз
            transform.position += Vector3.down * _fallSpeed * Time.deltaTime;
        }
        else if (_meteorType == MeteorType.StaticItem)
        {
            // Медленно вращаем полезный предмет
            transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_hasImpacted) return;
        _hasImpacted = true;

        // Создаем эффект столкновения (если задан)
        if (_impactEffectPrefab != null)
        {
            Instantiate(_impactEffectPrefab, transform.position, Quaternion.identity);
        }

        // Если это падающий метеор, удаляем его через заданное время
        if (_meteorType == MeteorType.FallingMeteor)
        {
            Destroy(gameObject, _destroyAfterImpactTime);
        }
    }
}
