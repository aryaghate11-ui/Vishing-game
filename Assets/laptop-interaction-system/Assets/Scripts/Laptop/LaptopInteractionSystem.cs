using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VishingGame.Laptop
{
    [System.Serializable]
    public sealed class LaptopStringEvent : UnityEvent<string> { }

    public sealed class LaptopInteractionSystem : MonoBehaviour
    {
        [Header("Story Gate")]
        [SerializeField] private bool requireStoryGate;
        [SerializeField] private bool storyGateOpen = true;
        [SerializeField] private string openLaptopEventName = "LaptopClicked";

        [Header("Player Detection")]
        [SerializeField] private Transform player;
        [SerializeField] private float interactionDistance = 2.2f;
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField] private KeyCode closeKey = KeyCode.Escape;

        [Header("UI")]
        [SerializeField] private GameObject laptopUiRoot;
        [SerializeField] private LaptopScreen startScreen;
        [SerializeField] private bool freezePlayerWhileOpen = true;
        [SerializeField] private MonoBehaviour[] movementScriptsToDisable;

        [Header("Events")]
        public UnityEvent OnLaptopOpened;
        public UnityEvent OnLaptopClosed;
        public UnityEvent OnLaptopUnavailable;
        public LaptopStringEvent OnLaptopEvent;

        private readonly Stack<LaptopScreen> screenHistory = new Stack<LaptopScreen>();
        private LaptopScreen currentScreen;
        private bool isOpen;

        public bool IsOpen => isOpen;
        public bool IsPlayerNear => player != null && Vector3.Distance(player.position, transform.position) <= interactionDistance;
        public bool IsStoryAllowed => !requireStoryGate || storyGateOpen;
        public bool CanInteract => IsPlayerNear && IsStoryAllowed;

        private void Awake()
        {
            if (laptopUiRoot != null)
            {
                laptopUiRoot.SetActive(false);
            }
        }

        private void Update()
        {
            if (!isOpen)
            {
                if (Input.GetKeyDown(interactKey) && IsPlayerNear)
                {
                    TryOpenLaptop();
                }

                return;
            }

            if (Input.GetKeyDown(closeKey))
            {
                CloseLaptop();
            }
        }

        public void TryOpenLaptop()
        {
            if (!CanInteract)
            {
                OnLaptopUnavailable?.Invoke();
                return;
            }

            OpenLaptop();
        }

        public void OpenLaptop()
        {
            if (isOpen)
            {
                return;
            }

            isOpen = true;
            screenHistory.Clear();

            if (laptopUiRoot != null)
            {
                laptopUiRoot.SetActive(true);
            }

            SetCursorEnabled(true);
            SetPlayerControlEnabled(false);
            ShowScreen(startScreen, false);

            ReportLaptopEvent(openLaptopEventName);
            OnLaptopOpened?.Invoke();
        }

        public void CloseLaptop()
        {
            if (!isOpen)
            {
                return;
            }

            isOpen = false;

            if (laptopUiRoot != null)
            {
                laptopUiRoot.SetActive(false);
            }

            SetCursorEnabled(false);
            SetPlayerControlEnabled(true);
            OnLaptopClosed?.Invoke();
        }

        public void ShowScreen(LaptopScreen nextScreen)
        {
            ShowScreen(nextScreen, true);
        }

        public void Back()
        {
            if (screenHistory.Count == 0)
            {
                CloseLaptop();
                return;
            }

            LaptopScreen previousScreen = screenHistory.Pop();
            ShowScreen(previousScreen, false);
        }

        public void SetStoryGateOpen(bool open)
        {
            storyGateOpen = open;
        }

        public void SetRequireStoryGate(bool required)
        {
            requireStoryGate = required;
        }

        public void ReportLaptopEvent(string eventName)
        {
            if (!string.IsNullOrEmpty(eventName))
            {
                OnLaptopEvent?.Invoke(eventName);
            }
        }

        private void ShowScreen(LaptopScreen nextScreen, bool rememberCurrent)
        {
            if (nextScreen == null)
            {
                return;
            }

            if (currentScreen != null)
            {
                if (rememberCurrent)
                {
                    screenHistory.Push(currentScreen);
                }

                currentScreen.Hide();
            }

            currentScreen = nextScreen;
            currentScreen.Show(this);
        }

        private void SetCursorEnabled(bool enabled)
        {
            Cursor.visible = enabled;
            Cursor.lockState = enabled ? CursorLockMode.None : CursorLockMode.Locked;
        }

        private void SetPlayerControlEnabled(bool enabled)
        {
            if (!freezePlayerWhileOpen)
            {
                return;
            }

            foreach (MonoBehaviour movementScript in movementScriptsToDisable)
            {
                if (movementScript != null)
                {
                    movementScript.enabled = enabled;
                }
            }
        }

        private void Reset() { }
    }
}
