using UnityEngine;

namespace GunPackCyber
{
    public class WeaponEnergyBar : MonoBehaviour
    {
        [Header("Energy")]
        public float maxEnergy = 100f;
        public float currentEnergy;
        public float energyCostPerShot = 5f;

        [Header("Energy Segments")]
        public GameObject[] lightMeshes; // 10 - 15 segments
        [Header("Materials")]
        public Material chargedMaterial; // full
        public Material activeMaterial;  // segment being used
        public Material emptyMaterial;   // empty

        void Start()
        {
            currentEnergy = maxEnergy;
            UpdateEnergyBar();
        }

        void Update()
        {
        #if ENABLE_LEGACY_INPUT_MANAGER

            if (Input.GetButtonDown("Fire1"))
            {
                Shoot();
            }

        #else

            Debug.LogWarning("Este script usa el OLD Input System. Cambia Player Settings a 'Both' o 'Old' para usarlo.");

        #endif
        }

        void Shoot()
        {
            if (currentEnergy <= 0)
                return;

            currentEnergy -= energyCostPerShot;
            currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);

            UpdateEnergyBar();
        }

        void UpdateEnergyBar()
        {
            int totalSegments = lightMeshes.Length;

            float energyPercent = currentEnergy / maxEnergy;

            int filledSegments = Mathf.FloorToInt(energyPercent * totalSegments);

            for (int i = 0; i < totalSegments; i++)
            {
                Renderer r = lightMeshes[i].GetComponent<Renderer>();

                if (i < filledSegments)
                {
                    r.material = chargedMaterial;
                }
                else if (i == filledSegments && currentEnergy > 0)
                {
                    r.material = activeMaterial;
                }
                else
                {
                    r.material = emptyMaterial;
                }
            }
        }
    }
}