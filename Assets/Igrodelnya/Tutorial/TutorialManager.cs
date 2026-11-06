using System.Collections.Generic;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;
    [SerializeField] private List<TutorialStep> _tutorialSteps = new List<TutorialStep>();
    [SerializeField] private bool _authoredFirstRun;
    public bool AuthoredFirstRun => _authoredFirstRun;
    public bool IntroComplete => _currentIndex >= _tutorialSteps.Count;
    public int CurrentStepIndex => _currentIndex;
    private int _currentIndex;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        GameManager.Instance.GameStart += StartTutorial;
    }

    private void OnDisable()
    {
        if (GameManager.Instance)
            GameManager.Instance.GameStart -= StartTutorial;
    }


    public void StartTutorial()
    {
        if (!_authoredFirstRun && SaveManager.Instance.GetTutorialProgress())
            return;

        if (_tutorialSteps.Count <= 0)
        {
            Debug.Log("Не заполнены Шаги тутера");
            return;
        }
        _currentIndex = 0;
        ActivateNewStep();
    }
    private void StepEnd(TutorialStep step)
    {
        step.StepEnd -= StepEnd;
        _currentIndex = _tutorialSteps.IndexOf(step) + 1;
        ActivateNewStep();
    }
    private void ActivateNewStep()
    {
        if (_currentIndex < _tutorialSteps.Count)
        {
            _tutorialSteps[_currentIndex].StepEnd += StepEnd;
            _tutorialSteps[_currentIndex].ActivateStep();
        } 
        else
        {
            if (!_authoredFirstRun) SaveManager.Instance.SaveTutorialProgress(true);
            Debug.Log("Тутор завершон");
        }
    }
    public void QuickStopTutorial()
    {
        if (!_authoredFirstRun && SaveManager.Instance.GetTutorialProgress())
            return;
        if (_currentIndex >= _tutorialSteps.Count)
            return;
        _tutorialSteps[_currentIndex].StepEnd -= StepEnd;
        _tutorialSteps[_currentIndex].DeactivateStep();
        _currentIndex = _tutorialSteps.Count;
        ActivateNewStep();
    }
}
