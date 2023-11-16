using UnityEngine;

namespace Project.Scripts.Objects
{
    public class ParticleController : ProjectPoolObject
    {
        [SerializeField] private ParticleSystem m_Particle;

        [SerializeField] private float m_MinEmissionAmount;
        [SerializeField] private float m_MaxEmissionAmount;

        public void SetParticleEmission(float percentage)
        {
            var emission = m_Particle.emission;
            emission.burstCount = (int) Mathf.Lerp(m_MinEmissionAmount, m_MaxEmissionAmount, percentage);
        }
    }
}