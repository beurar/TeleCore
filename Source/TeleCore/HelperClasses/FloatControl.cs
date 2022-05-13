using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public class FloatControl
    {
        private const float deltaTime = 0.016666668f;

        private readonly float fixedAcc;
        private readonly float fixedTimeInc;
        private readonly float maxValue;

        private bool starting, stopping;
        private float curProgress = 0;
        private float curValue;

        private SimpleCurve AccelerationCurve;
        private SimpleCurve DecelerationCurve;
        private SimpleCurve OutputCurve;

        public bool ReachedPeak => Math.Abs(CurPct - 1f) < 0.001953125f;
        public bool StoppedDead => CurPct == 0f;
        public float CurPct => curValue / maxValue;
        public float CurValue => curValue;
        public float OutputValue => OutputCurve?.Evaluate(CurPct) ?? curValue;

        public float Acceleration
        {
            get
            {
                if (CurState == FCState.Accelerating)
                    return AccelerationCurve.Evaluate(curProgress) * fixedAcc;
                if (CurState == FCState.Decelerating)
                    return (DecelerationCurve.Evaluate(curProgress) * fixedAcc).Negate();
                return 0;
            }
        }

        public enum FCState
        {
            Accelerating,
            Decelerating,
            Sustaining,
            Idle
        }

        public FCState CurState
        {
            get
            {
                if (starting && !ReachedPeak) return FCState.Accelerating;
                if (stopping && !StoppedDead) return FCState.Decelerating;
                return FCState.Sustaining;
            }
        }

        public FloatControl(float maxValue, float secondsToMax, SimpleCurve accCurve = null, SimpleCurve decCurve = null, SimpleCurve outCurve = null)
        {
            this.maxValue = maxValue;
            fixedAcc = maxValue / secondsToMax;
            fixedTimeInc = secondsToMax / deltaTime;

            AccelerationCurve = accCurve ?? new SimpleCurve()
            {
                new(0, 0),
                new(1, 1),
            };
            DecelerationCurve = decCurve ?? AccelerationCurve;
            OutputCurve = outCurve ?? new SimpleCurve()
            {
                new(0, 0),
                new(1, maxValue),
            };
        }

        public void Tick()
        {
            if (CurState == FCState.Sustaining) return;
            curProgress = Mathf.Clamp01(curProgress + fixedTimeInc);
            curValue = Mathf.Clamp(curValue + Acceleration * deltaTime, 0, maxValue);
        }

        public void Start()
        {
            starting = true;
            stopping = false;
        }

        public void Stop()
        {
            starting = false;
            stopping = true;
        }
    }
}
