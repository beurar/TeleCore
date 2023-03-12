using UnityEngine;
using Verse;

namespace TeleCore
{
    internal class Dialog_AnimatorHelp : Window
    {
        public override Vector2 InitialSize => new Vector2(640f, 460f);

        public Dialog_AnimatorHelp()
        {
            doCloseButton = true;
            closeOnClickedOutside = true;
            layer = WindowLayer.Super;
        }

        public override void DoWindowContents(Rect inRect)
        {
            //
            Listing_Standard textListing = new Listing_Standard();
            textListing.Begin(inRect);

            textListing.Label("Time Line Controls".Bold());
            textListing.GapLine(4); 
            textListing.LabelDouble("Zoom In Timeline", $"[{KeyCode.LeftShift}] + [{EventType.ScrollWheel}]");
            textListing.LabelDouble("Copy Selected KeyFrame", $"[{KeyCode.LeftControl}] + [{KeyCode.C}]");
            textListing.LabelDouble("Paste Selected KeyFrame", $"[{KeyCode.LeftControl}] + [{KeyCode.V}]"); 
            textListing.LabelDouble("Delete Selected KeyFrame", $"[{KeyCode.Delete}]");

            textListing.Gap();
            textListing.Label("Texture Sheet Grid".Bold());
            textListing.GapLine(4);
            textListing.LabelDouble("Free Form Selection", $"[{KeyCode.LeftShift}] + [{EventType.MouseDrag}]");

            textListing.End();
        }
    }
}
