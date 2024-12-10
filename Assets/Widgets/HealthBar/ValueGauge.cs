using UnityEngine;
using UnityEngine.UI;

public class ValueGauge : Widget
{
    [SerializeField] private Slider slider;

    public void UpdateValue(float newValue, float newMaxValue)
    {
        if (newMaxValue == 0)
            return;

        slider.value = newValue / newMaxValue;
    }
}
