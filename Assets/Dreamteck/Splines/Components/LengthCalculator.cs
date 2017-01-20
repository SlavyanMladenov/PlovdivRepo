using UnityEngine;
using System.Collections;
using System.Reflection;
using UnityEngine.Events;

namespace Dreamteck.Splines
{
    public class LengthCalculator : SplineUser
    {
        [System.Serializable]
        public class LengthEvent
        {
            public bool enabled = true;
            public float targetLength = 0f;
            public SplineAction action = new SplineAction();
            public enum Type { Growing, Shrinking, Both}
            public Type type = Type.Both;

            public void Check(float fromLength, float toLength)
            {
                if (!enabled) return;
                bool condition = false;
                switch (type)
                {
                    case Type.Growing: condition = toLength >= targetLength && fromLength < targetLength; break;
                    case Type.Shrinking: condition = toLength <= targetLength && fromLength > targetLength; break;
                    case Type.Both: condition = toLength >= targetLength && fromLength < targetLength || toLength <= targetLength && fromLength > targetLength; break;
                }
                if (condition) action.Invoke();
            }
        }
        [HideInInspector]
        public LengthEvent[] lengthEvents = new LengthEvent[0];
        [HideInInspector]
        public float idealLength = 1f;
        private float _length = 0f;
        private float lastLength = 0f;
        public float length
        {
            get {
                lock (locker)
                {
                    return _length;
                }
            }
        }

        protected override void Awake()
        {
            base.Awake();
             _length = computer.CalculateLength(clipFrom, clipTo, resolution, _address);
            lastLength = _length;
            for (int i = 0; i < lengthEvents.Length; i++)
            {
                if (lengthEvents[i].targetLength == _length) lengthEvents[i].action.Invoke();
            }
        }

        protected override void Build()
        {
            base.Build();
            _length = computer.CalculateLength(clipFrom, clipTo, resolution, _address);
            if (lastLength != _length)
            {
                for (int i = 0; i < lengthEvents.Length; i++)
                {
                    lengthEvents[i].Check(lastLength, _length);
                }
                lastLength = _length;
            }
        }
    }
}
