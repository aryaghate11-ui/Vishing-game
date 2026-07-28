using UnityEngine;

namespace VishingGame.Story
{
    public sealed class DisableQrPanelOnPhoneReady : MonoBehaviour
    {
        [SerializeField] private GameObject qrPanel;

        public void OnPhoneReady()
        {
            if (qrPanel != null)
            {
                qrPanel.SetActive(false);
            }
        }

        private void Reset()
        {
            qrPanel = gameObject;
        }
    }
}
