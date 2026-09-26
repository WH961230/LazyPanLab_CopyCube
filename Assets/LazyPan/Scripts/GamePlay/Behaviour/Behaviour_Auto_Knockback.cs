using UnityEngine;

namespace LazyPan {
    /// <summary>
    /// 行为 - 击退位移
    /// 只做一件事: 别人喊一声就按给定方向推 CharacterController 飞一段，时间到自动停，不调别的移动行为。
    /// 停走命令看实体级 MoveAttr.Stopped(谁置 true 都停)，击退占领看 MoveAttr.KnockingBack(飞完自动松开)。
    /// 配置来源 Setting/KnockbackSetting，运行时状态只写自己的 KnockbackData。
    /// </summary>
    public class Behaviour_Auto_Knockback : Behaviour {
        /// <summary>击退节点只读便签：图节点上直接显示，给用户看的参数说明</summary>
        public static readonly string MemoDoc =
            "【击退】管一个实体被打飞，别人喊（接触伤害命中）才飞，自己不监听按键。\n" +
            "停走命令看实体级 MoveAttr，谁置停都停。\n" +
            "— 配置参数（KnockbackSetting 里按 SourceSign 配）—\n" +
            "- <color=#FFD54F>Duration</color>：默认飞多久（秒），喊的人没给时长就用这个\n" +
            "- <color=#FFD54F>DecayCurve</color>：减速曲线，x=进度0~1，y=速度倍率，空=匀速\n" +
            "- <color=#FFD54F>KnockbackPriority</color>：击退优先级，跟 MoveAttr.MovePriority/TeleportPriority 比，默认最大\n" +
            "— 数据交流（读写实体级 MoveAttr，不直接调别的行为）—\n" +
            "- <color=#FFD54F>停走</color>：MoveAttr.Stopped=true 全体移动行为一起停，false=恢复\n" +
            "- <color=#FFD54F>占领</color>：起飞置 MoveAttr.KnockingBack=true，WASD 和瞬移水平让路只留重力，落地=false=恢复\n" +
            "- <color=#FFD54F>位置</color>：推 CharacterController，方向是喊的人给的";
        /// <summary>硬依赖的 Unity 组件：检查按钮真去预制体上找，缺了判红</summary>
        public static readonly string[] RequiredComponents = { "CharacterController" };
        private const string settingPath = "Setting/KnockbackSetting";

        /// <summary>
        /// 上岗检查：只读配置不改东西，红=本节点缺的，黄=提醒，不拦保存。
        /// </summary>
        public static void CheckContract(object config, System.Collections.Generic.List<string> red, System.Collections.Generic.List<string> yellow) {
            if (!(config is KnockbackSettingData c)) {
                red.Add("节点 Config 读不到，先重新生成节点");
                return;
            }

            if (c.Duration <= 0f) {
                red.Add("时长<=0，飞不起来，先填个 0.3 试试");
            }

            yellow.Add("提醒：光挂行为不会飞，得有人喊（接触伤害配击退距离），或调 Request");
        }

        /// <summary>
        /// 对外唯一的喊飞口（薄皮）：只往目标身上写纸条，不碰击退行为数据，真正起飞由击退自己轮询纸条决定。
        /// 遗留调用方不用改，老样子调这个就行。
        /// </summary>
        public static bool Request(Entity target, Vector3 direction, float distance, float duration) {
            if (target == null || target.Data == null || distance <= 0f) {
                return false;
            }

            Vector3 dir = direction;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) {
                return false;
            }

            WriteVector3(target, DataLabels.KnockbackDir, dir.normalized);
            WriteFloat(target, DataLabels.KnockbackDistance, distance);
            WriteFloat(target, DataLabels.KnockbackDuration, duration > 0f ? duration : 0.3f);
            int seq = 0;
            if (Cond.Instance.PeekData(target, DataLabels.KnockbackSeq, out IntData seqData) && seqData != null) {
                seq = seqData.Int;
            }

            WriteInt(target, DataLabels.KnockbackSeq, seq + 1);
            return true;
        }

        private KnockbackData _moveData;

        private MoveAttr _moveAttr;

        private int _lastSeq;

