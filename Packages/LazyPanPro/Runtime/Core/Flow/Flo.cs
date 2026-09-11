using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.SceneManagement;

namespace LazyPan {
    public class Flo : Singleton<Flo> {
        Dictionary<Type, Flow> flows = new Dictionary<Type, Flow>();
        Dictionary<string, Flow> signFlows = new Dictionary<string, Flow>();
        public string CurFlowSign;
        public void Preload() {
            string sceneName = SceneManager.GetActiveScene().name;
            SceneConfig sceneConfig = SceneConfig.Get(sceneName);
            CurFlowSign = string.Concat("Flow_", sceneConfig.Flow);
            Type type = Assembly.Load("Assembly-CSharp").GetType(string.Concat("LazyPan.", CurFlowSign));
            Flow flow = (Flow) Activator.CreateInstance(type);
            flows.Clear();
            flows.Add(type, flow);
            signFlows.Clear();
            signFlows.Add(CurFlowSign, flow);
            flow.Init(null);
        }

        public bool GetFlow<T>(out T flow) where T : Flow {
            if (flows.ContainsKey(typeof(T))) {
                flow = (T)flows[typeof(T)];
                return true;
            }

            flow = default;
            return false;
        }

        public bool GetCurFlow(out Flow outFlow) {
            if (signFlows.ContainsKey(CurFlowSign)) {
                outFlow = signFlows[CurFlowSign];
                return true;
            }

            outFlow = default;
            return false;
        }
    }
}