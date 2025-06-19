using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrainMagnet : MonoBehaviour
{
    [SerializeField] private List<PickableItem> _items = new List<PickableItem>();
    private void OnTriggerEnter(Collider other)
    {
        if (GameManager.Instance.isEndGame)
            return;
        if (other.tag == "Player" || other.tag == "Enemy" || other.tag == "Item")
            other.transform.SetParent(this.gameObject.transform.parent);
        if (other.tag == "Item" && other.TryGetComponent<PickableItem>(out PickableItem item) && !_items.Contains(item))
            _items.Add(item);
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player" || other.tag == "Enemy" || other.tag == "Item")
            other.transform.SetParent(null);
        if(other.tag == "Player")
        {
            DontDestroyOnLoad(other.gameObject);
        }
    }
    private void Awake()
    {
        LoadItems();
    }
    private void Start()
    {
        StartCoroutine(SaveItem());
    }
    private void OnDisable() 
    { 
        StopAllCoroutines();
    }
    IEnumerator SaveItem()
    {
        while (this.enabled)
        {
            yield return new WaitForSeconds(1);
            _items.RemoveAll(item => item.Status == ItemStatus.InInventory);
            _items.RemoveAll(item => !item);
            foreach (var item in _items)
            {
                item.CheckSaveItem();
            }

            if (GameManager.Instance.isEndGame)
                continue;
            SaveManager.Instance.SaveBoardItem(_items);
        }
    }
    private void LoadItems()
    {
        foreach (var item in SaveManager.Instance.LoadBoardItem())
        {
            PickableItem pickItem = new PickableItem();
            pickItem = Instantiate(ItemsManager.Instance.GetItem(item.prefabName),null);
            pickItem.SetLoadItem(item);
            _items.Add(pickItem);
        }
        
    }
}
