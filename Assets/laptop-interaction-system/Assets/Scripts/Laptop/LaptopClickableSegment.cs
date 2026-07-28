using UnityEngine;
using UnityEngine.Events;

namespace VishingGame.Laptop
{
    public sealed class LaptopClickableSegment : MonoBehaviour
    {
        [SerializeField] private LaptopInteractionSystem laptop;
        [SerializeField] private LaptopScreen targetScreen;
        [SerializeField] private bool reportLaptopEvent;
        [SerializeField] private string laptopEventName;
        [SerializeField] private bool closeLaptopAfterClick;

        public UnityEvent OnClicked;

        public void Click()
        {
            if (reportLaptopEvent)
            {
                laptop.ReportLaptopEvent(laptopEventName);
            }

            OnClicked?.Invoke();

            if (targetScreen != null)
            {
                laptop.ShowScreen(targetScreen);
            }

            if (closeLaptopAfterClick)
            {
                laptop.CloseLaptop();
            }
        }

        private void Reset()
        {
            laptop = FindObjectOfType<LaptopInteractionSystem>();
        }
    }
}
