using UnityEngine;
using UnityEngine.Events;

namespace VishingGame.Laptop
{
    public enum LaptopEmailKind
    {
        Normal,
        Suspicious
    }

    public sealed class LaptopEmailRow : MonoBehaviour
    {
        [SerializeField] private LaptopInteractionSystem laptop;
        [SerializeField] private LaptopEmailKind emailKind;
        [SerializeField] private LaptopScreen emailScreen;
        [SerializeField] private bool reportEventForSuspicious = true;
        [SerializeField] private string suspiciousEmailEventName = "EmailClicked";

        public UnityEvent OnOpened;

        public void OpenEmail()
        {
            OnOpened?.Invoke();

            if (emailScreen != null)
            {
                laptop.ShowScreen(emailScreen);
            }

            if (emailKind == LaptopEmailKind.Suspicious && reportEventForSuspicious)
            {
                laptop.ReportLaptopEvent(suspiciousEmailEventName);
            }
        }

        private void Reset()
        {
            laptop = FindObjectOfType<LaptopInteractionSystem>();
        }
    }
}
