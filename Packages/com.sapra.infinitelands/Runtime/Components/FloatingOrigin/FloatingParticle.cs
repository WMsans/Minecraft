using UnityEngine;

namespace sapra.InfiniteLands{
    public class FloatingParticle : MonoBehaviour
    {
        private FloatingOrigin origin;
        private ParticleSystem particleSystm;

        // Start is called before the first frame update
        void Awake()
        {
            origin = FindAnyObjectByType<FloatingOrigin>();
            particleSystm = GetComponent<ParticleSystem>();
        }
        void OnDisable()
        {
            if(origin != null)
                origin.OnOriginMove -= OnOriginShift;
        }
        void OnEnable()
        {
            if(origin != null)
                origin.OnOriginMove += OnOriginShift;
        }
        private void OnOriginShift(Vector3Double newOrigin, Vector3Double previousOrigin){
            ParticleSystem.Particle[] particles = new ParticleSystem.Particle[particleSystm.particleCount];
            particleSystm.GetParticles(particles);
            Vector3 offset = previousOrigin-newOrigin;
            for(int i = 0; i < particleSystm.particleCount; i++){
                ParticleSystem.Particle particle = particles[i];
                particle.position += offset;
                particles[i] = particle;
            }
            particleSystm.SetParticles(particles);
        }
    }
}
