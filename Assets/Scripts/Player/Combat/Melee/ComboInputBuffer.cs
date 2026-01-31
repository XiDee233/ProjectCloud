using System.Collections.Generic;
using UnityEngine;

namespace Player.Combat.Melee
{
    public class ComboInputBuffer
    {
        private readonly List<ComboInputEvent> _events = new List<ComboInputEvent>(16);

        public int MaxCount { get; set; } = 12;
        public float MaxAgeSeconds { get; set; } = 0.35f;

        public void Push(ComboInputEvent e)
        {
            _events.Add(e);

            if (_events.Count > MaxCount)
            {
                int removeCount = _events.Count - MaxCount;
                _events.RemoveRange(0, removeCount);
            }
        }

        public void Cleanup(float now)
        {
            for (int i = _events.Count - 1; i >= 0; i--)
            {
                if (now - _events[i].time > MaxAgeSeconds)
                    _events.RemoveAt(i);
            }
        }

        public void Clear()
        {
            _events.Clear();
        }

        public IReadOnlyList<ComboInputEvent> Events => _events;

        public void ConsumePrefix(int count)
        {
            if (count <= 0) return;
            if (count >= _events.Count) { _events.Clear(); return; }
            _events.RemoveRange(0, count);
        }
    }
}
