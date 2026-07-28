using UnityEngine;

namespace VishingGame.Laptop
{
    public sealed class LaptopScreen : MonoBehaviour
    {
        private LaptopInteractionSystem laptop;

        public void Show(LaptopInteractionSystem owner)
        {
            laptop = owner;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Back()
        {
            laptop?.Back();
        }
    }
}