        public Behaviour_Auto_Knockback(Entity entity, string behaviourSign) : base(entity, behaviourSign) {
            _moveData = AttachBehaviourData<KnockbackData>();

            KnockbackSetting setting = Loader.LoadAsset<KnockbackSetting>(AssetType.ASSET, settingPath);

            if (!setting.TryGet(entity.ObjConfig.Sign, out KnockbackSettingData settingData)) {
                return;
            }

            _moveData.Config.Duration = Mathf.Max(settingData.Duration, 0.01f);
            _moveData.Config.DecayCurve = settingData.DecayCurve;
            _moveData.Config.KnockbackPriority = settingData.KnockbackPriority;
            _moveAttr = EntityAttrRegistry.RegisterOrGetMove(entity);
            if (_moveAttr != null) {
                _moveAttr.KnockbackPriority = _moveData.Config.KnockbackPriority;
            }

            Game.instance.OnUpdateEvent.AddListener(OnUpdate);
        }

        public override void DelayedExecute() {
        }

        private void OnUpdate() {
            PollKnockbackIntent();
            if (!_moveData.IsKnockingBack) {
                return;
            }

            if (_moveAttr != null && _moveAttr.Stopped) {
                StopKnockback();
                return;
            }

            CharacterController controller = Cond.Instance.Get<CharacterController>(entity, "CharacterController");
            if (controller == null) {
                StopKnockback();
                return;
            }

            _moveData.Elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_moveData.Elapsed / _moveData.Duration);
            float curveK = _moveData.Config.DecayCurve != null && _moveData.Config.DecayCurve.length > 0
                ? Mathf.Max(_moveData.Config.DecayCurve.Evaluate(t), 0f)
                : 1f;
            float speed = _moveData.Distance / _moveData.Duration * curveK;
            controller.Move(_moveData.Direction * speed * Time.deltaTime);
            if (t >= 1f) {
                StopKnockback();
            }
        }

        /// <summary>
        /// 只看自己身上的纸条：序号变了才起飞，不认识碰伤是谁。纸条缺失或方向距离无效就当没看见。
        /// </summary>
        private void PollKnockbackIntent() {
            if (!Cond.Instance.PeekData(entity, DataLabels.KnockbackSeq, out IntData seqData) || seqData == null) {
                return;
            }

            if (seqData.Int == _lastSeq) {
                return;
            }

            _lastSeq = seqData.Int;
            if (!Cond.Instance.PeekData(entity, DataLabels.KnockbackDir, out Vector3Data dirData) || dirData == null) {
                return;
            }

            Vector3 dir = dirData.Vector3;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) {
                return;
            }

            float distance = 0f;
            if (Cond.Instance.PeekData(entity, DataLabels.KnockbackDistance, out FloatData distData) && distData != null) {
                distance = distData.Float;
            }

            if (distance <= 0f) {
                return;
            }

            float duration = _moveData.Config.Duration;
            if (Cond.Instance.PeekData(entity, DataLabels.KnockbackDuration, out FloatData durData) && durData != null && durData.Float > 0f) {
                duration = durData.Float;
            }

            _moveData.Direction = dir.normalized;
            _moveData.Distance = distance;
            _moveData.Duration = duration;
            _moveData.Elapsed = 0f;
            _moveData.IsKnockingBack = true;
            if (_moveAttr != null) {
                _moveAttr.KnockingBack = true;
                _moveAttr.KnockbackPriority = _moveData.Config.KnockbackPriority;
            }
        }

        private void StopKnockback() {
            _moveData.IsKnockingBack = false;
            if (_moveAttr != null) {
                _moveAttr.KnockingBack = false;
            }
        }

        private static void WriteFloat(Entity target, string sign, float value) {
            if (!Cond.Instance.PeekData(target, sign, out FloatData data) || data == null) {
                target.Data.Add<FloatData>(sign, sign);
                Cond.Instance.PeekData(target, sign, out data);
            }

            if (data != null) {
                data.Float = value;
            }
        }

        private static void WriteInt(Entity target, string sign, int value) {
            if (!Cond.Instance.PeekData(target, sign, out IntData data) || data == null) {
                target.Data.Add<IntData>(sign, sign);
                Cond.Instance.PeekData(target, sign, out data);
            }

            if (data != null) {
                data.Int = value;
            }
        }

        private static void WriteVector3(Entity target, string sign, Vector3 value) {
            if (!Cond.Instance.PeekData(target, sign, out Vector3Data data) || data == null) {
                target.Data.Add<Vector3Data>(sign, sign);
                Cond.Instance.PeekData(target, sign, out data);
            }

            if (data != null) {
                data.Vector3 = value;
            }
        }

        public override void Clear() {
            Game.instance.OnUpdateEvent.RemoveListener(OnUpdate);
            if (_moveAttr != null) {
                _moveAttr.KnockingBack = false;
            }

            DetachBehaviourData<KnockbackData>();
            base.Clear();
        }
    }
}
