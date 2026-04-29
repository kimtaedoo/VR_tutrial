using Oculus.Interaction;
using Oculus.Interaction.Input;
using UnityEngine;

public class ControllerGrabInputOverride : MonoBehaviour
{
    [SerializeField] private ControllerButtonUsage grabButtonUsage = ControllerButtonUsage.TriggerButton;

    private void Awake()
    {
        ControllerSelector[] selectors = FindObjectsByType<ControllerSelector>(
            FindObjectsInactive.Include);

        foreach (ControllerSelector selector in selectors)
        {
            if (!IsGrabSelector(selector))
            {
                continue;
            }

            if (selector.ControllerButtonUsage == ControllerButtonUsage.GripButton)
            {
                selector.ControllerButtonUsage = grabButtonUsage;
            }
        }
    }

    private bool IsGrabSelector(ControllerSelector selector)
    {
        Transform current = selector.transform;

        while (current != null)
        {
            if (current.name.Contains("Grab"))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }
}
