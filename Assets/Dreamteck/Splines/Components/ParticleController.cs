using UnityEngine;
using System.Collections;

namespace Dreamteck.Splines
{
    [ExecuteInEditMode]
    [AddComponentMenu("Dreamteck/Splines/Particle Controller")]
    public class ParticleController : SplineUser
    {
        public class Particle
        {
            public Vector2 startOffset = Vector2.zero;
            public Vector2 endOffset = Vector2.zero;
            public float speed = 0f;
            public double splinePercent = 0f;
            public float lifePercent = 0f;
        }

        [HideInInspector]
        public ParticleSystem _particleSystem;
        public enum EmitPoint { Beginning, Ending, Random, Ordered }

        public enum MotionType { None, UseParticleSystem, FollowForward, FollowBackward, ByNormal, ByNormalRandomized }

        public enum Wrap { Default, Loop }

        [HideInInspector]
        public bool volumetric = false;
        [HideInInspector]
        public bool emitFromShell = false;
        [HideInInspector]
        public Vector2 scale = Vector2.one;
        [HideInInspector]
        public EmitPoint emitPoint = EmitPoint.Beginning;
        [HideInInspector]
        public MotionType motionType = MotionType.UseParticleSystem;
        [HideInInspector]
        public Wrap wrapMode = Wrap.Default;
        [HideInInspector]
        public float minCycles = 1f;
        [HideInInspector]
        public float maxCycles = 2f;

        private ParticleSystem.Particle[] particles = null;
        private ParticleController.Particle[] particleControllers = null;
        private int particleCount = 0;
        private int birthIndex = 0;

        protected override void Awake() 
        {
            base.Awake();
        }

        protected override void Run()
        {
            if (_particleSystem == null) return;
            lock (locker)
            {
                if (particles == null || particles.Length != _particleSystem.maxParticles)
                {
                    particles = new ParticleSystem.Particle[_particleSystem.maxParticles];
                    if (particleControllers == null) particleControllers = new ParticleController.Particle[_particleSystem.maxParticles];
                    else
                    {
                        ParticleController.Particle[] newControllers = new ParticleController.Particle[_particleSystem.maxParticles];
                        for (int i = 0; i < newControllers.Length; i++)
                        {
                            if (i >= particleControllers.Length) break;
                            newControllers[i] = particleControllers[i];
                        }
                        particleControllers = newControllers;
                    }
                }
                particleCount = _particleSystem.GetParticles(particles);
                bool isLocal = _particleSystem.simulationSpace == ParticleSystemSimulationSpace.Local;
                Transform particleSystemTransform = _particleSystem.transform;
                for (int i = 0; i < particleCount; i++)
                {
                    if (particles[i].remainingLifetime <= 0f) continue;
                    if (isLocal)
                    {
                        particles[i].position = particleSystemTransform.TransformPoint(particles[i].position);
                        particles[i].velocity = particleSystemTransform.TransformDirection(particles[i].velocity);
                    }
                    HandleParticle(i);
                    if (isLocal)
                    {
                        particles[i].position = particleSystemTransform.InverseTransformPoint(particles[i].position);
                        particles[i].velocity = particleSystemTransform.InverseTransformDirection(particles[i].velocity);
                    }
                }
                _particleSystem.SetParticles(particles, particleCount);
            }
        }

        protected override void Reset()
        {
            base.Reset();
            if (_particleSystem == null) _particleSystem = GetComponent<ParticleSystem>();
        }

        void HandleParticle(int index)
        {
            float lifePercent = particles[index].remainingLifetime / particles[index].startLifetime;
            if (particleControllers[index] == null)
            {
                particleControllers[index] = new ParticleController.Particle();
            }
            if (lifePercent > particleControllers[index].lifePercent) OnParticleBorn(index);
           // if (particleControllers[index].lifePercent > lifePercent && lifePercent <= Time.deltaTime) OnParticleDeath(index);

           
            float lifeDelta = particleControllers[index].lifePercent - lifePercent;

            switch (motionType)
            {
                case MotionType.FollowForward: particleControllers[index].splinePercent += lifeDelta * particleControllers[index].speed; break;
                case MotionType.FollowBackward: particleControllers[index].splinePercent -= lifeDelta * particleControllers[index].speed; break;
            }

            if (particleControllers[index].splinePercent < 0f)
            {
                if (wrapMode == Wrap.Loop) particleControllers[index].splinePercent += 1f;
                if (wrapMode == Wrap.Default) particleControllers[index].splinePercent = 0f;
            }
            else if (particleControllers[index].splinePercent > 1f)
            {
                if (wrapMode == Wrap.Loop) particleControllers[index].splinePercent -= 1f;
                if (wrapMode == Wrap.Default) particleControllers[index].splinePercent = 1f;
            }

            if (motionType == MotionType.FollowBackward || motionType == MotionType.FollowForward || motionType == MotionType.None)
            {
                SplineResult result = Evaluate(DMath.Lerp(clipFrom, clipTo, particleControllers[index].splinePercent));
                particles[index].position = result.position;
                if (volumetric)
                {
                    Vector3 right = -Vector3.Cross(result.direction, result.normal);
                    Vector2 offset = particleControllers[index].startOffset;
                    if (motionType != MotionType.None) offset = Vector2.Lerp(particleControllers[index].startOffset, particleControllers[index].endOffset, 1f - particleControllers[index].lifePercent);
                    particles[index].position += right * offset.x * scale.x * result.size + result.normal * offset.y * scale.y * result.size;
                }
                particles[index].velocity = result.direction;
            }

            particleControllers[index].lifePercent = lifePercent;
        }


