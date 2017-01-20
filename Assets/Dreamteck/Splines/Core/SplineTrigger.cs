using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using System.Reflection;

namespace Dreamteck.Splines
{

    [System.Serializable]
    public class SplineAction
    {
        [SerializeField]
        public Object target = null;

        public int intValue;
        public float floatValue;
        public double doubleValue;
        public string stringValue;
        public bool boolValue;
        public GameObject goValue;
        public Transform transformValue;


        private UnityAction action;
        private UnityAction<int> intAction;
        private UnityAction<float> floatAction;
        private UnityAction<double> doubleAction;
        private UnityAction<string> stringAction;
        private UnityAction<bool> boolAction;
        private UnityAction<GameObject> goAction;
        private UnityAction<Transform> transformAction;

        private MethodInfo methodInfo = null;

        [SerializeField]
        private string methodName = "";

        [SerializeField]
        private int paramType = 0;

        public void SetMethod(MethodInfo newMethod)
        {

            ParameterInfo[] parameters = newMethod.GetParameters();
            if(parameters.Length > 1)
            {
                Debug.LogError("Cannot add method with more than one argument");
                return;
            }
            methodInfo = newMethod;
            methodName = methodInfo.Name;
            if (parameters.Length == 0) paramType = 0;
            else
            {
                if (parameters[0].ParameterType == typeof(int)) paramType = 1;
                else if (parameters[0].ParameterType == typeof(float)) paramType = 2;
                else if (parameters[0].ParameterType == typeof(double)) paramType = 3;
                else if (parameters[0].ParameterType == typeof(string)) paramType = 4;
                else if (parameters[0].ParameterType == typeof(bool)) paramType = 5;
                else if (parameters[0].ParameterType == typeof(GameObject)) paramType = 6;
                else if (parameters[0].ParameterType == typeof(Transform)) paramType = 7;
            }
            ConstructUnityAction();
        }

        public MethodInfo GetMethod()
        {
            if(methodInfo == null)
            {
                if(target != null && methodName != "") methodInfo = target.GetType().GetMethod(methodName);
            }
            return methodInfo;
        }

        private void ConstructUnityAction()
        {
            action = null;
            intAction = null;
            floatAction = null;
            doubleAction = null;
            stringAction = null;
            boolAction = null;
            goAction = null;
            transformAction = null;
            if(methodInfo == null)
            {
                methodInfo = target.GetType().GetMethod(methodName);
            }
            switch (paramType)
            {
                case 0: action = (UnityAction)System.Delegate.CreateDelegate(typeof(UnityAction), target, methodInfo); break;
                case 1: intAction = (UnityAction<int>)System.Delegate.CreateDelegate(typeof(UnityAction<int>), target, methodInfo); break;
                case 2: floatAction = (UnityAction<float>)System.Delegate.CreateDelegate(typeof(UnityAction<float>), target, methodInfo); break;
                case 3: doubleAction = (UnityAction<double>)System.Delegate.CreateDelegate(typeof(UnityAction<double>), target, methodInfo); break;
                case 4: stringAction = (UnityAction<string>)System.Delegate.CreateDelegate(typeof(UnityAction<string>), target, methodInfo); break;
                case 5: boolAction = (UnityAction<bool>)System.Delegate.CreateDelegate(typeof(UnityAction<bool>), target, methodInfo); break;
                case 6: goAction = (UnityAction<GameObject>)System.Delegate.CreateDelegate(typeof(UnityAction<GameObject>), target, methodInfo); break;
                case 7: transformAction = (UnityAction<Transform>)System.Delegate.CreateDelegate(typeof(UnityAction<Transform>), target, methodInfo); break;
            }
        }

        public void Invoke()
        {
            switch (paramType)
            {
                case 0: if (action == null) ConstructUnityAction(); action.Invoke(); break;
                case 1: if (intAction == null) ConstructUnityAction();  intAction.Invoke(intValue); break;
                case 2: if (floatAction == null) ConstructUnityAction(); floatAction.Invoke(floatValue); break;
                case 3: if (doubleAction == null) ConstructUnityAction(); doubleAction.Invoke(doubleValue); break;
                case 4: if (stringAction == null) ConstructUnityAction(); stringAction.Invoke(stringValue); break;
                case 5: if (boolAction == null) ConstructUnityAction(); boolAction.Invoke(boolValue); break;
                case 6: if (goAction == null) ConstructUnityAction(); goAction.Invoke(goValue); break;
                case 7: if (transformAction == null) ConstructUnityAction(); transformAction.Invoke(transformValue); break;
            }
        }

    }


    [System.Serializable]
    public class SplineTrigger : ScriptableObject
    {
        public enum Type { Double, Forward, Backward}
        [SerializeField]
        public Type type = Type.Double;
        [Range(0f, 1f)]
        public double position = 0.5;
        [SerializeField]
        public bool enabled = true;
        [SerializeField]
        public Color color = Color.white;
        [SerializeField]
        [HideInInspector]
        public SplineAction[] actions = new SplineAction[0];
        public GameObject[] gameObjects;

        public SplineTrigger()
        {
        }

        public void init(Type t)
        {
            type = t;
            switch (t)
            {
                case Type.Double: color = Color.yellow; break;
                case Type.Forward: color = Color.green; break;
                case Type.Backward: color = Color.red; break;
            }
            enabled = true;
        }

        public void Check(double previousPercent, double currentPercent)
        {
            if (!enabled) return;
            bool passed = false;
            switch (type)
            {
                case Type.Double: passed = (previousPercent <= position && currentPercent >= position) || (currentPercent <= position && previousPercent >= position); break;
                case Type.Forward: passed = previousPercent <= position && currentPercent >= position; break;
                case Type.Backward: passed = currentPercent <= position && previousPercent >= position; break;
            }
            if (passed)
            {
                for(int i = 0; i < actions.Length; i++)
                {
                    actions[i].Invoke(); 
                }
            }
        }

        private void AddAction()
        {
            SplineAction[] newActions = new SplineAction[actions.Length + 1];
            actions.CopyTo(newActions, 0);
            newActions[newActions.Length - 1] = new SplineAction();
            actions = newActions;
        }

        public void AddListener(MonoBehaviour behavior, string method, object arg)
        {
            AddAction();
            actions[actions.Length - 1].target = behavior;
            MethodInfo methodInfo = behavior.GetType().GetMethod(method);
            actions[actions.Length - 1].SetMethod(behavior.GetType().GetMethod(method));
            ParameterInfo[] parameters = methodInfo.GetParameters();
            if(parameters.Length == 1)
            {
                if (parameters[0].ParameterType == typeof(int)) actions[actions.Length - 1].intValue = (int)arg;
                else if (parameters[0].ParameterType == typeof(float)) actions[actions.Length - 1].floatValue = (float)arg;
                else if (parameters[0].ParameterType == typeof(double)) actions[actions.Length - 1].doubleValue = (double)arg;
                else if (parameters[0].ParameterType == typeof(string)) actions[actions.Length - 1].stringValue = (string)arg;
                else if (parameters[0].ParameterType == typeof(bool)) actions[actions.Length - 1].boolValue = (bool)arg;
                else if (parameters[0].ParameterType == typeof(GameObject)) actions[actions.Length - 1].goValue = (GameObject)arg;
                else if (parameters[0].ParameterType == typeof(Transform)) actions[actions.Length - 1].transformValue = (Transform)arg;
            }
        }
    }
}
