using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class WebEventsHandler : MonoBehaviour
{
    [SerializeField] private WebEventReceivedEvent onAnyWebEventReceived = new WebEventReceivedEvent();
    [SerializeField] private List<WebEventRoute> eventRoutes = new List<WebEventRoute>();

    public WebEventReceivedEvent OnAnyWebEventReceived => onAnyWebEventReceived;

    public void ReceivedWebEvent(string eventName)
    {
        DispatchWebEvent(eventName, string.Empty);
    }

    public void ReceivedWebEventPayload(string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            Debug.LogWarning("Received empty web event payload.");
            return;
        }

        WebEventPayload payload;

        try
        {
            payload = JsonUtility.FromJson<WebEventPayload>(payloadJson);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Could not parse web event payload: {payloadJson}. {exception.Message}");
            return;
        }

        if (payload == null || string.IsNullOrWhiteSpace(payload.eventName))
        {
            Debug.LogWarning($"Received web event payload without an event name: {payloadJson}");
            return;
        }

        DispatchWebEvent(payload.eventName, payload.eventData ?? string.Empty);
    }

    public void ReceivedWebEventWithData(string eventName, string eventData)
    {
        DispatchWebEvent(eventName, eventData);
    }

    private void DispatchWebEvent(string eventName, string eventData)
    {
        if (string.IsNullOrWhiteSpace(eventName))
        {
            Debug.LogWarning("Received web event without an event name.");
            return;
        }

        Debug.Log($"Received web event: {eventName} with data: {eventData}");
        onAnyWebEventReceived?.Invoke(eventName, eventData);

        for (int i = 0; i < eventRoutes.Count; i++)
        {
            WebEventRoute route = eventRoutes[i];

            if (route != null && route.Matches(eventName))
            {
                route.Invoke(eventData);
            }
        }
    }

    [Serializable]
    public sealed class WebEventReceivedEvent : UnityEvent<string, string>
    {
    }

    [Serializable]
    private sealed class WebEventPayload
    {
        public string eventName;
        public string eventData;
    }

    [Serializable]
    private sealed class WebEventRoute
    {
        [SerializeField] private string eventName;
        [SerializeField] private UnityEvent<string> onReceived = new UnityEvent<string>();

        public bool Matches(string receivedEventName)
        {
            return string.Equals(eventName, receivedEventName, StringComparison.Ordinal);
        }

        public void Invoke(string eventData)
        {
            onReceived?.Invoke(eventData);
        }
    }
}
