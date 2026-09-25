using UnityEngine;

/// <summary>
/// Confirmation gate for leaving an active run from the BASE command.
/// The dialog is authored into Canvas.prefab by the editor visual-refresh
/// script, while this component owns the safe runtime behavior.
/// </summary>
public class TreadShredBaseConfirmation : MonoBehaviour
{
    public GameObject dialogRoot;

    private void Awake()
    {
        if (dialogRoot != null)
            dialogRoot.SetActive(false);
    }

    public void RequestReturnToBase()
    {
        if (dialogRoot != null)
            dialogRoot.SetActive(true);
    }

    public void CancelReturnToBase()
    {
        if (dialogRoot != null)
            dialogRoot.SetActive(false);
    }

    public void ConfirmReturnToBase()
    {
        if (dialogRoot != null)
            dialogRoot.SetActive(false);

        var managerButton = FindObjectOfType<ManagerButton>();
        if (managerButton != null)
        {
            managerButton.LoadMenu();
            return;
        }

        if (LoadLv.instance != null)
        {
            LoadLv.instance.PlayToMenu();
            return;
        }

        // Keep the command safe if a future scene does not include LoadLv.
        Application.LoadLevel("Start");
    }
}
