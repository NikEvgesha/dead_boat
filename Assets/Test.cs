using MirraGames.SDK;
using System;
using UnityEngine;

public class Test : MonoBehaviour
{
    Action action;
    // Start is called before the first frame update
    void Start()
    {
        action += Action;
        MirraSDK.WaitForProviders(action);
    }
    void Action()
    {
        MirraSDK.Ads.InvokeInterstitial(
            onOpen: () =>
            {
                Debug.Log("MirraSDK: Interstitial ad opened");
            },
            onClose: (success) =>
            {
                Debug.Log("MirraSDK: Interstitial ad closed");
            }
        );
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
