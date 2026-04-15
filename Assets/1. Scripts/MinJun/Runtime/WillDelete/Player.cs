using UnityEngine;

namespace minjun
{
    public class Player : MonoBehaviour
    {
        private Food food;
        public bool HasFood()
        {
            return true;
        }

        public void AddFood(Food food)
        {
            this.food = food;
        }

        public Food RemoveFood()
        {
            var temp = food;
            food = null;
            return temp;
        }
    }
}