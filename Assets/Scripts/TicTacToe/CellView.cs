using UnityEngine;
using UnityEngine.UI;

namespace TicTacToe5
{
    [RequireComponent(typeof(Button))]
    public class CellView : MonoBehaviour
    {
        [Tooltip("row * 5 + column, starting at the top-left square")]
        public int Index;

        [Tooltip("Image that shows the black square / red triangle")]
        public Image Mark;

        [Tooltip("Background shown on the winning line")]
        public Image Highlight;

        Button button;

        public Button Button => button != null ? button : (button = GetComponent<Button>());

        public void Clear()
        {
            Mark.sprite = null;
            Mark.enabled = false;
            Highlight.enabled = false;
            Button.interactable = true;
        }

        public void SetMark(Sprite sprite)
        {
            Mark.sprite = sprite;
            Mark.enabled = true;
            Button.interactable = false;
        }
    }
}
