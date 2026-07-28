using System.Collections;
using UnityEngine;

public class PhoneCallSequenceExample : MonoBehaviour
{
    private void OnEnable()
    {
        StartCoroutine(DemoSequence());
    }

    private IEnumerator DemoSequence()
    {
        yield return new WaitUntil(() => PhoneCallBridge.Instance != null);

        PhoneCallBridge.Instance.OnCallAnswered += HandleAnswered;
        PhoneCallBridge.Instance.OnCallDeclined += HandleDeclined;
        PhoneCallBridge.Instance.OnAccountFrozen += HandleAccountFrozen;

        yield return new WaitForSeconds(8f);
        PhoneCallBridge.Instance.TriggerIncomingCall("Cyber Crime Cell", "Unknown number");
    }

    private void HandleAnswered(string payload)
    {
        Debug.Log("[PhoneCallSequenceExample] Player answered. Continue scam dialogue in Unity.");
        StartCoroutine(ShowBankingSoon());
    }

    private void HandleDeclined(string payload)
    {
        Debug.Log("[PhoneCallSequenceExample] Player declined. Trigger another call or branch story.");
    }

    private void HandleAccountFrozen(string payload)
    {
        Debug.Log("[PhoneCallSequenceExample] Player froze account. Advance to resolution.");
    }

    private IEnumerator ShowBankingSoon()
    {
        yield return new WaitForSeconds(5f);
        PhoneCallBridge.Instance.ShowBankingApp();
    }
}
