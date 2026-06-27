using UnityEngine;

namespace CrowdRunner
{
    // Подарок на краю дорожки: при касании толпой даёт случайный бонус юнитов.
    public class GiftBox : MonoBehaviour
    {
        private int _min = 10;
        private int _max = 30;
        private bool _used;

        public void Init(int min, int max)
        {
            _min = min; _max = max; _used = false;
        }

        public void Collect(SquadController squad)
        {
            if (_used) return;
            _used = true;
            int n = Random.Range(_min, _max + 1);
            squad.ModifyUnits(GateOp.Add, n);
            AudioController.Instance?.PlayGate();
            EffectsManager.Burst(transform.position + Vector3.up * 0.6f, new Color(1f, 0.85f, 0.3f), 1.5f);
            gameObject.SetActive(false);
        }
    }
}
