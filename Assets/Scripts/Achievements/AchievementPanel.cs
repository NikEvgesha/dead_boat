using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AchievementPanel : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
    [SerializeField] private Text _title;
    [SerializeField] private Text _description;

    [SerializeField] private Animator _animator;

    private LinkedList<Achievement> _achievmentsQueue = new();

    public bool isActive { get; set; }

    private void Start()
    {
        _animator = GetComponent<Animator>();
    }


    void Show()
    {
        //_panel.SetActive(true);
        _animator.SetTrigger("Open");
        StartCoroutine(Wait(5));
    }

    void Hide()
    {
        _achievmentsQueue.RemoveFirst();
        //_panel.SetActive(false);
        if (_achievmentsQueue.Count > 0)
        {
            SetAchievement(_achievmentsQueue.First.Value);
        } else
        {
            isActive = false;
        }
    }

    private void SetAchievement(Achievement achievment)
    {
        _title.text = achievment.title;
        _description.text = achievment.description;
        isActive = true;
        Show();
    }

    IEnumerator Wait(float t)
    {
        yield return new WaitForSeconds(t-1);
        _animator.SetTrigger("Hide");
        yield return new WaitForSeconds(1);
        Hide();
    }

    public void ShowAchievement(Achievement achievement)
    {
        _achievmentsQueue.AddLast(achievement);
        if (!isActive)
        {
            SetAchievement(_achievmentsQueue.First.Value);
        }
    }

}
