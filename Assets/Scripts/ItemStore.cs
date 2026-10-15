using System;
using System.Collections.Generic;
using UnityEngine;

public class ItemStore : MonoBehaviour
{
    [SerializeField] private Transform _pointsParent;
    [SerializeField] private StoreItemsCollection _collection;

    [SerializeField] private bool _inLobby;

    public Action<bool> PlayerEnter;

    private void Start()
    {
        var points = _pointsParent.GetComponentsInChildren<StorePoint>();
        var random = DeadBoat.Online.SharedRunContext.GenerationActive
            ? DeadBoat.Online.SharedRunContext.Random("store:" +
                DeadBoat.Online.WorldSpawnIdentity.StablePath(transform), 0) : null;
        var assortment = SelectAssortment(points, random);
        for (int i = 0; i < points.Length; i++)
            if (points[i].StaticItem || assortment[i] != null)
                points[i].InitPoint(_inLobby, this, assortment[i]);
    }

    // No UI, instantiation, or shared pool mutation; also used by the Editor audit.
    private PickableItem[] SelectAssortment(StorePoint[] points, DeadBoat.Online.RunRandom random)
    {
        var available = _collection != null ? new List<PickableItem>(_collection.Items) : new List<PickableItem>();
        var result = new PickableItem[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i].StaticItem || available.Count == 0) continue;
            int index = random != null ? random.Range(0, available.Count)
                : UnityEngine.Random.Range(0, available.Count);
            result[i] = available[index];
            available.RemoveAt(index);
        }
        return result;
    }



    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            PlayerEnter?.Invoke(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            PlayerEnter?.Invoke(false);
        }
    }




}
