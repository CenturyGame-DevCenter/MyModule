using UnityEngine;
using UnityEngine.UI;

namespace CenturyGame.Framework.UI
{
    public static class UIExtension
    {
        public static Text GetText(this Transform t)
        {
            return t.GetComponent<Text>();
        }

        public static Button GetButton(this Transform t)
        {
            return t.GetComponent<Button>();
        }
    }
}