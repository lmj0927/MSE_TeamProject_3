using UnityEngine;

namespace sy
{
 
    public class Food : MonoBehaviour
    {
        [SerializeField] private FoodSO data;

        public FoodSO Data => data;

        public void SetData(FoodSO so)
        {
            data = so;
        }
    }
}
