using UnityEngine;

[CreateAssetMenu(menuName = "Paper Game/Online account settings")]
public class OnlineAccountSettings : ScriptableObject
{
    [Tooltip("PlayFab Account Recovery template ID; requires the title's SMTP and recovery callback configuration.")]
    public string recoveryEmailTemplateId;
}
