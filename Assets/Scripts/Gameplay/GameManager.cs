using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LedShow.Gameplay
{
    public enum GameState
    {
        Idle,
        Playing,
        Dead
    }

    // Single source of truth for the game loop state, so gameplay, UI and the
    // LED show can all react to Idle/Playing/Dead without polling each other.
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private float deadToIdleDelay = 3f;

        public GameState CurrentState { get; private set; } = GameState.Idle;

        public event Action<GameState> StateChanged;

        private Coroutine returnToIdleRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (CurrentState == GameState.Idle && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                StartPlaying();
            }
        }

        public void StartPlaying()
        {
            if (CurrentState != GameState.Idle)
            {
                return;
            }

            SetState(GameState.Playing);
        }

        public void NotifyCollision()
        {
            if (CurrentState != GameState.Playing)
            {
                return;
            }

            SetState(GameState.Dead);

            if (returnToIdleRoutine != null)
            {
                StopCoroutine(returnToIdleRoutine);
            }

            returnToIdleRoutine = StartCoroutine(ReturnToIdleAfterDelay());
        }

        private IEnumerator ReturnToIdleAfterDelay()
        {
            yield return new WaitForSeconds(deadToIdleDelay);
            returnToIdleRoutine = null;
            SetState(GameState.Idle);
        }

        private void SetState(GameState newState)
        {
            CurrentState = newState;
            StateChanged?.Invoke(newState);
        }
    }
}
