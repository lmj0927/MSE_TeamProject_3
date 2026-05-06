using UnityEngine;
using DG.Tweening;
public class RefrigeratorCounter : ACounter
{
    [SerializeField] private IngredientPopupUI ingredientPopupUI;
    private PlayerController interactPlayer;

    [SerializeField] private GameObject[] hinges;
    [SerializeField] private float openAngle = 40f;
    [SerializeField] private float doorAnimDuration = 0.5f;
    void Start()
    {
        ingredientPopupUI.OnIngredientSelected += OnIngredientSelected;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (interactPlayer != null)
            {
                interactPlayer.GetComponent<PlayerMovement>().SetInteracting(false);
                interactPlayer = null;
            }
            SetDoors(false);
            ingredientPopupUI.Hide();
        }
    }

    public override void Interact(PlayerController player)
    {
        if (!player.HasFood())
        {
            interactPlayer = player;
            interactPlayer.GetComponent<PlayerMovement>().SetInteracting(true);
            SetDoors(true);
            ingredientPopupUI.Show();
        }
    }

    private void OnIngredientSelected(Food food)
    {
        if(food != null)
            interactPlayer.AddFood(food);
        interactPlayer.GetComponent<PlayerMovement>().SetInteracting(false);
        interactPlayer = null;
        SetDoors(false);

    }
    private void SetDoors(bool isOpen)
    {
        float targetAngle = isOpen ? openAngle : 0f;

        hinges[0].transform.DOLocalRotate(new Vector3(0, targetAngle, 0), doorAnimDuration)
            .SetEase(Ease.OutQuad);

        hinges[1].transform.DOLocalRotate(new Vector3(0, -targetAngle, 0), doorAnimDuration)
            .SetEase(Ease.OutQuad);
    }
}
