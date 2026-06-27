using UnityEngine;

namespace CrowdRunner
{
    // Боковая статуя-усилитель: пока отряд в зоне действия — тикает +N юнитов.
    // Не блокирует движение (стоит за бордюром).
    public class Booster : MonoBehaviour
    {
        [SerializeField] private TextMesh _label;
        [SerializeField] private BoxCollider _zone;   // триггер-зона вдоль Z

        private int _perTick = 2;
        private float _tick = 0.5f;
        private float _timer;
        private bool _inside;
        private SquadController _squad;

        public void Init(int perTick, float tick)
        {
            _perTick = Mathf.Max(1, perTick);
            _tick = Mathf.Max(0.1f, tick);
            if (_label != null) { _label.text = "+" + _perTick; _label.transform.rotation = Quaternion.Euler(0f, 180f, 0f); }
            if (_zone != null) _zone.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            var sq = other.GetComponentInParent<SquadController>();
            if (sq != null) { _squad = sq; _inside = true; _timer = 0f; }
        }

        private void OnTriggerExit(Collider other)
        {
            var sq = other.GetComponentInParent<SquadController>();
            if (sq != null && sq == _squad) _inside = false;
        }

        private void Update()
        {
            if (!_inside || _squad == null) return;
            var gm = RunnerGameManager.Instance;
            if (gm == null || gm.Phase != GamePhase.Running) return;

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _timer = _tick;
                _squad.ModifyUnits(GateOp.Add, _perTick);
                // число летит от статуи к отряду
                EffectsManager.Float(transform.position + Vector3.up * 2f, "+" + _perTick, new Color(0.5f, 1f, 0.6f));
            }
        }
    }
}
