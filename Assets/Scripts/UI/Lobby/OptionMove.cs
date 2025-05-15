using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Lobby
{
    public enum OptionMoveDir
    {
        None = 0,
        Left,
        Right
    }
    public class OptionMove : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private OptionMoveDir _dir;

        public Action<OptionMoveDir> OnClick;

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClick?.Invoke(_dir);
        }
    }
}
