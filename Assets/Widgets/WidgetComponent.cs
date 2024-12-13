using System;
using UnityEngine;

public class WidgetComponent : MonoBehaviour
{
    [SerializeField] private Widget widgetPrefab;
    [SerializeField] private Transform attachTransform;
    [SerializeField] private Camera mainCamera;

    private Widget _widget;
    private void Awake()
    {
        _widget = Instantiate(widgetPrefab);
        _widget.SetOwner(gameObject);

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas)
        {
            _widget.transform.SetParent(canvas.transform);
        }
    }

    private void Update()
    {
        if (_widget && attachTransform && mainCamera)
        {
            _widget.transform.position = mainCamera.WorldToScreenPoint(attachTransform.position);
        }
    }
}
