using UnityEngine;

namespace Gumroad.Window
{
    [CreateAssetMenu(fileName = "WindowColors", menuName = "Gumroad/Package Manager/Create Window Colors", order = 2)]
    public class GumroadWindowColors : ScriptableObject
    {
        public Color side_background = new Color();
        public Color left_panel_background = new Color();
        public Color right_panel_background = new Color();
        public Color wireframe = new Color();
        public Color button_selected = new Color();
        public Color button_background = new Color();
        public Color text_color = new Color();
        public Color link_color = new Color();
    }
}