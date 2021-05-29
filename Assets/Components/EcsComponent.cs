using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Client {
    struct UnitComponent
    {
        public GameObject gameObject;

        public void setHealth(float value)
        {
            Slider healthSlider = gameObject.transform.Find("Health Bar Canvas").transform.Find("Slider").GetComponent<Slider>();
            healthSlider.value = value;
        }

        public void deacreaseHealth(float value)
        {
            Slider healthSlider = gameObject.transform.Find("Health Bar Canvas").transform.Find("Slider").GetComponent<Slider>();
            healthSlider.value -= value;
        }

        public float getHealth()
        {
            Slider healthSlider = gameObject.transform.Find("Health Bar Canvas").transform.Find("Slider").GetComponent<Slider>();
            return healthSlider.value;
        }   
    }

    struct SpawnRequest
    {

    }

    struct BombComponent
    {
        public GameObject gameObject;
        public float expForce, radius;
    }

    struct SpawnBombRequest
    {
        public Vector3 position;
    }

    struct DetonateBombRequest
    {
        public BombComponent bombComponent;
    }

    struct AttackUnitRequest
    {
        public float intencity;
        public UnitComponent unitComponent;
    }
}