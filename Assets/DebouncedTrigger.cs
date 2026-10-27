using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[AddComponentMenu("Event/Event Trigger V2 (Debounced)")]
public class EventTriggerV2 : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler,
    IPointerClickHandler, IInitializePotentialDragHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IDropHandler, IScrollHandler, IUpdateSelectedHandler,
    ISelectHandler, IDeselectHandler, IMoveHandler,
    ISubmitHandler, ICancelHandler
{
    [Serializable]
    public class TriggerEvent : UnityEvent<BaseEventData> { }

    [Serializable]
    public class Entry
    {
        public EventTriggerType eventID = EventTriggerType.PointerClick;
        public TriggerEvent callback = new TriggerEvent();
    }

    [SerializeField]
    private List<Entry> m_Delegates;

    public List<Entry> triggers
    {
        get
        {
            if (m_Delegates == null) m_Delegates = new List<Entry>();
            return m_Delegates;
        }
        set { m_Delegates = value; }
    }

    [Header("Debounce Settings")]
    [Tooltip("Seconds to wait after enabling before events are allowed")]
    public float debounceTime = 5f;

    private float enableTime;
    private bool debouncePassed => Time.time - enableTime >= debounceTime;

    private void OnEnable()
    {
        enableTime = Time.time;
        // Play click sound when any event is triggered
        
    }

    private void Execute(EventTriggerType id, BaseEventData eventData)
{
    if (!debouncePassed) return; // ignore events during debounce
    
  
    
    for (int i = 0; i < triggers.Count; i++)
    {
        var ent = triggers[i];
        if (ent.eventID == id && ent.callback != null)
            ent.callback.Invoke(eventData);
    }
}

    // --- Override all EventTrigger events ---
    public void OnPointerEnter(PointerEventData eventData) => Execute(EventTriggerType.PointerEnter, eventData);
    public void OnPointerExit(PointerEventData eventData) => Execute(EventTriggerType.PointerExit, eventData);
    public void OnPointerDown(PointerEventData eventData) => Execute(EventTriggerType.PointerDown, eventData);
    public void OnPointerUp(PointerEventData eventData) => Execute(EventTriggerType.PointerUp, eventData);
    public void OnPointerClick(PointerEventData eventData) => Execute(EventTriggerType.PointerClick, eventData);
    public void OnSelect(BaseEventData eventData) => Execute(EventTriggerType.Select, eventData);
    public void OnDeselect(BaseEventData eventData) => Execute(EventTriggerType.Deselect, eventData);
    public void OnScroll(PointerEventData eventData) => Execute(EventTriggerType.Scroll, eventData);
    public void OnMove(AxisEventData eventData) => Execute(EventTriggerType.Move, eventData);
    public void OnUpdateSelected(BaseEventData eventData) => Execute(EventTriggerType.UpdateSelected, eventData);
    public void OnInitializePotentialDrag(PointerEventData eventData) => Execute(EventTriggerType.InitializePotentialDrag, eventData);
    public void OnBeginDrag(PointerEventData eventData) => Execute(EventTriggerType.BeginDrag, eventData);
    public void OnDrag(PointerEventData eventData) => Execute(EventTriggerType.Drag, eventData);
    public void OnEndDrag(PointerEventData eventData) => Execute(EventTriggerType.EndDrag, eventData);
    public void OnDrop(PointerEventData eventData) => Execute(EventTriggerType.Drop, eventData);
    public void OnSubmit(BaseEventData eventData) => Execute(EventTriggerType.Submit, eventData);
    public void OnCancel(BaseEventData eventData) => Execute(EventTriggerType.Cancel, eventData);
}
