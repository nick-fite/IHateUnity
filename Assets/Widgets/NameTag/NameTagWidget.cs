using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class NameTagWidget : Widget
{

    [SerializeField] private TextMeshProUGUI nameText;

    public override void SetOwner(GameObject newOwner)
    {
        base.SetOwner(newOwner);
        NameComponent ownerNameComponent = newOwner.GetComponent<NameComponent>();
        if (ownerNameComponent)
        {
            ownerNameComponent.OnDisplayNameChanged += UpdateText;
        }
    }
    private void UpdateText(string newName)
    {
        if (newName == "" || newName == null)
        {
            return;
        }
        nameText.text = newName;
    }
}