        void OnParticleBorn(int index)
        {
            birthIndex++;
            double percent = 0.0;
#if UNITY_5_3 || UNITY_5_3_OR_NEWER
            float emissionRate = Mathf.Lerp(_particleSystem.emission.rate.constantMin, _particleSystem.emission.rate.constantMax, 0.5f);
#else
            float emissionRate = _particleSystem.emissionRate;
#endif
            float expectedParticleCount = emissionRate * _particleSystem.startLifetime;
            if (birthIndex > expectedParticleCount) birthIndex = 0;
            switch (emitPoint)
            {
                case EmitPoint.Beginning: percent = 0f; break;
                case EmitPoint.Ending: percent = 1f; break;
                case EmitPoint.Random: percent = Random.Range(0f, 1f); break;
                case EmitPoint.Ordered: percent = expectedParticleCount > 0 ? (float)birthIndex / expectedParticleCount : 0f;  break;
            }
            SplineResult result = Evaluate(DMath.Lerp(clipFrom, clipTo, percent));
            particleControllers[index].splinePercent = percent;
          
            particleControllers[index].speed = Random.Range(minCycles, maxCycles);
            Vector2 circle = Vector2.zero;
            if (volumetric)
            {
                if (emitFromShell) circle = Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.forward) * Vector2.right;
                else circle = Random.insideUnitCircle;
            }
            particleControllers[index].startOffset = circle * 0.5f;
            particleControllers[index].endOffset = Random.insideUnitCircle * 0.5f;


            Vector3 right = Vector3.Cross(result.direction, result.normal);
            particles[index].position = result.position + right * particleControllers[index].startOffset.x * result.size * scale.x + result.normal * particleControllers[index].startOffset.y * result.size * scale.y;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
            float forceX = _particleSystem.forceOverLifetime.x.constantMax;
            float forceY = _particleSystem.forceOverLifetime.y.constantMax;
            float forceZ = _particleSystem.forceOverLifetime.z.constantMax;
            if (_particleSystem.forceOverLifetime.randomized)
            {
                forceX = Random.Range(_particleSystem.forceOverLifetime.x.constantMin, _particleSystem.forceOverLifetime.x.constantMax);
                forceY = Random.Range(_particleSystem.forceOverLifetime.y.constantMin, _particleSystem.forceOverLifetime.y.constantMax);
                forceZ = Random.Range(_particleSystem.forceOverLifetime.z.constantMin, _particleSystem.forceOverLifetime.z.constantMax);
            }


            float time = particles[index].startLifetime - particles[index].remainingLifetime;
            Vector3 forceDistance = new Vector3(forceX, forceY, forceZ) * 0.5f * (time * time);

            if (motionType == MotionType.ByNormal)
            {
                particles[index].position += result.normal * _particleSystem.startSpeed * (particles[index].startLifetime - particles[index].remainingLifetime);
                particles[index].position += forceDistance;
                particles[index].velocity = result.normal * _particleSystem.startSpeed + new Vector3(forceX, forceY, forceZ) * time;
            }
            else if (motionType == MotionType.ByNormalRandomized)
            {
                Vector3 normal = Quaternion.AngleAxis(Random.Range(0f, 360f), result.direction) * result.normal;
                particles[index].position += normal * _particleSystem.startSpeed * (particles[index].startLifetime - particles[index].remainingLifetime);
                particles[index].position += forceDistance;
                particles[index].velocity = normal * _particleSystem.startSpeed + new Vector3(forceX, forceY, forceZ) * time;
            }
#endif
        }
    }
}
