using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Toolbar : MonoBehaviour
{
    public UIItemSlot[] slots;
    public RectTransform highlight;
    public Player player;
    public int slotIndex = 0;

    public void Start()
    {
        // Uncomment this if you want to populate toolbar with first 9 blocks of random amount
        /*
        byte index = 1;

        foreach (UIItemSlot s in slots)
        {
            ItemStack stack = new ItemStack(index, Random.Range(2,65));
            ItemSlot slot = new ItemSlot(s, stack);
            index++;
        }
        */
    }

    private void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0)
        {
            if (scroll > 0)
                slotIndex--;
            else
                slotIndex++;
        }

        if (slotIndex > slots.Length - 1)
            slotIndex = 0;
        if (slotIndex < 0)
            slotIndex = slots.Length - 1;

        // Position we are not srcolled on and we want to highlight for Player
        highlight.position = slots[slotIndex].slotIcon.transform.position;
    }
}
