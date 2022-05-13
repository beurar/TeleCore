using UnityEngine;

namespace TeleCore
{
    public class MoveProperties
    {
        public int startOffset = 0;
        public int endOffset = 5;
        public int moveSpeed = 1;

        public float MoverSpeed => Mathf.Lerp(0, (endOffset - startOffset), moveSpeed);
    }
}
