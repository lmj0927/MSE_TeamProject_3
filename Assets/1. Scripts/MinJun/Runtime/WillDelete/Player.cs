using UnityEngine;

namespace minjun
{
    public class Player : MonoBehaviour
    {
        private Food food;
        public bool HasFood()
        {
            return food != null;
        }

        public void AddFood(Food food)
        {
            this.food = food;
            this.food.transform.position = this.transform.position;
        }

        public Food RemoveFood()
        {
            var temp = food;
            food = null;
            return temp;
        }
    }
}